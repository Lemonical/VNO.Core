using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace VNO.Core.Protocol;

/// <summary>
/// Reassembles whole messages from a byte stream that arrives in chunks
/// </summary>
/// <remarks>
/// TCP gives no message boundaries, a single read can hold part of a message or
/// several messages. Feed every received chunk to the append methods and they
/// return each complete message they can extract. State is kept between calls so
/// a split message is joined across reads. This type is not thread safe, use one
/// framer per connection
/// </remarks>
public sealed class MessageFramer
{
    private const char Escape = '\\';

    private readonly StringBuilder _pending = new();
    private readonly Decoder _decoder = ProtocolConstants.WireEncoding.GetDecoder();
    private readonly int _maximumMessageCharacters;
    private bool _escaping;

    /// <summary>
    /// Creates a framer with a bounded pending-message size
    /// </summary>
    public MessageFramer(int maximumMessageBytes = ProtocolConstants.MaxMessageBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumMessageBytes);
        _maximumMessageCharacters = maximumMessageBytes;
    }

    /// <summary>
    /// Adds a chunk of received bytes and returns any complete messages found
    /// </summary>
    public IReadOnlyList<NetworkMessage> Append(ReadOnlySpan<byte> chunk)
    {
        var buffer = ArrayPool<char>.Shared.Rent(ProtocolConstants.WireEncoding.GetMaxCharCount(chunk.Length));
        try
        {
            var count = _decoder.GetChars(chunk, buffer, flush: false);
            return Append(buffer.AsSpan(0, count));
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Adds already decoded text and returns any complete messages found
    /// </summary>
    public IReadOnlyList<NetworkMessage> Append(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Append(text.AsSpan());
    }

    private IReadOnlyList<NetworkMessage> Append(ReadOnlySpan<char> text)
    {
        var messages = new List<NetworkMessage>();
        foreach (var character in text)
        {
            if (_escaping)
            {
                AppendPending(character);
                _escaping = false;
                continue;
            }

            if (character == Escape)
            {
                _pending.Append(character);
                _escaping = true;
                continue;
            }

            if (character == ProtocolConstants.MessageTerminator)
            {
                messages.Add(MessageCodec.Decode(_pending.ToString()));
                _pending.Clear();
                if (_pending.Capacity > _maximumMessageCharacters / 4)
                {
                    _pending.Capacity = Math.Max(16, _maximumMessageCharacters / 4);
                }
                continue;
            }

            AppendPending(character);
        }

        return messages;
    }

    private void AppendPending(char character)
    {
        if (_pending.Length >= _maximumMessageCharacters)
        {
            throw new InvalidOperationException("Incoming message exceeded the size limit");
        }

        _pending.Append(character);
    }
}
