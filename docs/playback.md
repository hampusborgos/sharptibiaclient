# Playback and seeking

The client plays `.tmv` recordings, and the transport under the game view can
scrub, pause, change speed and run the recording backwards.

None of that is something the file format helps with. A `.tmv` is a forward-only
append log read through a `GZipStream`: no index, no keyframes, only a delay
before each chunk. There is nothing to seek *to*.

What makes it work is a property of the original design. `CTC/Protocol/` and
`CTC/Client/` have no graphics references at all, and building client state from
packets is deterministic. So any point in the recording can be reached by
feeding packets through — which `ClientState.ForwardTo` already did, from the
start, on every launch.

Seeking is that, plus somewhere closer to start from.

## The pieces

```
TibiaMovieRecording   the whole .tmv parsed into memory: packets with absolute
                      timestamps, and a duration that is actually right
MoviePlayback         the clock. Position, Speed, Direction, Playing, and the
                      cursor into the recording
ClientState           owns both, plus the snapshots. ReplayTo is the single
                      packet delivery path; SeekTo drives it
ClientSnapshot        a deep copy of the viewport and the parser's session
CloneContext          the identity map that keeps the copy's aliases intact
```

Loading the whole file up front costs nothing worth measuring — the largest
recording here is 2.1MB uncompressed and 21891 packets — and it turns the
timeline into an array that can be binary searched.

## Snapshot density

Snapshots are not evenly spaced. Reversing needs one within about a second of
the playhead to be smooth; a point half an hour back only has to be reachable
at all. So the required spacing widens with distance from the playhead:

| distance behind the playhead | spacing |
|---|---|
| up to 5 seconds | 1 second |
| up to 30 seconds | 5 seconds |
| up to 30 minutes | 30 seconds |
| beyond | 15 minutes |

One rule drives both ends of it. `MaybeSnapshot` captures when the timeline has
gone longer than the interval without one, and `Thin` drops anything that has
become denser than its distance warrants. As the playhead moves, the region
behind it fills in and the region it left thins out. Reversing into a fresh
thirty second block pays for one replay of that block and gets the whole fine
ladder built on the way through; the fifteen or so steps after it replay about
a second of packets each.

The effect on a long recording is that memory stops tracking length. Xyvero is
two hours and twelve minutes and settles at around thirty snapshots.

## Backwards playback runs at 15Hz

Forward is cheap: deliver the packets the playhead has passed, never restore
anything. Backwards has to restore and replay on every step, so the steps are
rationed — state is applied fifteen times a second and the frames in between
redraw the last one.

Nothing is lost by that here. Creatures do slide between their tiles going
forward, but that runs on the playback clock, which does not advance while
reversing — and every reverse step is a seek, which drops the walks along with
the effects, so creatures stand on their tiles the whole way back. Between two
applications of state there is nothing left to move, and a reverse step at 120Hz
would produce the same picture as one at 15Hz, eight times over.

## What has to be copied, and what must not be

The client state is not a tree. The same `ClientCreature` is reached from the
tile it stands on, from `ClientViewport.Creatures`, and from the parser's own
`KnownCreatures` cache, and `Viewport.Player` is literally the same object as
`Creatures[PlayerID]`. Copy those independently and you get a state that looks
right and then drifts, because an update arriving through one route is not seen
through the others. So every copy goes through one `CloneContext`, and anything
reached twice comes out as one object.

`ItemType` is the opposite case: an immutable singleton owned by
`TibiaGameData` that reaches a live `Texture2D` through its `GameSprite`. It is
always shared, never copied — which also means a snapshot only belongs to the
`ClientState` that took it, since each one loads its own `TibiaGameData`.

**The parser has state too.** `TibiaGamePacketParserFactory` carries
`CurrentPosition`, `Player` and `KnownCreatures` between packets. Rewind the
viewport without them and the parser keeps walking on from wherever it had got
to, handing out creatures the restored viewport has never heard of. They travel
in the same snapshot, through the same `CloneContext`.

## Restoring without breaking the panels

A restore writes back into the live `ClientViewport` rather than replacing it.
Every sidebar panel holds that reference, and several hold references to things
inside it, so swapping the object out would leave them pointed at a state that
nothing updates any more.

The same reasoning goes one level further for the player. When the restored
player has the same ID as the live one — which is always, within a recording —
the fields are copied into the existing `ClientPlayer` and its existing
`ClientSkill` objects. `SkillPanel` caches one `ClientSkill` per row and reads
it every frame, so that is what keeps it correct with no resync at all.

What that buys, per panel:

| | |
|---|---|
| `SkillPanel` | nothing to do, its `ClientSkill` objects are the same ones |
| `InventoryPanel` | nothing to do, it re-reads `Viewport.Inventory` every frame |
| `VIPPanel` | needs `RaiseVIPStatusChanged(null)`; it only rebuilds from that event |
| `ContainerPanel` | needs a close/update/open diff, in that order |

The container order matters. `GameDesktop` builds a `ContainerPanel` in
response to `OpenContainer`, and that panel's constructor reads
`Viewport.Containers[ID]` immediately, so the dictionary has to be repopulated
before the opens go out — and the diff has to be taken against the keys that
were open *before* it was rewritten, or a container that never closed gets a
second panel subscribed to it.

## Effects are dropped, not seeked

`MagicEffect`, `DistanceEffect` and `AnimatedText` accumulate elapsed time from
wherever they started, so there is no meaningful way to move them to a new point
on the timeline. `GameCanvas` ignores the three protocol events that create them
while `ClientState.Seeking` is set — a seek rushes minutes of packets past in a
few milliseconds, and left alone it would pile up every spell cast in between
and play them all at once on arrival — and clears what is playing when the seek
lands.

They also run on the playback clock rather than the wall clock, so a spell at
eight times speed finishes eight times sooner and a paused client stops
animating.

## Verifying it

The failure this design invites is not a crash. It is a field somebody adds to
`ClientCreature` or `ClientViewport` later and forgets to copy: no compiler
error, no exception, just a state that quietly disagrees with itself after a
seek.

```bash
./CTC --verify-seek recordings/Xyvero.tmv
```

This runs headless — nothing under the UI layer touches a graphics device, since
textures are only built inside `GameRenderer.DrawImage` — and compares the state
reached by seeking against the state reached by playing straight through, over
a fingerprint of every tile, creature, item, skill, container and channel. It
also asserts the aliasing directly.

**Run it after touching anything in `CTC/Client/`.**
