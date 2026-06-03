# Contributing to dotnet-archive

Thanks for your interest in contributing! This document explains how to build, test, and submit changes.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 8.0, 9.0, and 10.0
- Git
- On Linux/macOS: standard build tools (for the self-extracting archive experiments)

## Building

```bash
git clone https://github.com/lorenzoandreamusto/dotnet-archive.git
cd dotnet-archive
dotnet restore dotnet-archive.sln
dotnet build dotnet-archive.sln -c Release
```

## Testing

```bash
dotnet test tests/DotNetArchive.Tests/DotNetArchive.Tests.csproj
```

Tests are written with xunit + FluentAssertions and run on every commit via GitHub Actions across Windows, Linux, and macOS.

## Project structure

```
src/DotNetArchive.Cli/    The CLI tool (entry point)
  Commands/               System.CommandLine command definitions
  Services/               Core services (export, run, manifest, cache, paths)
  Models/                 Data model + System.Text.Json source-gen
  Platform/               OS-specific file-association implementations
tests/DotNetArchive.Tests/  Unit and integration tests
samples/                  Sample projects to exercise the tool
```

## Submitting a pull request

1. Fork and create a feature branch.
2. Make your changes with tests where appropriate.
3. Ensure `dotnet build` is clean (no warnings).
4. Ensure `dotnet test` is green.
5. Open a pull request with a clear description of the change.

## Code style

The project uses an `.editorconfig` with the dotnet/aspnetcore conventions. File-scoped namespaces, `var` where the type is apparent, and modern C# features are encouraged.

## Commit messages

Use the imperative mood ("Add feature", not "Added feature") and keep the first line under 72 characters.

## Reporting bugs

Open an issue on GitHub with:
- A clear, descriptive title
- The OS and .NET version
- A minimal reproduction
- The expected and actual behavior
