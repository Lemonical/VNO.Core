using System;
using System.Collections.Generic;
using System.Text;

namespace VNO.Core.Protocol;

/// <summary>
/// Turns messages into wire text and back
/// </summary>
/// <remarks>
/// The wire form is HEADER then a delimiter then each argument joined by the
/// delimiter then the terminator. Delimiter and terminator characters inside an
/// argument are escaped so they cannot break the framing
/// </remarks>
public static class MessageCodec
{
    private const char Escape = '\\';

    /// <summary>
    /// Encodes a message into its wire text form including the terminator
    /// </summary>
    public static string Encode(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var builder = new StringBuilder();
        builder.Append(Escaped(message.Header));
        foreach (var argument in message.Arguments)
        {
            builder.Append(ProtocolConstants.FieldDelimiter);
            builder.Append(Escaped(argument));
        }

        builder.Append(ProtocolConstants.MessageTerminator);
        return builder.ToString();
    }

    /// <summary>
    /// Encodes a message into the bytes used on the wire
    /// </summary>
    public static byte[] EncodeBytes(NetworkMessage message) =>
        ProtocolConstants.WireEncoding.GetBytes(Encode(message));

    /// <summary>
    /// Parses one message body, the text between terminators without the terminator
    /// </summary>
    public static NetworkMessage Decode(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var fields = SplitFields(body);
        if (fields.Count == 0)
        {
            return new NetworkMessage(string.Empty, MessageType.Unknown, Array.Empty<string>());
        }

        var header = fields[0];
        var arguments = fields.GetRange(1, fields.Count - 1);
        return new NetworkMessage(header, MessageHeaders.FromHeader(header), arguments);
    }

    private static List<string> SplitFields(string body)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var escaping = false;

        foreach (var character in body)
        {
            if (escaping)
            {
                current.Append(character);
                escaping = false;
                continue;
            }

            switch (character)
            {
                case Escape:
                    escaping = true;
                    break;
                case ProtocolConstants.FieldDelimiter:
                    fields.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(character);
                    break;
            }
        }

        fields.Add(current.ToString());
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
