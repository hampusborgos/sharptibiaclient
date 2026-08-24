# Developer documentation

Notes for working on this client. The top level `README.md` describes what the
project is and how the original author designed it; these pages cover what you
need to know to build, run, test and change it today.

| | |
|---|---|
| [building-and-running.md](building-and-running.md) | Toolchain, extracting the Tibia data files, running a recording |
| [testing-interactions.md](testing-interactions.md) | Driving the interface from a script instead of the real mouse |
| [ui-framework.md](ui-framework.md) | How the UI layer works, and the traps in it |
| [port-notes.md](port-notes.md) | What changed moving off XNA, and why |

## Orientation

The client plays back `.tmv` recordings of Tibia 7.4 sessions. There is no
network play and barely any input handling; it is a viewer.

```
CTC/Protocol/   parses the Tibia protocol and the .dat/.spr data files,
                exposing an event per packet type
CTC/Client/     the game state built from those events. No XNA references
                at all, so it can be driven without a graphics device
CTC/Game/       rendering of the world: sprites, lighting, effects
CTC/UI/         the widget framework and the panels built from it
```

The split is worth preserving. `Protocol` and `Client` know nothing about
drawing, which is what makes it possible to fast-forward a recording by
feeding packets through without a renderer attached — `ClientState.ForwardTo`
relies on exactly that.

## Conventions

Source files are **CRLF**, and most carry a **UTF-8 BOM**. This is a 2010
codebase and the whole tree is consistent about it, so tooling that rewrites
files needs to preserve both or every diff becomes the entire file. Check with:

```bash
head -c3 <file> | xxd -p     # efbbbf means BOM present
grep -c $'\r' <file>         # 0 means the CRLFs were lost
```

Naming follows the original: `PascalCase` for locals and parameters as well as
members. Match the surrounding code rather than modern C# conventions.
