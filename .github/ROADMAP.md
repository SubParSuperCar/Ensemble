# Ensemble Roadmap

A living checklist. Not exhaustive, not ordered by priority within a section, and subject to change.

- `[ ]` not started, `[x]` done, `(WIP)` in progress, `(deferred)` parked
- `(S)` about a session, `(M)` a few sessions, `(L)` a week or more

---

## Tooling

The tool suite, in rough intended order of implementation.

- DestructTool (Delete) - remove instances from the local plot (WIP)
    - [x] (S) SolidHighlight, a flat-color variant of AxialHighlight, red for destruct
    - [x] (S) Raycast collision mask - hit World only, ignore characters
    - [x] (S) "Clear All" toolbar button - visible only with DestructTool on, owner, not spawned; disabled at 0
      instances; double-click to confirm; disables DestructTool after use
    - [x] (M) Multi-selection - `Shift`-click to toggle, click a selected instance to delete them all, bulk
      `RemoveInstancesAction` (up to 256, token cost scales with count)
    - [ ] (S) Finer raycast filtering once dedicated physics layers exist (plot base vs. instances)
- ConstructTool / CtorTool (Place) - place the selected asset (WIP)
    - [x] (M) Raycast placement, face/grid snapping, rotation, ghost preview
    - [x] (M) OBB SAT-based intersection resolution - previews push out of overlaps by the minimum translation, snap
      along the surface with edges aligned to the targeted instance, and never clip
    - [x] (S) Shared placement rules (`PlotPlacement`) - bounds, overlap, and quotas, also validated server-side
    - [x] (M) Asset Selector UI - categorized tree of placeable assets, feeds the tool
- [ ] AttrTool (Edit) - view/modify asset properties not prefixed with an underscore
- [ ] TextureTool (Paint) - drives the `_colorHex` / `_materialId` asset attributes
- [ ] TransformTool (Move) - move a whole creation or a selection; possible copy/paste
- [x] (M) Shared ToolCommon - TriggerAction, RayLength, collision masks, etc.
- [x] (M) MultiSelector - shared multi-selection state across tools, Baja Builders click semantics
- [x] (M) Marquee Selector - Alt+drag to box-select, Shift to toggle-select, Esc to cancel, Baja Builders parity
- [x] (S) Scripted building - Lua `add_inst`, `rm_insts`, `set_inst_prop`, and `dmp_insts` work on the local plot
  through the same actions (and validation) as the tools, so they also work in multiplayer
- [ ] (M) Build share codes - `copy_build()` / `paste_build(code)` turn a plot into a short compressed text code to
  paste in chat (reuses the save pipeline)
- [ ] (M) Build replays - record a plot's add / remove / property actions and replay them as a time-lapse; groundwork
  for undo / redo

## Multiplayer / replication

SessionManager (Sessions/) has the infra (pluggable transports, versioned auth handshake, server-assigned player IDs,
self-registering RPC actions and late-join snapshots, kicks, graceful shutdown notices). Actions and snapshots live in
Replication/, and actions are submitted with `new SetPlotAction(id).Submit()`.

- [x] (M) Main-menu session sub-menu - Single-Player / Multi-Player, Host / Join, address, port, password, remembered
  display name
- [x] (M) Late-join state sync - PlotsSnapshot (occupants, owner, spawn state, instances + properties), requested by the
  client once its world exists; actions are withheld from a peer until it is synced
- [x] (M) Add / Remove / ClearInstancesAction - ConstructTool, DestructTool, and "Clear All" go through actions
- [x] (S) Instance references - actions verify an instance's ID against its asset and transform, since freed IDs are
  reused
- [x] (S) SetPropertiesAction - type- and key-checked property edits, ready for AttrTool
- [x] (M) SessionManager v2 - `Peer` objects (address, ping), `Config` / server info, `PeerRegistered(Peer)` signals
- (WIP) (S) Peer UI - ping column in the player list done; server info panel and kick button next
- [x] (S) Headless dedicated server - `--headless -- --port=N --password=X --max-clients=N --upnp` (or `--join=HOST:P`)
- [x] (S) UPnP port forwarding - opt-out host checkbox (remembered), leased + renewed off the main thread, join code
  shown under the player list
- (WIP) (M) Character replication - position / yaw at 20 Hz, server-relayed, interpolated (no animation state yet)
- [x] (S) Character reset - hold H for 1 s, or `tp_char()` / `tp_char(x, y, z)` / `tp_char("name or id")` in Lua
- [ ] (L) Dynamic instance replication - placed blocks sync + authority model
- (WIP) (M) Plot ownership / edit permissions over the wire - SetPlotAction + occupant/owner checks done
- [x] (S) Text chat - Text Chat widget (opens in multiplayer, `/` to type, Roblox-style nametag colors, join / leave
  notices, unread count in the drawer), chat action with the host's profanity filter (masks with `#`, or `<Redacted>` if
  nothing else remains), late-join history snapshot, optional per-session file logs, and Lua functions
- [x] (S) Profanity filter - host-chosen (remembered), masks chat profanity and replaces inappropriate display names
- [ ] (M) Chat bubbles over players
- [ ] (M) Plot Manager (replaces the Plot Selector) - join requests for owners (the host can override), kicking
  occupants (owner or host; `KickOccupantAction` and `kick_occupant` done), and Player List context menus for kicking
  peers
- [ ] (L) Physics replication - plot-owner authority, possibly over custom RPCs

## Gameplay / content

- [ ] (M) Plot spawn / despawn - `PlotHandle.Spawning` stubs are in place
- [ ] (L) Live asset logic - infrastructure for assets with behavior
- [ ] (L) Input management - player-bound inputs driving assets (e.g., motors)
- [ ] (M) Player models - replace the placeholder crystal, with animation state to replicate
- [ ] (L) A real map, built with Unibuilder
- [ ] (L) Speaker asset (and/or a music player widget) - plays audio to nearby players. Open problems: moderating
  inappropriate audio, replicating playback, and storing / fetching the audio in the first place

## UI / UX framework

Avalonia + Estragonia. MVVM, NavigatorService, ViewLocatorService in place.

- [x] (S) Split DocFileView / WebBrowserView - base views/VMs moved to Views/Common + ViewModels/Common, no
  NavigatorService dependency; MenuDocFileView / MenuWebBrowserView wrappers add the Back button for menu use
- (WIP) (L) Widget system - scoped `WidgetManagerService`, one widget per view model, draggable / resizable /
  maximizable frames with fades, a collapsible widget drawer with badges; Plot Selector, Asset Selector, Web Browser,
  Lua Editor, Log Output, and Text Chat are widgets. Next: settings, docs, and persisted layouts
- [ ] (M) Settings menu (hosted in a window)
- [ ] (M) In-game HUD pass
- [ ] (S) Main menu personality - community links (e.g., Discord) and randomized flavor text
- [ ] (S) Web browser on WPE WebKit 2.54+ - blocked on Avalonia's WPE adapter moving to the WPEPlatform API; it only
  renders on Linux with WPE WebKit 2.52 or older (other engines need native windows Estragonia doesn't have)

## Housekeeping / infra

- [x] (S) Saving/ - binary + JSON serializers, Zstd/Brotli compression, AES-256-GCM encryption, SHA-256 integrity
- [x] (S) Tests/ project scaffold (xUnit v3, MTP)
- [x] (S) Sessions/Impl gdignored; Sentinels dependency removed
- [x] (S) NsDepCop - namespace dependency rules between subsystems, enforced as build warnings
- [ ] (S) Scripts/ directory reorg (consolidate single-file folders)
- [ ] (S) Dedicated 3D physics layers - Plot Base, Instances (only World + Character exist today)
- [x] (S) Populate Tests/ - Occupants ownership, Instances IDs (HoleyArray), CoreVariant, save-pipeline roundtrips,
  HostEndPoint parsing, OBB/grid math, NavigatorService
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
