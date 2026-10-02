# Ensemble Roadmap

A living checklist. Not exhaustive, not ordered by priority within a section, and subject to change.

- `[ ]` not started, `[x]` done, `(WIP)` in progress, `(deferred)` parked
- `(S)` ~a session, `(M)` a few sessions, `(L)` a week or more

---

## Tooling

The tool suite, in rough intended order of implementation.

- DestructTool (Delete) - remove instances from the local plot (WIP)
    - [x] (S) SolidHighlight, a flat-color variant of AxialHighlight, red for destruct
    - [x] (S) raycast collision mask - hit World only, ignore characters
    - [x] (S) "Clear All" toolbar button - visible only with DestructTool on, owner, not spawned; disabled at 0
      instances; double-click to confirm; disables DestructTool after use
    - [ ] (S) finer raycast filtering once dedicated physics layers exist (plot base vs. instances)
- ConstructTool / CtorTool (Place) - place the selected asset (WIP)
    - [x] (M) raycast placement, face/grid snapping, rotation, ghost preview
    - [ ] (M) SAT-based intersection resolution (nudge placement out of overlaps)
    - [x] (M) Asset Selector UI - categorized tree of placeable assets, feeds the tool
- [ ] AttrTool (Edit) - view/modify asset properties not prefixed with an underscore
- [ ] TextureTool (Paint) - drives the _colorHex / _materialId asset attributes
- [ ] TransformTool (Move) - move a whole creation or a selection; possible copy/paste
- [x] (M) shared ToolCommon - TriggerAction, RayLength, collision masks, etc.
- [ ] (M) MultiSelector - shared multi-selection state across tools (deferred)
- [ ] (M) Marquee Selector UI - shift+drag to box-select, ctrl to toggle-select, Baja Builders parity (deferred)

## Multiplayer / replication

SessionManager has the infra (sessions, versioned auth handshake, server-assigned player IDs, self-registering RPC
actions and late-join snapshots, kicks, graceful shutdown notices). Actions and snapshots live in Networking/, and
actions are submitted with `new SetPlotAction(id).Submit()`.

- [x] (M) Main-menu session sub-menu - Single-Player / Multi-Player, Host / Join, address, port, password, remembered
  display name
- [x] (M) Late-join state sync - PlotsSnapshot (occupants, owner, spawn state, instances + properties), requested by
  the client once its world exists; actions are withheld from a peer until it is synced
- [x] (M) Add / Remove / ClearInstancesAction - ConstructTool, DestructTool, and "Clear All" go through actions
- [x] (M) SessionManager v2 - `Peer` objects (address, ping), `Config` / server info, `PeerRegistered(Peer)` signals
- (WIP) (S) Peer UI - ping column in the player list done; server info panel and kick button next
- [x] (S) Headless dedicated server - `--headless -- --port=N --password=X --max-clients=N` (or `--join=HOST:P`)
- (WIP) (M) Character replication - position / yaw at 20 Hz, server-relayed, interpolated (no animation state yet)
- [x] (S) Character reset - hold H for 1 s, or `tp_char()` / `tp_char(x, y, z)` / `tp_char("name or id")` in Lua
- [ ] (L) Dynamic instance replication - placed blocks sync + authority model
- (WIP) (M) Plot ownership / edit permissions over the wire - SetPlotAction + occupant/owner checks done
- [ ] (S) Text chat (multiplayer only) - chat RPC action + chat box UI

## UI / UX framework

Avalonia + Estragonia. MVVM, NavigatorService, ViewLocatorService in place.

- [x] (S) Split DocFileView / WebBrowserView - base views/VMs moved to Views/Common + ViewModels/Common, no
  NavigatorService dependency; MenuDocFileView / MenuWebBrowserView wrappers add the Back button for menu use
- [ ] (L) Window-widget system - draggable / resizable / minimizable panels (the "utensil drawer") to host browsers,
  settings, chat, docs
- [ ] (M) Settings menu (hosted in a window)
- [ ] (M) In-game HUD pass

## Housekeeping / infra

- [x] (S) Saving/ - binary + JSON serializers, Zstd/Brotli compression, AES-256-GCM encryption, SHA-256 integrity
- [x] (S) Tests/ project scaffold (xUnit v3, MTP)
- [x] (S) SessionManager fully gdignored; Sentinels dependency removed
- [ ] (S) Scripts/ directory reorg (consolidate single-file folders)
- [ ] (S) dedicated 3D physics layers - Plot Base, Instances (only World + Character exist today)
- [x] (S) populate Tests/ - Occupants ownership, Instances IDs (HoleyArray), CoreVariant, save-pipeline roundtrips,
  HostEndPoint parsing
- [ ] (S) audit global using static (Globals / Constants / GContext / Sentinels) - keep only what must be global

## Later / big

- [ ] (M) Save envelope: optional plaintext metadata block (name, thumbnail, timestamps, instance count) so a browser
  can list encrypted saves (deferred)
- [ ] (L) Comprehensive Saves UI - browser, thumbnails, autosave ring buffer, per-save password prompt, soft delete
  (deferred)
