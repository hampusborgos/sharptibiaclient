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

## Outfit colours

Added in `OutfitColors.cs` + `GameRenderer.DrawImage`. Like lighting, this
needed no reverse engineering — everything it uses was already parsed and then
dropped on the floor.

A player sprite in `Tibia.spr` is painted in greys and comes with a second
layer masked in pure yellow, red, green and blue for head, body, legs and feet.
`Tibia.dat` attribute `0x14` is what marks a creature as having one, and it was
already being read (as `GameSprite.ColorTemplate`), and `LookHead`/`LookBody`/
`LookLegs`/`LookFeet` were already being parsed off the wire. `GetImage` picked
layer 0 and ignored the rest, with the template lookup left behind as a
commented-out stub from the C++ client it was ported from. So every player
rendered in the greys of the underlying sprite.

Dyeing is a multiply: each masked pixel becomes the grey times the colour the
mask names. One sprite sheet, every outfit in the game.

The four `Look*` fields are indices into a 133-colour table, not colours. That
table is in neither data file — the client generates it, as nineteen hues at
seven saturation/intensity pairs, with the first column of each pair replaced by
a step down a grey ramp. That is why index 0 is white and 19, 38, 57 ... are
progressively darker greys rather than continuing the hue wheel.

Two details worth keeping:

- **Compositing happens on the CPU, in `OutfitColors.Dye`, which never touches a
  graphics device.** `GameRenderer` only turns the result into a `Texture2D`.
  That keeps the invariant `--verify-seek` relies on, and it means the dye can
  be checked headlessly.
- **Dyed sprites are cached per look, not per creature**, keyed by the two
  sprite ids and the four colour indices. An outfit is worn by everyone wearing
  it, and a recording only ever shows a handful.

## Messages on the map

Added in `SpokenText.cs` + `GameRenderer.DrawSpeech`. Speech stands over the
speaker in yellow, monsters talk in red, and the messages the server sends
about you - what you looked at, what you advanced to, what you were warned
about - stand over your own character in green, white and red.

Which messages belong on the map and which are console lines is decided by
`MessageType`, and the enum already carried the answer in its comments:
`Description` is "green message in game window", `ConsoleBlue` is "blue message
in the console". Channel and private talk carries no position at all, which is
its own answer.

Two things this does not do the way the tile effects do:

- **Text is not held per tile.** Several people can talk from one tile, a
  message about you follows the view rather than a tile, and a monster yelling
  from off screen has no tile on screen to hang from. So it is one list, and
  `DrawSpeech` stacks whatever shares an anchor with the newest line closest to
  the head.
- **Off-screen speakers are clamped to the edge** of the view rather than
  dropped, which is how you notice the yelling in the first place.

The 7.4 speak packet carries the speaker's *name* and a position, but no
creature id - the field the parser reads first in later versions simply is not
there. So there is nothing to attach the text to, and it stays on the tile it
was said from rather than following a walking speaker. Over the few seconds a
line is up, that is where the speaker still is.

Along the way, `MakeColor` was wrong. Tibia's 216-colour palette is six steps of
51 per channel, but the red channel divided by six rather than five and so never
reached zero: pure green came out as `(35, 255, 0)`. Only animated damage text
used it before now.

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
