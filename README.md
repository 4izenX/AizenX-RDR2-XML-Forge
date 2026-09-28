## Requirements

- Windows 10/11 x64
- Red Dead Redemption 2
- RDR2 Manifest Tool installed separately

AizenX does not bundle CodeX, Oodle, Rockstar Games files, or the RDR2 Manifest Tool exporter.

## Build from source

Requires the .NET 8 SDK.

Run:

dotnet publish AizenX.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false

The compiled executable will be generated in the publish output folder.

## Tested RDR2 formats

Archive mode:
- YFT
- YDD
- YTD
- YMT

Loose-file mode with clean standalone RDR2 resources:
- YFT
- YDD
- YTD

## License

AizenX is released under the MIT License.
