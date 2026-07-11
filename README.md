<p align="center">
  <img src="docs/assets/vno-icon.png" width="96" alt="Visual Novel Online icon">
</p>

<h1 align="center">Visual Novel Online Core</h1>

<p align="center">Shared protocol, models, framing, and network transports for the VNO stack.</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/"><img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/github/license/Lemonical/VNO.Core"></a>
  <a href="https://github.com/Lemonical/VNO.Core/commits/main"><img alt="Last commit" src="https://img.shields.io/github/last-commit/Lemonical/VNO.Core/main"></a>
  <a href="https://github.com/Lemonical/VNO.Core/issues"><img alt="Open issues" src="https://img.shields.io/github/issues/Lemonical/VNO.Core"></a>
</p>

`VNO.Core` defines the cross-process contract used by Client, Master, and Server. It contains the human-readable message protocol, escaping and streaming framing, shared models, and interchangeable TCP and WebSocket transports.

> [!IMPORTANT]
> Core is under active development. It is consumed as a Git submodule by the VNO applications and is not currently published as a NuGet package. Protocol changes must be coordinated across every affected repository.

## Features

- Stable protocol constants, typed message kinds, encoding, escaping, and decoding
- Stateful stream framing that safely reconstructs messages across arbitrary network reads
- TCP client/server implementations
- WebSocket/WSS client and Kestrel-hosted server implementations
- WebSocket upgrade, health, readiness, subprotocol, keepalive, queue, and size-limit support
- Shared server-listing, player-stat, ban, music, and other protocol models
- Separate bounded message limits for authentication and gameplay traffic
- Tests for codecs, framing, transports, hashes, models, and compatibility behavior

## Quick start

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Install, build, and test

```bash
git clone https://github.com/Lemonical/VNO.Core.git
cd VNO.Core
dotnet restore VNO.Core.slnx
dotnet build VNO.Core.slnx
dotnet test VNO.Core.slnx
```

VNO applications normally consume Core as a submodule and project reference. For another local project:

```xml
<ProjectReference Include="path/to/VNO.Core.csproj" />
```

## Minimal usage

```csharp
using VNO.Core.Protocol;

var outbound = new NetworkMessage(MessageType.InCharacter, "Phoenix", "Objection!");
var wireText = MessageCodec.Encode(outbound);
var parsed = MessageCodec.Decode(
    wireText.TrimEnd(ProtocolConstants.MessageTerminator));
```

Use `MessageFramer` for bytes arriving in arbitrary chunks:

```csharp
var framer = new MessageFramer();
var messages = framer.Append(receivedBytes);
```

The complete protocol, framing, TCP/WebSocket, compatibility and transport-configuration guides belong in the VNO.Core GitHub wiki once it is enabled.

## Build and publish

```bash
dotnet build VNO.Core.slnx -c Release
dotnet test VNO.Core.slnx
dotnet publish src/VNO.Core/VNO.Core.csproj -c Release -o ./publish/core
```

The publish command creates framework-dependent output for local use; it does not create a supported package.

## Consumers

- [VNO.Client](https://github.com/Lemonical/VNO.Client) - desktop player
- [VNO.Master](https://github.com/Lemonical/VNO.Master) - authentication and server directory
- [VNO.Server](https://github.com/Lemonical/VNO.Server) - game host and administration

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before proposing a contract change. Use the [issue tracker](https://github.com/Lemonical/VNO.Core/issues) for protocol and transport problems; identify every known downstream consumer and never post credentials or sensitive traffic captures.

## License

VNO.Core is licensed under the [MIT License](LICENSE).
