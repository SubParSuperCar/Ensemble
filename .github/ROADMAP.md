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
    - [ ] (M) raycast placement, face/grid snapping, rotation, ghost preview
    - [ ] (M) SAT-based intersection resolution (nudge placement out of overlaps)
    - [ ] (M) Asset Selector UI - grid of placeable assets, feeds the tool
- [ ] AttrTool (Edit) - view/modify asset properties not prefixed with an underscore
- [ ] TextureTool (Paint) - drives the _colorHex / _materialId asset attributes
- [ ] TransformTool (Move) - move a whole creation or a selection; possible copy/paste
- [ ] (M) shared ToolConstants (or Tools/Utils.cs) - TriggerAction, RayLength, collision masks, etc.
- [ ] (M) MultiSelector - shared multi-selection state across tools (deferred)
- [ ] (M) Marquee Selector UI - shift+drag to box-select, ctrl to toggle-select, Baja Builders parity (deferred)

## Multiplayer / replication

SessionManager has the infra (sessions, auth, RPC action registry); nothing uses it yet.

- [ ] (M) Main-menu session sub-menu - Singleplayer / Multiplayer, Host / Join, address, port, password (stretches
  NavigatorService)
- [ ] (M) Character replication - position / rotation / state
- [ ] (L) Dynamic instance replication - placed blocks sync + authority model
- [ ] (M) Plot ownership / edit permissions over the wire
- [ ] (S) Text chat (multiplayer only) - chat RPC action + chat box UI

## UI / UX framework

Avalonia + Estragonia. MVVM, NavigatorService, ViewLocatorService in place.

- [x] (S) Split DocFileView / WebBrowserView - base views/VMs moved to Views/Common + ViewModels/Common, no
  NavigatorService dependency; MenuDocFileView / MenuWebBrowser wrappers add the Back button for menu use
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
- [ ] (S) populate Tests/ - HoleyArray, OccupantRegistry, CoreVariant, save-pipeline roundtrip
- [ ] (S) audit global using static (Globals / Constants / GContext / Sentinels) - keep only what must be global

## Later / big

- [ ] (M) Save envelope: optional plaintext metadata block (name, thumbnail, timestamps, instance count) so a browser
  can list encrypted saves (deferred)
- [ ] (L) Comprehensive Saves UI - browser, thumbnails, autosave ring buffer, per-save password prompt, soft delete
  (deferred)
