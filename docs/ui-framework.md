# The UI framework

`CTC/UI/Framework/` is a small hand-rolled widget toolkit: a view tree, skinned
drawing, polled mouse input. It works, but several of its rules are implicit
and cost real debugging time to rediscover. They are written down here.

## Geometry

Four rectangles, and mixing them up is the most common mistake:

| | |
|---|---|
| `Bounds` | Position and size **relative to the parent's `Bounds` origin** |
| `ClientBounds` | `Bounds` inset by `SkinPadding` and `Padding` — the area children live in |
| `FullBounds` | `Bounds` grown by `Margin` — what a parent's layout reserves |
| `ScreenBounds` | Absolute, accumulated down the tree. What you draw and hit-test with |

`SkinPadding` is derived from the skin element, so it changes with
`ElementType`. A view's own `Bounds.X` is *not* relative to its parent's
`ClientBounds` — layout code has to add `ClientBounds.Left` itself, which most
of it does.

## Layout

`UIView.Update` calls `LayoutSubviews()` on **any view whose `NeedsLayout` is
set**, wherever it is in the tree. A parent does not get the chance to restore
its child's size first.

That single fact is behind a whole class of bugs. If a view derives its size
from its content during layout, the next pass starts from that derived size
rather than the size the parent intended, and the result drifts a little
further every frame.

`Autoresizable` means *my parent sizes me*. A view with it set does not resize
itself during layout.

### UIStackView

Stacks children along one axis, wrapping into rows or columns when `Overflow`
is set. The rule that matters:

> **An overflowing stack must not overwrite the axis it wraps on.**

A horizontal stack wraps based on its width, a vertical one on its height. That
axis is an *input*. The other axis is the *result* and can be written back.
Overwriting the input axis with the content size means the next pass sees less
room than the last and wraps progressively earlier — a row at a time until it
collapses. This is why `Columns = 2` never actually worked in the original
sidebar.

Non-overflowing stacks still size to content on both axes, which is what
`UIFrame.ContentView` relies on.

## Mouse

Input is **polled**, not evented. `Game.Update` reads the mouse each frame and
calls `Desktop.MouseLeftClick` only when the button state *differs from the
previous frame*. Nothing is queued: a press and release in the same frame never
happened. See [testing-interactions.md](testing-interactions.md).

Move events only reach a view that has **captured** the mouse:

```csharp
CaptureMouse();     // asks the current holder to give it up, may fail
ReleaseMouse();     // always succeeds
```

`UIContext.MouseFocusedPanel` holds the capture, and `GameDesktop` routes
events straight to it when set.

`MouseLost()` is called on the previous holder when someone else captures.
`UIFrame` raises `FrameStoppedMoving` from it — **so that event fires whenever
focus changes, not only at the end of a drag.** Handlers must tolerate being
called with no drag in progress.

## Reparenting mid-drag

`UIFrame` records the anchor a drag is measured from when the drag starts, then
each move recomputes `Bounds` from that anchor and clamps against the *current*
parent's `ClientBounds`.

So if you reparent a frame mid-drag — as the sidebar does, lifting a window out
of its column onto the sidebar so it draws on top — moving `Bounds` alone is not
enough. The anchor is still in the old parent's coordinates, and the clamp is
applied before any correction you make afterwards, which pins the frame to
wherever it started. The symptom is a window that can be dragged one way but
never back.

Use `UIFrame.OffsetWhileDragging(dx, dy)`, which moves the frame and its anchor
together.

## Drawing

Each view owns its own `SpriteBatch` and calls `Begin`/`End` inside its parent's
open batch. Nested batches are fine because the guard is per-instance and
deferred sorting flushes at `End`.

`GameCanvas` additionally switches render targets while ancestor batches are
open. That survives OpenGL only because `RenderTargetUsage.PreserveContents` is
forced on the back buffer in `Game.PrepareDevice`. If the game view ever renders
black while the rest of the UI is fine, that is the first place to look.

Scissor rectangles are set between `Begin` and the draw calls
(`UIView.BeginDraw`). MonoGame applies device state at flush time for deferred
batches, so a mid-batch change can apply to the whole batch.

`UISkin.DrawBackground` and `DrawBox` take an optional tint, which is how
`UIDropPlaceholder` renders a real frame at half alpha rather than an
approximation of one.

## The sidebar

`GameSidebar` is the most involved thing built on the framework.

- Each column is its own vertical `UIStackView` that **owns** its windows. With
  a single flowing stack, which column a window landed in was only a side
  effect of its position in the list, so it could not be dragged between them.
- Width snaps to a whole number of 176px columns, because the panels are that
  fixed width and anything in between leaves a dead strip. Minimum one column,
  maximum half the desktop.
- Dragging shows a `UIDropPlaceholder` in the slot the window would land in. It
  is a real view in the layout, so the other windows shuffle around it exactly
  as they will on release, and the drop reads the placeholder's position rather
  than recomputing it, so preview and result cannot disagree.
- New windows open into whichever column is currently emptiest.
- Narrowing the sidebar moves windows out of columns that no longer exist into
  the last surviving one.

`GameDesktop.LayoutSubviews` calls `Sidebar.ResolveWidth` **before** laying
anything out, because the game view's size depends on the answer.
