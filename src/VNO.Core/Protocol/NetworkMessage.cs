using System.Collections.Generic;
using System.Linq;

namespace VNO.Core.Protocol;

/// <summary>
/// One protocol message, a header plus its string arguments
/// </summary>
/// <remarks>
/// This type is transport agnostic, it does not know about sockets. Use
/// <see cref="MessageCodec"/> to turn it into bytes or to parse it from bytes
/// </remarks>
public sealed class NetworkMessage
{
    /// <summary>
    /// Creates a message from a known type and its arguments
    /// </summary>
    public NetworkMessage(MessageType type, params string[] arguments)
        : this(MessageHeaders.ToHeader(type), type, arguments)
    {
    }

    /// <summary>
    /// Creates a message from a raw header, used when parsing unknown traffic
    /// </summary>
    public NetworkMessage(string header, MessageType type, IReadOnlyList<string> arguments)
    {
        Header = header;
        Type = type;
        Arguments = arguments;
    }

    /// <summary>
    /// The wire header as sent or received
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// The resolved message type, Unknown when the header is not recognized
    /// </summary>
    public MessageType Type { get; }

    /// <summary>
    /// The ordered argument list, never null
    /// </summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>
    /// Reads an argument by index, returns an empty string when out of range
    /// </summary>
    public string GetArgument(int index) =>
        index >= 0 && index < Arguments.Count ? Arguments[index] : string.Empty;

    /// <summary>
    /// Builds a message from a known type with no arguments
    /// </summary>
    public static NetworkMessage Create(MessageType type) => new(type);

    /// <inheritdoc />
    public override string ToString()
    {
        if (Arguments.Count == 0)
        {
            return Header;
        }

        return Type is MessageType.Login
            or MessageType.MasterLogin
            or MessageType.CreateAccount
            or MessageType.ModeratorAuth
            or MessageType.GameTokenIssued
            or MessageType.GameTokenValidate
            ? $"{Header}(<redacted>)"
            : $"{Header}({string.Join(", ", Arguments)})";
    }
}
