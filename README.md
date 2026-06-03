# dotnet-archive

`dotnet-archive` is a lightweight .NET global tool that packages framework-dependent .NET applications into a single-file portable archive (analogous to Java's `.jar`) with a `.dna` extension.

Unlike the native .NET "Single File" publishing (`PublishSingleFile=true`) which produces platform-specific native binaries, `dotnet-archive` keeps the compiled bytecode platform-independent. A single archive can be distributed and run on any operating system (Windows, macOS, Linux) that has a compatible .NET runtime installed.

For an offline experience, the tool can also bundle a portable .NET runtime into the archive (`--self-contained`), producing a single `.dna` file that runs on a target OS without a pre-installed .NET runtime.

---

## How it works

### Packaging (`export`)
1. Compiles your project in `Release` mode using `dotnet publish` as a platform-agnostic build.
2. Injects a `manifest.json` file inside the publish directory declaring the entry point assembly, target framework, and metadata.
3. Packages all output assets into a single ZIP archive, renamed with the `.dna` extension.

### Execution (`run`)
1. Computes the SHA-256 of the `.dna` file to establish a unique identity.
2. Extracts the archive to a cache directory (`%TEMP%/dotnet-archive-cache/<hash>`) on first use; subsequent runs reuse the cache.
3. Reads `manifest.json` to find the entry point.
4. Launches the application by spawning `dotnet <entrypoint>.dll` (or directly the bundled apphost if `--self-contained` was used) and forwards command-line arguments.

---

## Installation

Install the tool globally from NuGet.org using the .NET CLI:

```bash
dotnet tool install -g dotnet-archive
```

Then run it with `dotnet archive`.

To install a specific version:

```bash
dotnet tool install -g dotnet-archive --version 1.1.0
```

To update an existing installation:

```bash
dotnet tool update -g dotnet-archive
```

---

## Commands

### `export` (default)
Compiles a .NET project and packs it into a single `.dna` archive.

```bash
dotnet archive [project] [options]
dotnet archive export [project] [options]
```

**Arguments**
- `[project]` — path to a `.csproj` file or a project directory. Defaults to the current directory. Solution files (`.sln`/`.slnx`) are not supported.

**Options**
- `-o`, `--output <file>` — destination path for the `.dna` file. Defaults to `<ProjectName>.dna` in the current directory.
- `-r`, `--runtime <rid>` — target runtime identifier (e.g. `linux-x64`, `win-x64`, `osx-arm64`). Required for `--self-contained`; otherwise optional.
- `--self-contained` — bundle a portable .NET runtime into the archive. Requires `--runtime`.
- `--compression <Optimal|Fastest|NoCompression>` — ZIP compression level. Default `Optimal`.
- `--include <glob>` — file pattern(s) to include. Repeatable. If unset, all files are included.
- `--exclude <glob>` — file pattern(s) to exclude. Repeatable.
- `--manifest-field KEY=VALUE` — add a custom field to the archive manifest. Repeatable.

### `run`
Executes a `.dna` archive, or compiles and runs a project on the fly.

```bash
dotnet archive run [archive] [-- app-args...]
```

**Arguments**
- `[archive]` — path to a `.dna` archive, a `.csproj` file, or a project directory. Defaults to the current directory.
- `[app-args...]` — arguments forwarded to the application (use `--` to separate from tool flags).

**Options**
- `--no-cache` — extract to a temporary directory and clean it up afterwards.
- `--env KEY=VALUE` — environment variable to pass to the application. Repeatable.
- `--working-dir <path>` — working directory for the spawned process.

### `info`
Shows detailed metadata and contents of a `.dna` archive.

```bash
dotnet archive info <file.dna> [--json]
```

### `verify`
Verifies that a `.dna` archive is intact and has a valid manifest.

```bash
dotnet archive verify <file.dna>
```

### `register`
Associates the `.dna` file extension with this tool on the current OS, so `.dna` files can be launched by double-clicking them.

```bash
dotnet archive register
```

**Per-platform behavior**
- **Windows** — creates registry entries under `HKEY_CURRENT_USER\Software\Classes` to associate `.dna` files with the tool.
- **Linux** — installs a custom MIME type (`application/x-dotnet-archive`), creates a desktop entry and a wrapper script.
- **macOS** — generates a `DotNetArchiveRunner.app` bundle with the proper `Info.plist`.

### `unregister`
Reverses `register` on the current OS.

```bash
dotnet archive unregister
```

### `cache`
Manages the on-disk cache of extracted archives.

```bash
dotnet archive cache list
dotnet archive cache clean [--older-than <days>] [--keep <n>]
dotnet archive cache set-dir <path>
```

**Subcommands**
- `list` — list all cached entries with hash, last access, size, and source path.
- `clean` — remove old or excess entries. With no flag, applies the default policy (TTL of 30 days, max 5 GB). With `--older-than N` removes entries older than N days. With `--keep N` keeps the N most-recently-accessed.
- `set-dir <path>` — change the cache directory (default `%TEMP%/dotnet-archive-cache`).

### `version`
Prints the tool version and exits.

```bash
dotnet archive version
```

---

## Examples

```bash
# Package the project in the current folder
dotnet archive

# Export to a custom path
dotnet archive export ./src/MyApp -o /backups/myapp.dna

# Build a self-contained archive (needs --runtime)
dotnet archive export ./src/MyApp --self-contained --runtime linux-x64 -o ./MyApp.dna

# Run an archive
dotnet archive run app.dna

# Run an archive and pass through application arguments
dotnet archive run app.dna -- --verbose --port 8080

# Build and run a project in the current directory on the fly
dotnet archive run

# Show archive contents and metadata
dotnet archive info app.dna

# Verify an archive is intact
dotnet archive verify app.dna

# Inspect the local cache
dotnet archive cache list

# Clean the cache
dotnet archive cache clean --older-than 7
```

---

## Framework-Dependent vs Self-Contained vs Single-File

| Mode                            | Cross-OS? | Needs .NET installed? | Archive size |
|---------------------------------|-----------|----------------------|--------------|
| `dotnet archive export`         | Yes       | Yes (any 8/9/10)     | Small        |
| `dotnet archive export --self-contained --runtime <rid>` | No (single RID) | No | Large |
| `dotnet publish -p:PublishSingleFile=true` (native) | No (single RID + arch) | No | Small/Medium |

---

## Manifest schema

The `manifest.json` inside each `.dna` archive has this shape:

```json
{
  "EntryPoint": "MyApp.dll",
  "ProjectName": "MyApp",
  "Tfm": "net8.0",
  "Version": "1.0.0",
  "Author": "Acme",
  "Rid": null,
  "RuntimeIncluded": false,
  "AppHost": null,
  "ExtraFields": {}
}
```

---

## License

MIT — see [LICENSE](LICENSE).
