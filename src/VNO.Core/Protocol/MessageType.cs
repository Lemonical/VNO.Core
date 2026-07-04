namespace VNO.Core.Protocol;

/// <summary>
/// Known message headers in the VNO protocol
/// </summary>
/// <remarks>
/// These map to the features found in the legacy forms, such as in character
/// chat, music, moderation, and the handshake with the auth server. The header
/// string is what travels on the wire, see <see cref="MessageHeaders"/>
/// </remarks>
public enum MessageType
{
    /// <summary>
    /// A header we do not recognize, kept so unknown traffic does not crash a peer
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Connection handshake and version exchange
    /// </summary>
    Hello,

    /// <summary>
    /// Login with user name and password
    /// </summary>
    Login,

    /// <summary>
    /// Server reply that login succeeded
    /// </summary>
    LoginAccepted,

    /// <summary>
    /// Server reply that login failed
    /// </summary>
    LoginRejected,

    /// <summary>
    /// Keep alive ping so idle sockets are not dropped
    /// </summary>
    Heartbeat,

    /// <summary>
    /// In character scene message, the core visual novel line
    /// </summary>
    InCharacter,

    /// <summary>
    /// Out of character chat message
    /// </summary>
    OutOfCharacter,

    /// <summary>
    /// Request or broadcast to play a music track
    /// </summary>
    Music,

    /// <summary>
    /// The list of users in an area
    /// </summary>
    UserList,

    /// <summary>
    /// The list of areas a server hosts
    /// </summary>
    AreaList,

    /// <summary>
    /// The list of music tracks a server offers, sent on join
    /// </summary>
    MusicList,

    /// <summary>
    /// The list of characters a server offers for its roster, sent on join
    /// </summary>
    CharacterList,

    /// <summary>
    /// A player claims a roster character, sent from client to server
    /// </summary>
    PickCharacter,

    /// <summary>
    /// The set of currently claimed characters, broadcast so grids grey them out
    /// </summary>
    CharacterTaken,

    /// <summary>
    /// A player asks to move to an area by its index, client to server
    /// </summary>
    JoinArea,

    /// <summary>
    /// A player submits the moderator password to gain staff powers
    /// </summary>
    ModeratorAuth,

    /// <summary>
    /// The server grants moderator status, reply to a good ModeratorAuth
    /// </summary>
    ModeratorGranted,

    /// <summary>
    /// The server refuses moderator status, reply to a bad ModeratorAuth
    /// </summary>
    ModeratorDenied,

    /// <summary>
    /// A countdown timer in seconds, staff start it and the server broadcasts it
    /// </summary>
    Timer,

    /// <summary>
    /// Staff broadcast an image url for every client to display in the scene
    /// </summary>
    StreamImage,

    /// <summary>
    /// Staff force a music stream url onto every client
    /// </summary>
    StreamMusic,

    /// <summary>
    /// Moderator kick command
    /// </summary>
    Kick,

    /// <summary>
    /// Moderator mute command
    /// </summary>
    Mute,

    /// <summary>
    /// Moderator unmute command
    /// </summary>
    Unmute,

    /// <summary>
    /// Moderator ban a user account
    /// </summary>
    Ban,

    /// <summary>
    /// Moderator remove a ban on a user account
    /// </summary>
    Unban,

    /// <summary>
    /// Moderator ban an address
    /// </summary>
    BanIp,

    /// <summary>
    /// Moderator remove a ban on an address
    /// </summary>
    UnbanIp,

    /// <summary>
    /// Moderator mute every non staff player in the moderator's area
    /// </summary>
    MassMute,

    /// <summary>
    /// Moderator unmute every player in the moderator's area
    /// </summary>
    MassUnmute,

    /// <summary>
    /// Moderator grant a player permission to change the music
    /// </summary>
    DjOn,

    /// <summary>
    /// Moderator revoke a player's permission to change the music
    /// </summary>
    DjOff,

    /// <summary>
    /// Moderator lock the moderator's current area so others cannot enter
    /// </summary>
    LockRoom,

    /// <summary>
    /// Moderator unlock the moderator's current area
    /// </summary>
    UnlockRoom,

    /// <summary>
    /// Moderator toggle a player's isolation, argument 0 is the target player id.
    /// An isolated player's chat only echoes back to themselves
    /// </summary>
    Isolate,

    /// <summary>
    /// Moderator lookup request, argument 0 is the kind (char, user, ip, roomip)
    /// and argument 1 is the target, the server replies with StaffLookupResult
    /// </summary>
    StaffLookup,

    /// <summary>
    /// Server reply to a StaffLookup, argument 0 is the human readable result
    /// </summary>
    StaffLookupResult,

    /// <summary>
    /// Animator command to change a hit point or mana value
    /// </summary>
    StatChange,

    /// <summary>
    /// Animator command to grant an inventory item
    /// </summary>
    GiveItem,

    /// <summary>
    /// A human readable notice shown to the user
    /// </summary>
    Notice,

    // The verbs below are spoken between a game client or game server and the
    // central master (auth and listing) service. They were ported from the
    // legacy VNO master so the new master speaks one consistent protocol

    /// <summary>
    /// A peer announces its role and application version to the master
    /// </summary>
    VersionCheck,

    /// <summary>
    /// Master prompt sent on connect asking the peer to identify itself
    /// </summary>
    ConnectionPrompt,

    /// <summary>
    /// Master reply that the version check passed, carries the message of the day
    /// </summary>
    VersionAccepted,

    /// <summary>
    /// Master reply that the version check failed
    /// </summary>
    VersionRejected,

    /// <summary>
    /// Master reply that the peer address is globally banned
    /// </summary>
    AddressBanned,

    /// <summary>
    /// Request to create a new account with a user name and password
    /// </summary>
    CreateAccount,

    /// <summary>
    /// Master reply that the account was created
    /// </summary>
    AccountCreated,

    /// <summary>
    /// Master reply that the account name is already taken
    /// </summary>
    AccountTaken,

    /// <summary>
    /// Master reply that the supplied account details were invalid
    /// </summary>
    AccountInvalid,

    /// <summary>
    /// Master reply that the account is banned
    /// </summary>
    AccountBanned,

    /// <summary>
    /// Account login request, distinct from the in game LOGIN used by the game server
    /// </summary>
    MasterLogin,

    /// <summary>
    /// Master reply that login succeeded, carries the account name
    /// </summary>
    LoginGranted,

    /// <summary>
    /// Master reply that login failed because the credentials were wrong
    /// </summary>
    LoginDenied,

    /// <summary>
    /// Keep alive the master sends to every peer, distinct from the in game PING
    /// </summary>
    MasterHeartbeat,

    /// <summary>
    /// Client request for the current list of public servers
    /// </summary>
    RequestServers,

    /// <summary>
    /// Master reply describing one server in the public list
    /// </summary>
    ServerEntry,

    /// <summary>
    /// A game server publishes itself to the master so it appears in the list
    /// </summary>
    RegisterServer,

    /// <summary>
    /// A game server asks the master to resolve and ban check an address
    /// </summary>
    CheckIp,

    /// <summary>
    /// Master reply that an address is clear, carries the resolved user name
    /// </summary>
    IpClear,

    /// <summary>
    /// Master reply that an address is banned, carries the resolved user name
    /// </summary>
    IpBanned,
}
