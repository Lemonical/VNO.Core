using VNO.Core.Protocol;

namespace VNO.Core;

/// <summary>
/// Parses a recorded replay line back into a network message
/// </summary>
/// <remarks>
/// Replay files record scene traffic one line per event, pipe delimited. An in
/// character line is "IC|name|text" and an out of character line is "OOC|text".
/// Playing a replay turns each line back into the message it came from so it can
/// be sent to the room again
/// </remarks>
public static class ReplayLine
{
    /// <summary>
    /// Parses a replay line into a message, null when the line is not recognized
    /// </summary>
    public static NetworkMessage? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var parts = line.Split('|');
        switch (parts[0])
        {
            case "IC" when parts.Length >= 3:
                return new NetworkMessage(MessageType.InCharacter, parts[1], parts[2]);
            case "OOC" when parts.Length >= 2:
                return new NetworkMessage(MessageType.OutOfCharacter, parts[1]);
            default:
                return null;
        }
    }
}
