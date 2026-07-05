using System;
using System.Collections.Generic;
using System.Text;

namespace VNO.Core.Protocol;

/// <summary>
/// Turns messages into wire text and back
/// </summary>
/// <remarks>
/// The wire form is HEADER then a delimiter then each argument joined by the
/// delimiter. On the TCP stream a terminator ends each message so the framer can
/// split it back out. On the WebSocket path one frame is exactly one message, so the
/// terminator is omitted, see <see cref="EncodeFrameBytes"/>. Delimiter, terminator,
/// and escape characters inside an argument are escaped so they cannot break framing.
/// Decoding caps the field count and field length so a small hostile frame cannot force
/// a large allocation
/// </remarks>
public static class MessageCodec
{
    private const char Escape = '\\';

    /// <summary>
    /// Encodes a message into its wire text form including the terminator
    /// </summary>
    public static string Encode(NetworkMessage message) => Encode(message, includeTerminator: true);

    /// <summary>
    /// Encodes a message into the bytes used on the TCP stream, terminator included
    /// </summary>
    public static byte[] EncodeBytes(NetworkMessage message) =>
        ProtocolConstants.WireEncoding.GetBytes(Encode(message, includeTerminator: true));

    /// <summary>
    /// Encodes a message into the bytes of one WebSocket frame, no terminator
    /// </summary>
    /// <remarks>
    /// One frame is one message, so the terminator that the stream framer needs is dead
    /// weight here. Escaping is kept identical to the stream form so both paths share one
    /// decode routine and cannot drift
    /// </remarks>
    public static byte[] EncodeFrameBytes(NetworkMessage message) =>
        ProtocolConstants.WireEncoding.GetBytes(Encode(message, includeTerminator: false));

    /// <summary>
    /// Parses one message body, the text between terminators without the terminator
    /// </summary>
    public static NetworkMessage Decode(string body) =>
        Decode(body, ProtocolConstants.MaxFieldCount, ProtocolConstants.MaxFieldLength);

    /// <summary>
    /// Parses one message body under explicit field caps
    /// </summary>
    /// <exception cref="ProtocolFormatException">
    /// Thrown when the field count or a field length exceeds the given caps
    /// </exception>
    public static NetworkMessage Decode(string body, int maxFieldCount, int maxFieldLength)
    {
        ArgumentNullException.ThrowIfNull(body);

        var fields = SplitFields(body, maxFieldCount, maxFieldLength);
        if (fields.Count == 0)
        {
            return new NetworkMessage(string.Empty, MessageType.Unknown, Array.Empty<string>());
        }

        var header = fields[0];
        var arguments = fields.GetRange(1, fields.Count - 1);
        return new NetworkMessage(header, MessageHeaders.FromHeader(header), arguments);
    }

    /// <summary>
    /// Decodes the bytes of one WebSocket frame into a message
    /// </summary>
    /// <remarks>
    /// The frame carries no terminator, so the whole payload is one message body. Uses
    /// the auth traffic field caps by default
    /// </remarks>
    public static NetworkMessage DecodeFrame(ReadOnlySpan<byte> frame) =>
        DecodeFrame(frame, ProtocolConstants.MaxFieldCount, ProtocolConstants.MaxFieldLength);

    /// <summary>
    /// Decodes the bytes of one WebSocket frame into a message under explicit field caps
    /// </summary>
    public static NetworkMessage DecodeFrame(ReadOnlySpan<byte> frame, int maxFieldCount, int maxFieldLength)
    {
        var body = ProtocolConstants.WireEncoding.GetString(frame);
        return Decode(body, maxFieldCount, maxFieldLength);
    }

    private static string Encode(NetworkMessage message, bool includeTerminator)
    {
        ArgumentNullException.ThrowIfNull(message);

        var builder = new StringBuilder();
        builder.Append(Escaped(message.Header));
        foreach (var argument in message.Arguments)
        {
            builder.Append(ProtocolConstants.FieldDelimiter);
            builder.Append(Escaped(argument));
        }

        if (includeTerminator)
        {
            builder.Append(ProtocolConstants.MessageTerminator);
        }

        return builder.ToString();
    }

    private static List<string> SplitFields(string body, int maxFieldCount, int maxFieldLength)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var escaping = false;

        void Commit()
        {
            if (fields.Count >= maxFieldCount)
            {
                throw new ProtocolFormatException(
                    $"Message exceeded the field count cap of {maxFieldCount}");
            }

            fields.Add(current.ToString());
            current.Clear();
        }

        void AppendChecked(char character)
        {
            if (current.Length >= maxFieldLength)
            {
                throw new ProtocolFormatException(
                    $"A field exceeded the length cap of {maxFieldLength}");
            }

            current.Append(character);
        }

        foreach (var character in body)
        {
            if (escaping)
            {
                AppendChecked(character);
                escaping = false;
                continue;
            }

            switch (character)
            {
                case Escape:
                    escaping = true;
                    break;
                case ProtocolConstants.FieldDelimiter:
                    Commit();
                    break;
                default:
                    AppendChecked(character);
                    break;
            }
        }

        Commit();
        return fields;
    }

    private static string Escaped(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is Escape
                or ProtocolConstants.FieldDelimiter
                or ProtocolConstants.MessageTerminator)
            {
                builder.Append(Escape);
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
