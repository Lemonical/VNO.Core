namespace VNO.Core.Protocol;

/// <summary>
/// Wire level constants for the VNO text protocol
/// </summary>
/// <remarks>
/// VNO uses an Attorney Online style framing where each message is a header
/// followed by arguments and ends with a terminator. Arguments are separated by
/// a field delimiter. This keeps the format human readable for debugging
/// </remarks>
public static class ProtocolConstants
{
    /// <summary>
    /// Separates the message header from its arguments and arguments from each other
    /// </summary>
    public const char FieldDelimiter = '#';

    /// <summary>
    /// Marks the end of one message inside the byte stream
    /// </summary>
    public const char MessageTerminator = '%';

    /// <summary>
    /// Encoding used on the wire, fixed so both ends always agree
    /// </summary>
    public static readonly System.Text.Encoding WireEncoding = System.Text.Encoding.UTF8;

    /// <summary>
    /// Default TCP port a game server listens on for players
    /// </summary>
    public const int DefaultGameServerPort = 16789;

    /// <summary>
    /// TCP port the server uses to accept game clients in the legacy build
    /// </summary>
    public const int LegacyServerListenPort = 6541;

    /// <summary>
    /// TCP port of the central auth and listing service
    /// </summary>
    public const int AuthServerPort = 6543;

    /// <summary>
    /// Largest single message we will accept, guards against runaway buffers
    /// </summary>
    public const int MaxMessageBytes = 1024 * 1024;

    /// <summary>
    /// Application version shown on the login screen and reported to the auth server
    /// </summary>
    /// <remarks>
    /// The legacy client drew this in label_version on groupbox_login. Kept here so
    /// the client and server agree on one value instead of a magic string in a view
    /// </remarks>
    public const string ClientVersion = "1.0";
}
