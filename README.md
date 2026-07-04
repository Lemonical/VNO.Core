# Visual Novel Online Core

Shared protocol, networking and model library for the Visual Novel Online stack, built with .NET 10.

## Overview

`VNO.Core` contains the shared protocol and networking foundation used by the Visual Novel Online client, game server and Master Server.

It defines the common contract for:

- Wire-level protocol constants
- Message headers and strongly typed message kinds
- Message encoding, escaping and framing
- TCP client and TCP server abstractions
- Shared models such as server listings, player stats, bans and music tracks

The other `VNO.*` repositories consume this library as a Git submodule so protocol changes remain explicit, reviewable and easy to coordinate across the stack.

## Project Status

This project is currently under active development. It is the shared foundation for the VNO rewrite, so changes here are expected to remain conservative because they can affect every other `VNO.*` repository.

## Features

Current features include:

- Human-readable text protocol with stable message delimiters and escaping
- Streaming message framer for reconstructing complete messages from arbitrary TCP chunks
- Shared `NetworkMessage` model
- Shared `MessageType` enum for protocol-safe routing
- Reusable TCP transport implementations for client and server roles
- Shared models for server listings, player stats, bans and music tracks
- Protocol and transport tests covering encoding, framing, hashing and shared model behaviour

## Requirements

- .NET 10 SDK

## Installation

Clone and restore the repository:

```bash
git clone https://github.com/Lemonical/VNO.Core.git
cd VNO.Core
dotnet restore VNO.Core.slnx
```

If you want to consume the library from another local repository, reference the project directly:

```xml
<ProjectReference Include="path/to/VNO.Core.csproj" />
```

Most VNO repositories consume `VNO.Core` as a Git submodule instead of a direct package dependency. This keeps protocol changes visible in pull requests.

## Usage

The core library is intentionally small and low-level.

A typical message encoding and decoding flow looks like this:

```csharp
using VNO.Core.Protocol;

var outbound = new NetworkMessage(MessageType.InCharacter, "Phoenix", "Objection!");
var wireText = MessageCodec.Encode(outbound);
var parsed = MessageCodec.Decode(wireText.TrimEnd(ProtocolConstants.MessageTerminator));
```

For TCP streams that arrive in chunks, use `MessageFramer`:

```csharp
using VNO.Core.Protocol;

var framer = new MessageFramer();
var messages = framer.Append(receivedBytes);
```

## Testing

Run the test suite:

```bash
dotnet test VNO.Core.slnx
```

The tests cover message encoding, message framing, shared hash behaviour, player stat rules, replay parsing and Master Server protocol message shapes.

## Related Repositories

- [`VNO.Client`](https://github.com/Lemonical/VNO.Client): desktop player client
- [`VNO.Server`](https://github.com/Lemonical/VNO.Server): game server and staff control surface

## Contributing

Because this repository defines cross-process contracts, even small changes can affect the rest of the VNO stack.

See [CONTRIBUTING.md](CONTRIBUTING.md) for workflow and compatibility expectations.

Before opening a pull request:

```bash
dotnet test VNO.Core.slnx
```

If you add or change a message type, update both the implementation and the matching tests in the same change.

If the change requires updates in `VNO.Client`, `VNO.Server` or `VNO.Master`, mention those downstream changes in the pull request.

## Support

Use the [GitHub issue tracker](https://github.com/Lemonical/VNO.Core/issues) for bugs, protocol design discussions and shared model concerns.
