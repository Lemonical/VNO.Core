# Contributing to VNO.Core

Core changes can affect every VNO process. Favor compatibility, bounded behavior, explicit contracts and focused tests over convenience for one consumer.

## Setup

```bash
git clone https://github.com/Lemonical/VNO.Core.git
cd VNO.Core
dotnet restore VNO.Core.slnx
dotnet build VNO.Core.slnx
dotnet test VNO.Core.slnx
```

The only prerequisite is the .NET 10 SDK. Transport integration tests may bind local loopback ports.

## Change guidelines

- Treat message types, delimiters, escaping, framing, limits, paths, and subprotocols as public contracts.
- Preserve wire compatibility unless a coordinated version transition is intentional and documented.
- Keep models deterministic and avoid application-specific behavior in Core.
- Bound untrusted messages, queues, and resource use; validate at protocol boundaries.
- Preserve UTF-8 decoder state across stream reads and do not assume one read equals one message.
- Propagate cancellation and avoid blocking asynchronous I/O.
- Keep TCP and WebSocket behavior equivalent where their transport semantics allow it.
- Do not add application UI, persistence, or deployment policy to this library.

## Testing

Run `dotnet test VNO.Core.slnx` and add focused tests for any change to:

- Protocol constants, message kinds, encoding, decoding, or escaping
- Incremental framing, partial UTF-8, multiple messages, or malformed input
- TCP/WebSocket connection, health/readiness, TLS options, backpressure, or cancellation
- Authentication/game message limits and rejection paths
- Hashing or shared model behavior
- Existing Client, Master, and Server message shapes

Where practical, verify the corresponding application suites against the updated submodule revision.

## Compatibility and documentation

Describe affected messages, old/new behavior, rollout order, and downstream changes in the pull request. A breaking change must identify required updates to VNO.Client, VNO.Master, and VNO.Server.

Keep the README concise. Put the full protocol reference, transport guides, compatibility policy, and examples in the VNO.Core GitHub wiki once it is enabled. Update canonical docs here; do not hand-edit copies in an application's `external/VNO.Core` working tree.

## Pull requests and issues

Pull requests must build, pass tests, include contract tests, explain compatibility impact, and avoid unrelated cleanup. Do not include captured credentials, bearer tokens, private addresses, or sensitive message payloads.

Bug reports should include a minimal reproduction, expected and actual framing/transport behavior, .NET SDK and OS, affected application revisions, and sanitized logs.

## License

By contributing, you agree that your contribution is licensed under this project's [MIT License](LICENSE).
