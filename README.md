# GameVisionTool

![Build and Test](https://github.com/dmpsyjsn/GameVisionTool/actions/workflows/build-and-test.yml/badge.svg)

A Windows desktop tool for drafting game concepts and generating narrative content — character and
world backstory — with an LLM. Model inference runs locally via
[LlamaSharp](https://github.com/SciSharp/LLamaSharp), CUDA-accelerated.

## What it does

- **Ideas** — capture and manage game concept entries.
- **Backstory** — pick an idea and a local model, write a brief, and generate science-fiction
  backstory prose. The generator keeps a pinned system prompt across a long context and strips
  `<think>` reasoning blocks from the output before it reaches the UI.
- **Settings** — register local `.gguf` model files, and API keys for cloud LLM providers.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and, for the desktop
app, the MAUI workload (`dotnet workload install maui-windows`). Windows only.

```bash
# Build the whole solution
dotnet build GameVisionTool.sln

# Run the desktop app
dotnet run --project GameVisionTool.UI.Maui

# Run the tests (see note below — not "dotnet test")
dotnet build GameVisionTool.IntegrationTests
dotnet exec GameVisionTool.IntegrationTests/bin/Debug/net10.0/GameVisionTool.IntegrationTests.dll
```

> The test project runs on `xunit.v3` over Microsoft.Testing.Platform. On the current .NET 10 SDK,
> `dotnet test` reports "Zero tests ran" and exits without ever launching the app; running the built
> assembly directly is the reliable path, and it's what CI uses.

Local inference needs a `.gguf` model file, registered via the Settings page, and an NVIDIA GPU
(the LlamaSharp backend targets CUDA 12).

## Architecture

A CQRS-style pipeline: UI pages dispatch commands and queries through `IProcessCommand` /
`IProcessQuery`, which resolve handlers by reflection and run them through a decorator chain
(logging → validation → transaction → unit-of-work) built with
[Scrutor](https://github.com/khellang/Scrutor). Persistence is [LiteDB](https://www.litedb.org/), an
embedded document database, isolated behind its own project so the rest of the solution never
compiles against its API directly.

| Project | Role |
|---|---|
| `GameVisionTool.Common.Domain` | Core abstractions — `ICommand`, `IQuery<T>`, `Entity<TKey>`, `IDataStore`, `Result` |
| `GameVisionTool.Common.Validation` | FluentValidation decorator for command handlers |
| `GameVisionTool.Messages` | CQRS command and query DTOs |
| `GameVisionTool.Logic` | Handlers, domain entities, cross-cutting decorators, DI composition root |
| `GameVisionTool.Persistence.LiteDb` | The only project referencing LiteDB |
| `GameVisionTool.UI.Maui` | .NET MAUI desktop UI (Windows) |
| `GameVisionTool.Integration.LlamaSharp` | Local LLM inference, CUDA 12 backend |
| `GameVisionTool.Integration.GoogleGemini` | Gemini API client for a planned cloud inference path (not yet wired to the app) |
| `GameVisionTool.IntegrationTests` | xunit.v3 tests against a real in-memory `LiteDatabase` |

Further implementation detail — the persistence boundary, the decorator chain, the `Result` pattern,
package hygiene — is documented in `source/GameVisionTool/CLAUDE.md`.

## Status

Active development. The Settings page can register a Gemini API key as an `ApiLlmType`, but no
handler calls into `GameVisionTool.Integration.GoogleGemini` yet — backstory generation is
local-model-only for now.
