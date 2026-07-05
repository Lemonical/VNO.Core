using System.Collections.Generic;

namespace VNO.Core.Protocol;

/// <summary>
/// Maps between <see cref="MessageType"/> values and their wire header strings
/// </summary>
/// <remarks>
/// The header strings are short to keep messages small. The maps are built once
/// and are read only so they are safe to share across threads
/// </remarks>
public static class MessageHeaders
{
    private static readonly IReadOnlyDictionary<MessageType, string> ToWire =
        new Dictionary<MessageType, string>
        {
            [MessageType.Hello] = "HI",
            [MessageType.Login] = "LOGIN",
            [MessageType.LoginAccepted] = "AUTHOK",
            [MessageType.LoginRejected] = "AUTHFAIL",
            [MessageType.Heartbeat] = "PING",
            [MessageType.InCharacter] = "IC",
            [MessageType.OutOfCharacter] = "OOC",
            [MessageType.Music] = "MUSIC",
            [MessageType.UserList] = "USERS",
            [MessageType.AreaList] = "AREAS",
            [MessageType.MusicList] = "MLIST",
            [MessageType.CharacterList] = "CLIST",
            [MessageType.PickCharacter] = "PICK",
            [MessageType.CharacterTaken] = "TAKEN",
            [MessageType.JoinArea] = "JOIN",
            [MessageType.ModeratorAuth] = "MODAUTH",
            [MessageType.ModeratorGranted] = "MODOK",
            [MessageType.ModeratorDenied] = "MODNO",
            [MessageType.Timer] = "TIMER",
            [MessageType.StreamImage] = "IMG",
            [MessageType.StreamMusic] = "STREAM",
            [MessageType.Kick] = "KICK",
            [MessageType.Mute] = "MUTE",
            [MessageType.Unmute] = "UNMUTE",
            [MessageType.Ban] = "BAN",
            [MessageType.Unban] = "UNBAN",
            [MessageType.BanIp] = "IPBAN",
            [MessageType.UnbanIp] = "IPUNBAN",
            [MessageType.MassMute] = "MMUTE",
            [MessageType.MassUnmute] = "MUNMUTE",
            [MessageType.DjOn] = "DJON",
            [MessageType.DjOff] = "DJOFF",
            [MessageType.LockRoom] = "LOCK",
            [MessageType.UnlockRoom] = "UNLOCK",
            [MessageType.Isolate] = "ISOLATE",
            [MessageType.StaffLookup] = "LOOKUP",
            [MessageType.StaffLookupResult] = "LOOKRES",
            [MessageType.StatChange] = "STAT",
            [MessageType.GiveItem] = "ITEM",
            [MessageType.SceneEffect] = "EFFECT",
            [MessageType.RoomPolicy] = "POLICY",
            [MessageType.HideSelf] = "HIDE",
            [MessageType.CheckInventory] = "CHKINV",
            [MessageType.Notice] = "NOTICE",

            // Master service auth and listing verbs, ported one to one from the
            // legacy VNO master so the strings match the original wire exactly.
            // AddressBanned and AccountBanned share VNOBD as in the original, the
            // master only ever sends it so the reverse lookup never needs to split them
            [MessageType.VersionCheck] = "VER",
            [MessageType.ConnectionPrompt] = "CV",
            [MessageType.VersionAccepted] = "VEROK",
            [MessageType.VersionRejected] = "VERPB",
            [MessageType.AddressBanned] = "VNOBD",
            [MessageType.AccountBanned] = "VNOBD",
            [MessageType.MasterLogin] = "CO",
            [MessageType.LoginGranted] = "VNAL",
            [MessageType.LoginDenied] = "VNODE",
            [MessageType.MasterHeartbeat] = "HB",
            [MessageType.CreateAccount] = "CA",
            [MessageType.AccountCreated] = "OKCR",
            [MessageType.AccountTaken] = "NOKCR",
            [MessageType.AccountInvalid] = "INVCHAR",
            [MessageType.RequestServers] = "RPS",
            [MessageType.ServerEntry] = "SDP",
            [MessageType.RegisterServer] = "RequestPub",
            [MessageType.CheckIp] = "CHIP",
            [MessageType.IpClear] = "OKAY",
            [MessageType.IpBanned] = "SUBAN",
        };

    private static readonly IReadOnlyDictionary<string, MessageType> FromWire = BuildReverse();

    /// <summary>
    /// Gets the wire header for a message type
    /// </summary>
    public static string ToHeader(MessageType type) =>
        ToWire.TryGetValue(type, out var header) ? header : "UNKNOWN";

    /// <summary>
    /// Resolves a wire header into a message type, returns Unknown when not found
    /// </summary>
    public static MessageType FromHeader(string header) =>
        FromWire.TryGetValue(header, out var type) ? type : MessageType.Unknown;

    private static Dictionary<string, MessageType> BuildReverse()
    {
        var map = new Dictionary<string, MessageType>(System.StringComparer.Ordinal);
        foreach (var pair in ToWire)
        {
            map[pair.Value] = pair.Key;
        }

        return map;
    }
}
