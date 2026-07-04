# Contributing to VNO.Core

`VNO.Core` is the shared protocol, networking and model library used by the Visual Novel Online stack. Changes here can affect `VNO.Client`, `VNO.Server` and `VNO.Master`, so compatibility, clarity and test coverage matter more than speed.

## Prerequisites

- .NET 10 SDK

## Setup

Clone and restore the repository:

```bash
git clone https://github.com/Lemonical/VNO.Core.git
cd VNO.Core
dotnet restore VNO.Core.slnx
```

## Development Notes

When making changes, please keep the following in mind:

- Treat wire protocol changes as cross-repository changes, not local refactors.
- Keep message types, codecs, framers and transport behaviour backward-compatible unless the change intentionally updates the contract.
- Keep shared models stable and predictable.
- Avoid mixing protocol changes with unrelated cleanup.
- Prefer small, focused changes that are easier to review.
- Add comments for non-obvious protocol, framing or transport logic.
- Add or update tests whenever message shapes, framing rules, escaping rules, hash behaviour or shared model behaviour change.

## Testing

Run the full test suite before opening a pull request:

```bash
dotnet test VNO.Core.slnx
```

Add or update tests when changing:

- Protocol constants
- Message types
- Message encoding or decoding
- Message escaping
- Message framing
- TCP transport behaviour
- Shared hash behaviour
- Player stat rules
- Replay parsing
- Master Server protocol message shapes
- Shared models consumed by other `VNO.*` repositories

## Pull Requests

Before opening a pull request, make sure that:

- The project builds successfully.
- The test suite passes.
- Protocol or model changes are documented.
- The pull request explains the compatibility impact clearly.
- Any affected downstream repositories are called out.

Please mention whether corresponding changes are needed in:

- `VNO.Client`
- `VNO.Server`
- `VNO.Master` (private)

Keep protocol contract changes explicit. If a change intentionally breaks compatibility, explain why the break is necessary and which repositories need to be updated with it.

## Issues

Use GitHub issues to report bugs, protocol design concerns or shared model problems.

When reporting a bug, please include:

- What you expected to happen
- What actually happened
- Steps to reproduce the issue
- Relevant logs or test output, if available
- Your operating system
- Your .NET SDK version
- Any affected downstream repository, if known

## License

By contributing to this repository, you agree that your contributions will be licensed under the MIT License that covers this project.
