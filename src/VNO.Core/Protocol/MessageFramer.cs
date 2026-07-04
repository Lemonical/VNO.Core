using System;
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
    private bool _escaping;

    /// <summary>
    /// Adds a chunk of received bytes and returns any complete messages found
    /// </summary>
    public IReadOnlyList<NetworkMessage> Append(ReadOnlySpan<byte> chunk)
    {
        var text = ProtocolConstants.WireEncoding.GetString(chunk);
        return Append(text);
    }

    /// <summary>
    /// Adds already decoded text and returns any complete messages found
    /// </summary>
    public IReadOnlyList<NetworkMessage> Append(string text)
    {
        var messages = new List<NetworkMessage>();
        foreach (var character in text)
        {
            if (_pending.Length > ProtocolConstants.MaxMessageBytes)
            {
                throw new InvalidOperationException("Incoming message exceeded the size limit");
            }

            if (_escaping)
            {
                _pending.Append(character);
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
                continue;
            }

            _pending.Append(character);
        }

        return messages;
    }
}
