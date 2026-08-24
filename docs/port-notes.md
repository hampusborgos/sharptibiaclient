# Port notes

The project was written in 2010 against XNA Game Studio 4.0 on .NET Framework
4.0, x86 Windows only. XNA was discontinued in 2013 and never ran on macOS.
This records what changed and why, so the reasoning is not lost.

## Why it was an easy port

The XNA surface actually in use was tiny: `Framework`, `.Graphics`, `.Input`,
`.Content` and nothing else. No shaders, no `.fx` files, no 3D, no audio, no
P/Invoke, no registry, no `BinaryFormatter`. Rendering is entirely
`SpriteBatch`. All the Windows-only code was a single self-contained WinForms
debug window with one call site.

The bulk of the work was the build system, not the code.

## What changed

- `RC.sln`, `CTC.csproj` and `CTCContent.contentproj` replaced by one SDK-style
  project on `net10.0` + `MonoGame.Framework.DesktopGL`.
- The old csproj's dead references (`EnvDTE*`, `mshtml`, `PresentationCore`,
  `System.Xaml`, `UIAutomation*`, `WindowsBase`, `Accessibility`) all went. No
  code referenced any of them.
- `DebugWindow.*` deleted — the only WinForms, `System.Drawing` and `mshtml`
  usage in the tree. Its log output goes to stdout now.
- `using Microsoft.Xna.Framework.GamerServices` removed; that namespace does
  not exist in MonoGame. The unused `.Audio` and `.Media` usings do exist and
  were harmless.
- The `#if WINDOWS || XBOX` guard around `Main` removed — without `WINDOWS`
  defined it compiled the entry point away entirely.
- Hardcoded `./Tibia.dat`, `./Tibia.spr`, `./Test.tmv` replaced by `AppPaths`,
  resolving against `AppContext.BaseDirectory`.
- Frame rate capped at 120 rather than running uncapped.

### The embedded resource name is load-bearing

`TibiaGameProtocol` looks its packet map up by the literal string
`"CTC.TibiaProtocolMap.xml"`. SDK-style projects do not auto-include `.xml` as
embedded resources, and a mismatch fails as a `NullReferenceException` deep
inside the parser rather than anything obvious. The csproj pins it:

```xml
<EmbeddedResource Include="TibiaProtocolMap.xml"
                  LogicalName="CTC.TibiaProtocolMap.xml" />
```

## Bugs the port surfaced

These were all latent in the original; the first is what actually stopped
playback working.

**Short reads on the gzip stream.** `TibiaMovieStream` (since replaced by
`TibiaMovieRecording`, which kept the fix) ignored the return value
of every `Stream.Read`. A stream is only obliged to return *some* bytes, and
`.tmv` files are read through a `GZipStream`, which returns short reads at its
internal buffer boundaries. .NET Framework 4.0 happened to always fill the
buffer; .NET 10 does not, so packet lengths were read from the wrong offsets
and the stream desynced on the first chunk. The compiler flags these as
`CA2022` — worth heeding anywhere else they appear.

**Sprite lookups running off the end of `Tibia.spr`.** IDs past the sprite
count seek into the middle of the sprite data and read a nonsense length, and
empty sprites legitimately carry an offset of 0. Both are bounds-checked now.

**A crash inside a crash handler.** The "unknown speak class" diagnostic read
`props["MessageType"]`, which that parser never assigns; `Packet`'s indexer
returns `null` for absent keys, so unboxing it turned an unrecognised message
into a hard crash.

## Lighting

Added in `LightMap.cs` + `GameRenderer.DrawLights`. Worth knowing: this needed
**no reverse engineering**. Tibia's lighting is data, and all of it was already
being parsed and discarded — `Tibia.dat` carries `LightLevel`/`LightColor` per
item, `ClientCreature` has a `LightSource`, and the protocol map already routed
`WorldLight` (0x82) and `CreatureLight` (0x8D).

Two wiring bugs had to be fixed first: `OnCreatureLight` was registered against
`Protocol.CreatureTurn`, and `WorldLight` was never subscribed at all.

How it renders:

- Light sources accumulate additively into their own render target, cleared to
  the ambient colour.
- That buffer is multiplied over the finished scene, so unlit areas darken and
  lit areas keep their colour and take the light's hue.
- Colours are an index into Tibia's 216-entry palette, six steps per channel.
- **Below the sea floor the world light is ignored entirely.** Underground is
  always dark regardless of surface time of day. That is client-side behaviour,
  not something the server sends.

Creature names and health bars are drawn in the foreground pass, so they stay
readable outside the lit radius.

## Still outstanding

- **The player has no name.** Nothing in the 7.4 protocol ever sends it for
  your own character - the real client knew it from the login screen - so the
  game tab reads `(Kolskagg.tmv)` with an empty name in front of it.
- **The chat panel is dead.** `ActiveViewportChanged += Chat.OnNewState` is
  commented out in `GameDesktop.CreatePanels` — in the *original* source, not
  as a result of the port. It never receives a viewport, so it shows nothing.
- Walking animation was never implemented (the original README says so). This
  turns out to be load-bearing for backwards playback: with nothing tweened
  between packets, applying reverse steps at 15Hz looks the same as applying
  them every frame. See [playback.md](playback.md).
- Input is view-only beyond the playback transport: the keyboard now reaches
  `GameDesktop.KeyPressed`, but there is no focus system behind it, and the
  `send*` protocol functions are still unused.
- `MinimumChatHeight` is deliberately just the tab strip, because at wide short
  windows the 15x11 game view needs nearly all the height to reach the sidebar.
  Raising it reintroduces letterboxing at those window shapes.
