# Building and running

The client targets **.NET 10** and **MonoGame 3.8.5.1 (DesktopGL)**. It builds
and runs on macOS (Apple Silicon included), Windows and Linux.

## Toolchain

Any .NET 10 SDK will do. If you install it with the official script rather than
a system package, it lands in `~/.dotnet`, which is not a location the apphost
searches — so the build works but the built binary will not start:

```
You must install .NET to run this application.
```

Export `DOTNET_ROOT` in that case:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

Installing the `.pkg` to `/usr/local/share/dotnet` avoids needing it.

The content pipeline runs as a **project-local** tool, already committed in
`.config/dotnet-tools.json`. Restore it once per clone:

```bash
dotnet tool restore
```

## Game data

The client cannot render anything without Tibia's `.dat` and `.spr` files.
They are not redistributable, so they are not in the repository — `CTC/Data/`
is gitignored. Extract them from an original installer:

```bash
brew install innoextract
innoextract -d /tmp/tibia741 original/installers/tibia741.exe
mkdir -p CTC/Data
cp /tmp/tibia741/app/Tibia.dat /tmp/tibia741/app/Tibia.spr CTC/Data/
```

7.41's data files work with the 7.40 parser. `TibiaGameData.Version` is
hardcoded to `740` and the header parses cleanly: 3134 items, 255 creatures,
25 effects, 15 distance effects. If item counts ever come out absurd, that is
the first thing to check.

`recordings/` is gitignored too. Any 7.40 `.tmv` works; the header starts
`02 00 E4 02`, where `0x02E4` is 740.

## Running

```bash
dotnet build CTC/CTC.csproj
cd CTC/bin/Debug/net10.0
./CTC recordings/Kolskagg.tmv
```

With no argument it plays the first `.tmv` it finds next to the executable.
Both missing-input cases fail with an actionable message rather than a stack
trace.

Playback starts at the beginning, at 1x. `--start MM:SS` and `--speed N` open
somewhere else, which is mostly useful for getting a test to the same place
twice. Everything they do is also on the transport under the game view, and in
`docs/playback.md`.

`--verify-seek` runs the snapshot machinery against a straight play-through and
exits, without opening a window. Run it after changing anything in
`CTC/Client/`.

Paths resolve against `AppContext.BaseDirectory`, **not** the working
directory. This matters: `dotnet run` sets the working directory to the
*project* folder, and a macOS `.app` bundle gets `/`. The original code used
`./Tibia.dat` and only worked because Visual Studio happened to launch from
the output folder.

## Content pipeline

Only two assets, built by `MonoGame.Content.Builder.Task` from
`CTC/Content/Content.mgcb`: `DefaultSkin.bmp` and `StandardFont.spritefont`.

The spritefont points at a bundled `DejaVuSansCondensed-Bold.ttf` rather than
Tahoma, so the build does not depend on machine-installed fonts. Tahoma *is*
present on macOS at `/System/Library/Fonts/Supplemental/Tahoma Bold.ttf`, and
switching `<FontName>` to it gives the exact metrics the 2010 layout was tuned
against — at the cost of a system font dependency. Note the processor matches
on filename and ignores `<Style>`, so it must be `Tahoma Bold`, not `Tahoma`.

`DefaultSkin.bmp` has no alpha and its filler colour is `(200,10,200)`, not
XNA's colour-key magenta, so the colour key is a no-op. If pink blocks ever
appear, set `ColorKeyColor=200,10,200,255` in the `.mgcb`.
