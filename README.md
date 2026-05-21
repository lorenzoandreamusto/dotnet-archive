# dotnet-archive

`dotnet-archive` is a lightweight .NET global tool designed to package and run *framework-dependent* .NET applications as a single-file portable archive (analogous to Java's `.jar` format) with a `.dna` extension.

Unlike native .NET "Single File" publishing (`PublishSingleFile=true`) which produces platform-specific native binaries (e.g., Windows x64, Linux ARM64), `dotnet-archive` keeps compiled bytecode platform-independent. This allows a single compiled archive to be distributed and run on any operating system (Windows, macOS, Linux) with a compatible .NET runtime.

---

## How It Works

### 1. Packaging (`export`)
* Compiles your project in `Release` mode using `dotnet publish` as a platform-agnostic (framework-dependent) build.
* Injects a lightweight `manifest.json` file inside the publish directory to declare the main entry point assembly (`.dll`).
* Packages all publishing output assets into a single ZIP archive, renamed with the `.dna` extension.

### 2. Execution (`run`)
* Calculates the SHA-256 hash of the target `.dna` file to establish a unique version identity.
* Extracts the archive to a dedicated caching directory (`%TEMP%/dotnet-archive-cache/<hash>`) only if it has not been run or modified before.
* Inspects `manifest.json` to find the entry point.
* Launches the application by spawning a child process running `dotnet <entrypoint>.dll` and forwards all user-provided command-line arguments.

---

## Installation

Install the tool globally from NuGet.org using the .NET CLI:

```bash
dotnet tool install -g dotnet-archive
```

Once installed, the .NET CLI integrates it natively. You can run the tool by simply typing `dotnet archive`.

---

## Usage & Commands

The tool provides two main interfaces: **Export** (defaulting to the root command) and **Run**.

### 1. Export (Default Command)
Compiles your .NET project and packs it into a single `.dna` archive.

#### Syntax
```bash
dotnet archive [project] [options]
```
*or explicitly:*
```bash
dotnet archive export [project] [options]
```

* **`[project]`** *(Optional)*: The path to a `.csproj` file or a directory containing a single `.csproj`. If omitted, the tool automatically targets the current terminal directory.
  * *Note*: If a solution file (`.sln`/`.slnx`) is detected instead of a `.csproj`, the tool will request a specific project path.

#### Options
* `-o`, `--output <file>`: Specifies the destination path for the `.dna` output file. If omitted, the archive is created in the current directory, named after the project (e.g., `AppName.dna`).

#### Examples
```bash
# Package the project in the current folder (default output is AppName.dna)
dotnet archive

# Explicitly call export for a specific project folder
dotnet archive export ./src/MyConsoleApp

# Export and define a custom output path
dotnet archive ./src/MyConsoleApp -o /backups/compiled-app.dna
```

---

### 2. Run
Executes a packaged `.dna` archive or compiles and boots a project on the fly for development/testing.

#### Syntax
```bash
dotnet archive run [archive] [-- application_arguments]
```

* **`[archive]`** *(Optional)*:
  * If pointing to a `.dna` archive, the tool extracts and executes it.
  * If pointing to a `.csproj` file or a project directory, the tool automatically performs a temporary export and runs it immediately (similar to `dotnet run`).
  * If omitted, it defaults to running the project in the current terminal directory.
* **`-- application_arguments`**: Passes trailing parameters straight into the nested application. The `--` separator prevents `dotnet-archive` from consuming application-specific options.

#### Examples
```bash
# Run a pre-packaged .dna archive
dotnet archive run app.dna

# Run an archive and forward arguments to it
dotnet archive run app.dna -- --verbose --port 8080

# Build and run the project in the current directory on the fly
dotnet archive run

# Build and run a specific project folder on the fly
dotnet archive run ./src/MyConsoleApp
```