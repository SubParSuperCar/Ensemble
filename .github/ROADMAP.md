# Ensemble Roadmap

A living checklist. Not exhaustive, not ordered by priority within a section, and subject to change. The design behind
spawned creations, and what comes next for them, lives in [**LIVE-CREATIONS.md**](./LIVE-CREATIONS.md).

- `[ ]` not started, `[x]` done, `(WIP)` in progress, `(deferred)` parked
- `(S)` about a session, `(M)` a few sessions, `(L)` a week or more

---

## Tooling

The tool suite, in rough intended order of implementation.

- [x] DestructTool (Delete) - remove instances from the local plot
    - [x] (S) SolidHighlight, a flat-color variant of AxialHighlight, red for destruct
    - [x] (S) Raycast collision mask - the tools' rays hit World and Static Instances only, never characters
    - [x] (S) "Clear All" toolbar button - visible only with DestructTool on, owner, not spawned; disabled at 0
      instances; double-click to confirm; disables DestructTool after use
    - [x] (M) Multi-selection - `Shift`-click to toggle, click a selected instance to delete them all, bulk
      `RemoveInstancesAction` (up to 256, token cost scales with count)
- ConstructTool (Place) - place the selected asset (WIP)
    - [x] (M) Raycast placement, face/grid snapping, rotation, ghost preview
    - [x] (M) OBB SAT-based intersection resolution - previews push out of overlaps by the minimum translation, snap
      along the surface with edges aligned to the targeted instance, and never clip
    - [x] (S) Shared placement rules (`PlotPlacement`) - bounds, overlap, and quotas, also validated server-side
    - [x] (M) Asset Selector UI - categorized tree of placeable assets, feeds the tool
    - [ ] (S) Fade the placement preview in and out as it appears and disappears
- PropertyTool (Edit) - view/modify asset properties not prefixed with an underscore
    - [x] (M) Selection and merged properties - one value where the selected instances agree, a neutral one (marked
      mixed) where they differ, and separate entries for same-named properties of different types
    - [x] (M) Property Editor UI - a filterable, alphabetical list with an editor per type, mixed markers, and copy /
      paste buttons, open while the tool is enabled
- [ ] PaintTool (Paint) - drives the `_colorHex` / `_materialId` asset attributes, which `Paintable` assets follow
  (materials come from the `MaterialBank`: a look plus density, friction, and bounce)
- [ ] TransformTool (Move) - move a whole creation or a selection; possible copy/paste
- [x] (M) Shared ToolCommon - TriggerAction, RayLength, collision masks, etc.
- [x] (M) MultiSelector - shared multi-selection state across tools, Baja Builders click semantics
- [x] (M) Marquee Selector - Ctrl+drag to box-select, Shift to toggle-select, Esc to cancel, Baja Builders parity
- [x] (S) Plot outlines - the local plot's boundary rises from its grid in the active tool's color (white while hovered
  in the Plot Selector), fading in and out as it rises and sinks
- [ ] (S) Fade the delete tool's selection highlight boxes in and out, as the plot outlines do
- [x] (S) Scripted building - Lua `add_inst`, `rm_insts`, `set_inst_prop`, and `dmp_insts` work on the local plot
  through the same actions (and validation) as the tools, so they also work in multiplayer
- [x] (S) Console saves - `save_creation` / `load_creation` (through AddInstancesAction batches, paced under the rate
  limit) / `loc_load_creation` / `dmp_saves`, in `user://creations/`; the Saves UI below will share the same path
- [ ] (M) Build share codes - `copy_build()` / `paste_build(code)` turn a plot into a short compressed text code to
  paste in chat (reuses the save pipeline)
- [x] (M) Undo / redo - a local history the tools push to, not part of the actions (Lua and large scripted edits stay
  out of it): each entry pairs a redo with an undo thunk that submits the counter-action (e.g., re-adding removed
  instances), so validation still applies; `Ctrl+Z` / `Ctrl+X` (beside the clipboard keys, not `Ctrl+Y`), cleared
  when the local plot changes
- [x] (M) Clipboard - `Ctrl+C` / `Ctrl+V` copy the selection's shared properties and paste them onto others, next to
  undo / redo on the same hand
- [ ] (M) Build replays - record a plot's add / remove / property actions and replay them as a time-lapse

## Multiplayer / replication

SessionManager (`Sessions/`) has the infrastructure (pluggable transports, a versioned auth handshake, server-assigned
player IDs, self-registering RPC actions and late-join snapshots, kicks, graceful shutdown notices). Actions and
snapshots live in `Replication/`, and actions are submitted with `new SetPlotAction(id).Submit()`.

- [x] (M) Main-menu session sub-menu - Single-Player / Multi-Player, Host / Join, address, port, password, remembered
  display name
- [x] (M) Late-join state sync - PlotsSnapshot (occupants, owner, spawn state, instances + properties), requested by the
  client once its world exists; actions are withheld from a peer until it is synced
- [x] (M) Add / Remove / ClearInstancesAction - ConstructTool, DestructTool, and "Clear All" go through actions
- [x] (S) Instance references - actions verify an instance's ID against its asset and transform, since freed IDs are
  reused
- [x] (S) SetPropertiesAction - type- and key-checked property edits, up to 256 instances in one request
- [x] (S) AddInstancesAction - up to 256 instances with their properties in one request, placed in order and
  skipped where they don't fit, ready for build share codes and loading saves
- [x] (M) SessionManager v2 - `Peer` objects (address, ping), `Config` / server info, `PeerRegistered(Peer)` signals
- (WIP) (S) Peer UI - ping column and host kick / IP ban context menu in the player list done (reason box TODO);
  server info panel next
- [x] (S) DTLS - ENet traffic is encrypted with the host's self-signed certificate (created once, kept in `user://`);
  clients don't verify it yet, so it stops eavesdropping and tampering, not impersonation
- [ ] (S) Host certificate pinning - clients remember a host's certificate on first join and warn if it changes
- [x] (S) Headless dedicated server - `--headless -- --port=N --password=X --max-clients=N --upnp` (or `--join=HOST:P`)
- [x] (S) UPnP port forwarding - opt-out host checkbox (remembered), leased + renewed off the main thread, join code
  shown under the player list
- (WIP) (M) Character replication - position / yaw at 20 Hz, server-relayed, buffered and interpolated 100 ms behind
  on the sender's timeline, like creations (no animation state yet)
- [x] (S) Character reset - hold H for 1 s, or `tp_char()` / `tp_char(x, y, z)` / `tp_char(player)` in Lua
- [x] (L) Instance replication - placements, deletions, and property edits sync as validated actions, with the host
  as the authority and late joiners restored from snapshots
- (WIP) (M) Plot ownership / edit permissions over the wire - SetPlotAction + occupant/owner checks done
- [x] (S) Text chat - Text Chat widget (opens in multiplayer, `/` to type, Roblox-style nametag colors, join / leave
  notices, unread count in the drawer), chat action with the host's profanity filter (masks with `#`, or `<Redacted>` if
  nothing else remains), late-join history snapshot, optional per-session file logs, and Lua functions
- [x] (S) Profanity filter - host-chosen (remembered), masks chat profanity and replaces inappropriate display names
- [ ] (M) Chat bubbles over players
- [ ] (M) Plot join consent - joining a plot that has an owner asks them "Let <name> join your plot?" (Accept / Deny);
  no answer within 10 s denies it, and the host joins any plot without asking. A host-validated request / answer pair
  (one pending request per player, dropped when either leaves or the plot's owner changes), plus the prompt's layout
- [ ] (M) Plot Manager (replaces the Plot Selector) - join requests through the consent prompt above, and kicking
  occupants (owner or host; `KickOccupantAction` and `kick_occupant` done)

## Live physics

Spawned plots become live creations: welded assemblies simulated by one peer and streamed to everyone else.
[**LIVE-CREATIONS.md**](./LIVE-CREATIONS.md) explains how it works and holds the design for what comes next.

- [x] (M) Prerequisites - physics layers, a game-wide `Epsilon` (1/2048 m), multi-collider assets, the abut test, and
  weld groups (0.52.0)
- [x] (L) Single-player spawning - assemblies, members, mass, and a kill height (0.52.0); spawning moves the local
  character out of the creation, and characters press their weight on what they stand on
- [x] (L) Multiplayer with the host simulating - bit-packed state, proxies, epochs, keepalives, and late joins (0.53.0)
- [x] (L) Owner authority - plot owners simulate, with host-decided hand-offs, overrides, timeouts, relay limits, and
  proxies replayed 100 ms behind (0.54.0)
- [x] (S) Poor-connection simulator - `set_net_sim(latency_ms, jitter_ms, loss)` delays and drops incoming live state
- [x] (M) Creation arbitration - creations with different simulators that come close (the host's sphere tests at 1.5
  times their bounds) share one simulator, their largest's, until they've been apart for 2 s (0.55.0)
- [x] (M) Asset pieces and authored joints - `AssetPiece` children move on their own, linked by Godot joints authored
  in the asset's scene (the first: Suspension, with its coil)
- [x] (S) Terrain collision everywhere - Terrain3D builds its whole collision up front, so bodies far from the local
  character (another peer's creation, or a dedicated host's) stay on the ground
- (WIP) (L) Live asset logic - `AssetComponent` nodes under an asset's `AssetHandle` (unique behaviors may subclass
  `AssetHandle`) initialize once their whole creation exists; `Paintable` and the `MaterialBank` are done. Next: a
  batched simulation tick for many scripted blocks, then seats, input, and driven joints (motors, servos, pistons)
- [ ] (L) Input management - player-bound inputs driving assets (e.g., motors)
- [ ] (M) Bandwidth - a per-client budget that favors near and fast bodies over far ones
- [ ] (M) Characters riding moving creations (platform velocity)
- [ ] (M) Anchors - an asset that freezes its assembly while it holds it

Known hard parts:

- Collisions between creations with different simulators: arbitration hands close ones to one simulator, but fast ones
  may meet before the hand-off arrives (one-sided pushes until then)
- Joint stability across large mass ratios
- Bandwidth spikes when many loose bodies move at once
- Characters standing on someone else's moving creation

## Gameplay / content

- (WIP) (M) Plot spawn / despawn - `SetPlotSpawnedAction`, the floating Spawn / Despawn button (owner only), and
  `set_plot_spawned` done; an emptied plot despawns. Next: live asset logic (see [**Live physics**](#live-physics))
- [ ] (M) Player models - replace the placeholder crystal, with animation state to replicate
- [ ] (L) A real map, built with Unibuilder
- [ ] (L) Speaker asset (and/or a music player widget) - plays audio to nearby players. Open problems: moderating
  inappropriate audio, replicating playback, and storing / fetching the audio in the first place

## UI / UX framework

Avalonia + Estragonia. MVVM, NavigatorService, ViewLocatorService in place.

- [x] (S) Split DocFileView / WebBrowserView - base views/VMs moved to Views/Common + ViewModels/Common, no
  NavigatorService dependency; MenuDocFileView / MenuWebBrowserView wrappers add the Back button for menu use
- (WIP) (L) Widget system - scoped `WidgetManagerService`, one widget per view model, draggable / resizable /
  maximizable frames with fades, a collapsible widget drawer with badges; Plot Selector, Asset Selector, Property
  Editor, Web Browser, Lua Editor, Log Output, and Text Chat are widgets. Next: settings, docs, and persisted layouts
- [ ] (M) Settings menu (hosted in a window)
- [ ] (M) In-game HUD pass
- [ ] (S) Main menu personality - community links (e.g., Discord) and randomized flavor text
- [ ] (S) Web browser on WPE WebKit 2.54+ - blocked on Avalonia's WPE adapter moving to the WPEPlatform API; it only
  renders on Linux with WPE WebKit 2.52 or older (other engines need native windows Estragonia doesn't have)

## Housekeeping / infra

- [x] (S) Saving/ - binary + JSON serializers, Zstd/Brotli compression, AES-256-GCM encryption, SHA-256 integrity
- [x] (S) Tests/ project (xUnit v3, MTP) - Occupants ownership, Instances IDs (HoleyArray), CoreVariant, save-pipeline
  roundtrips, HostEndPoint parsing, OBB/grid math, welds, placement, chat text, the profanity filter, NavigatorService
- [x] (S) Sessions/Impl gdignored; Sentinels dependency removed
- [x] (S) NsDepCop - namespace dependency rules between subsystems, enforced as build warnings
- [x] (S) Dedicated 3D physics layers - World, Character, Static Instances, and Dynamic Instances
- [ ] (S) Scripts/ directory reorg (consolidate single-file folders)
- [ ] (S) I/O resilience audit - corrupt or failing files and network I/O never take the game down
- [ ] (S) Audit global using static (Globals / Constants / GContext / Sentinels) - keep only what must be global

## Later / big

- [ ] (M) Save envelope - optional plaintext metadata block (name, thumbnail, timestamps, instance count) so a browser
  can list encrypted saves (deferred)
- [ ] (L) Comprehensive Saves UI - browser, thumbnails, autosave ring buffer, per-save password prompt, soft delete
  (deferred)
- [ ] (L) In-world screens (low priority) - Avalonia UI rendered onto 3D meshes, e.g., a "monitor" asset showing the
  console, Lua editor, chat, or system terminal, for whole in-game workstations (with speakers). Estragonia is brittle
  about when Avalonia controls get instantiated, so the timing needs care first
