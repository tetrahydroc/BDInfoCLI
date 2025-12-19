# BDInfoCLI-ng

A command-line Blu-ray disc analysis tool with UHD support, built on .NET 8.

## About

Forked from [zoffline/BDInfoCLI-ng](https://github.com/zoffline/BDInfoCLI-ng), which was based on [UniqProject/BDInfo](https://github.com/UniqProject/BDInfo) and [BDInfoCLI](https://github.com/Tripplesixty/BDInfoCLI).

Original BDInfo source: http://www.cinemasquid.com/blu-ray/tools/bdinfo

This fork migrates from .NET Framework/Mono to native .NET 8, enabling cross-platform support without Mono.

## Download

Pre-built binaries are available on the [Releases](https://github.com/tetrahydroc/BDInfoCLI-ng/releases) page:

| Platform | File |
|----------|------|
| Linux x64 | `BDInfo-linux-x64.tar.gz` |
| Linux ARM64 | `BDInfo-linux-arm64.tar.gz` |
| Windows x64 | `BDInfo-win-x64.zip` |
| macOS x64 (Intel) | `BDInfo-osx-x64.tar.gz` |
| macOS ARM64 (Apple Silicon) | `BDInfo-osx-arm64.tar.gz` |

## Usage

```
Usage: BDInfo <BD_PATH> [REPORT_DEST]

BD_PATH may be a directory containing a BDMV folder or a BluRay ISO file.
REPORT_DEST is the folder the BDInfo report is to be written to. If not
given, the report will be written to BD_PATH. REPORT_DEST is required if
BD_PATH is an ISO file.

Options:
  -h, --help       Print out the options.
  -l, --list       Print the list of playlists.
  -m, --mpls=VALUE Comma separated list of playlists to scan.
  -w, --whole      Scan whole disc - every playlist.
  -v, --version    Print the version.
```

### Examples

```bash
# Display playlists, prompt to select, output report to disc path:
./BDInfo /path/to/disc

# Scan disc and output report to specific folder:
./BDInfo /path/to/disc /path/to/report

# Scan an ISO file (REPORT_DEST required):
./BDInfo /path/to/movie.iso /path/to/report

# Just list playlists without scanning:
./BDInfo -l /path/to/disc

# Scan entire disc non-interactively:
./BDInfo -w /path/to/disc

# Scan specific playlists:
./BDInfo -m 00006.MPLS,00009.MPLS /path/to/disc

# Show version:
./BDInfo -v
```

## Building from Source

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build

```bash
# Build
dotnet build BDInfo/BDInfo.csproj -c Release

# Run
dotnet run --project BDInfo/BDInfo.csproj -- /path/to/disc

# Publish self-contained binary
dotnet publish BDInfo/BDInfo.csproj -c Release -r linux-x64 --self-contained true -o ./publish
```

Replace `linux-x64` with your target runtime:
- `linux-x64`, `linux-arm64`
- `win-x64`
- `osx-x64`, `osx-arm64`

## Docker

A Docker image is also available:

```bash
# Build the image
docker build -t bdinfo .

# Run
docker run --rm -it -v /path/to/disc:/mnt/bd bdinfo /mnt/bd

# With separate report output:
docker run --rm -it -v /path/to/disc:/mnt/bd -v /path/to/report:/mnt/report bdinfo /mnt/bd /mnt/report
```

## License

LGPL-2.1 - See [LICENSE](LICENSE) for details.
