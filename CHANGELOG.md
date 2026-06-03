# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

### Fixed

## [1.1.0] - 2026-06-03

### Added
- **`version` command** — print the tool version.
- **`unregister` command** — remove the `.dna` file association.
- **`info <file.dna>` command** — inspect an archive (manifest, SHA-256, size, file list, JSON output).
- **`verify <file.dna>` command** — verify an archive is intact and has a valid manifest.
- **`cache` subcommand** — `list`, `clean [--older-than N] [--keep N]`, `set-dir <path>` for managing the on-disk cache.
- **`publish` command** — alias of `export`.
- **`--self-contained --runtime <rid>`** — bundle a portable .NET runtime into the archive; supports all official RIDs (`win-x64`, `win-x86`, `win-arm64`, `linux-x64`, `linux-musl-x64`, `linux-arm64`, `linux-musl-arm64`, `osx-x64`, `osx-arm64`).
- **`--compression <Optimal|Fastest|NoCompression>`** option for `export`.
- **`--include <glob>` / `--exclude <glob>`** options for `export` to filter archive contents.
- **`--manifest-field KEY=VALUE`** option for `export` to add custom fields to the manifest.
- **`--no-cache`, `--env KEY=VALUE`, `--working-dir <path>`** options for `run`.
- **Per-platform file association**: Windows registry, Linux MIME/desktop, macOS .app bundle.
- **Cache with TTL and max-size policy** (default 30 days, 5 GB).
- **Multi-TFM build** — `net8.0`, `net9.0`, `net10.0`.
- **Central Package Management** via `Directory.Packages.props`.
- **Source-generated JSON** for `Manifest` via `System.Text.Json` source generation.
- **Deterministic builds**, source link, documentation generation.
- **Comprehensive test suite**: 32 tests (unit + integration + RID smoke) passing on all 3 TFMs.
- **CI matrix** on Windows, Linux, macOS for build + test + smoke + NuGet release.
- **Dependabot** for NuGet and GitHub Actions updates.
- **Sample projects** (`HelloWorld`, `HelloWorld.SelfContained`, `MultiProject`).
- **`CONTRIBUTING.md`**, **`LICENSE`**, **`README.md`** (English), **`CHANGELOG.md`**.

### Changed
- **Refactored architecture**: `src/DotNetArchive.Cli/{Commands, Services, Models, Platform}` with a clean separation of concerns.
- **i18n**: all CLI descriptions and messages are now in English (consistent with the README).
- **Tool path resolution** uses `Environment.ProcessPath` → `dotnet tool list -g` → default `~/.dotnet/tools/` (instead of the hard-coded default).
- **stdio streaming**: build output and app stdout/stderr are streamed in real time.

### Fixed
- `using Microsoft.Win32;` is now platform-guarded so the project compiles on Linux/macOS.
- `ProcessStartInfo.ArgumentList` replaces manual quoting for safe argument passing.
- `mainDll` detection uses the project's `<AssemblyName>` instead of the unreliable runtimeconfig glob.
- `try/finally` cleanup of the temp publish dir on success and failure.
- stdin is now propagated to the spawned application.
- Linux registration is no longer fatal when `update-mime-database` / `update-desktop-database` are missing.

## [1.0.0] - 2026-06-03

### Added
- Initial release
- `export`, `run`, and `register` commands
- `.dna` archive format

[Unreleased]: https://github.com/lorenzoandreamusto/dotnet-archive/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/lorenzoandreamusto/dotnet-archive/releases/tag/v1.1.0
[1.0.0]: https://github.com/lorenzoandreamusto/dotnet-archive/releases/tag/v1.0.0
