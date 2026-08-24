# Testing interactions

The interface can be driven from a script instead of the real mouse:

```bash
./CTC recordings/Kolskagg.tmv --input-script drag.txt
```

Use this rather than synthetic OS mouse events. Those move the real cursor and
steal window focus, which makes the machine unusable while a test runs, and
they are unreliable besides — see *Why one step per frame* below.

## Script format

One command per line, `#` for comments.

| Command | Effect |
|---|---|
| `move X Y` | Put the cursor at X Y, in **client** coordinates |
| `down` / `up` | Press or release the left button (`down right` for the right) |
| `click` | Expands to `down` then `up` on consecutive frames |
| `keydown NAME` / `keyup NAME` | Hold or release a key, named as in XNA's `Keys` |
| `key NAME` | Expands to `keydown` then `keyup`, for the same reason as `click` |
| `wait MS` | Hold still for MS milliseconds |
| `shot PATH` | Save the next drawn frame as a png |
| `quit` | Stop the game |

```
wait 500
move 953 10
click
wait 400
shot /tmp/opened.png

move 1188 26         # grab a window by its title bar
down
wait 200
move 1012 330        # drag it into the other column
wait 200
up
wait 600
shot /tmp/dropped.png
quit
```

## Coordinates are client-relative

`Mouse.GetState()` reports client coordinates, and `shot` writes the back
buffer, so both use the same space: **(0,0) is the top-left of the drawable
area, below the title bar**.

Do not take coordinates off a `screencapture` of the window — that includes the
title bar and everything will be about 28px out. Take them off a `shot` image
instead, which is exactly what the client sees.

At the default 1280x800 client size with a two column sidebar: the sidebar
starts at x=920, the toggle row is at y=10, column one is centred on x=1012 and
column two on x=1188.

## Why one step per frame

The UI polls the mouse in `Game.Update` and only reacts when the button state
*differs from the previous poll*. At 120fps that is an 8ms window, so a press
and release delivered together are invisible — which is exactly how synthetic
OS clicks get silently swallowed.

`ScriptedInputSource` therefore consumes exactly one step per frame. Every
state change is guaranteed to be seen. `click` expands to two steps for the
same reason, and a drag needs at least one `move` between `down` and `up`.

## Adding a source

`IInputSource` (`CTC/UI/Framework/InputSource.cs`) is the whole seam:

```csharp
void Update(GameTime Time);
MouseState CurrentMouseState { get; }
KeyboardState CurrentKeyboardState { get; }
string PendingScreenshot { get; set; }
bool Finished { get; }
```

`SystemInputSource` is the real mouse and the default. Swap
`UIContext.Input` before `Game.Run()` to replace it.

Because the UI is entirely polled, this one interface is enough to drive all of
it. There is no event queue to fake.
