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
- [ ] TextureTool (Paint) - drives the `_colorHex` / `_materialId` asset attributes, which `Paintable` assets follow
  (materials come from the `MaterialBank`: a look plus density, friction, and bounce)
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
- (WIP) (S) Peer UI - ping column and host kick context menu in the player list done (kick reason box TODO); server
  info panel next
- [x] (S) DTLS - ENet traffic is encrypted with the host's self-signed certificate (created once, kept in `user://`);
  clients don't verify it yet, so it stops eavesdropping and tampering, not impersonation
- [ ] (S) Host certificate pinning - clients remember a host's certificate on first join and warn if it changes
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
- [ ] (M) Plot Manager (replaces the Plot Selector) - join requests for owners (the host can override) and kicking
  occupants (owner or host; `KickOccupantAction` and `kick_occupant` done)
- [ ] (L) Physics replication - plot-owner authority over custom RPCs; see
  [**Spawning and live physics**](#spawning-and-live-physics-plan)

## Gameplay / content

- (WIP) (M) Plot spawn / despawn - `SetPlotSpawnedAction` and `set_plot_spawned` done; spawned plots become live,
  welded creations in single-player, and an emptied plot despawns. Next: multiplayer; see
  [**Spawning and live physics**](#spawning-and-live-physics-plan)
- (WIP) (L) Live asset logic - infrastructure for assets with behavior. `AssetComponent`s are nodes dropped under an
  asset's `AssetHandle` in its scene (unique behaviors may subclass `AssetHandle`); handles bind to their plot and
  instance, then initialize their components once their whole creation exists, and live in one mode (build or live)
  for life. `Paintable` (material overrides from `_colorHex` / `_materialId`, opt-out by node) is the first. Next: a
  material registry, and a batched tick for many scripted blocks
- [ ] (L) Input management - player-bound inputs driving assets (e.g., motors)
- [ ] (M) Player models - replace the placeholder crystal, with animation state to replicate
- [ ] (L) A real map, built with Unibuilder
- [ ] (L) Speaker asset (and/or a music player widget) - plays audio to nearby players. Open problems: moderating
  inappropriate audio, replicating playback, and storing / fetching the audio in the first place

## Spawning and live physics (plan)

The agreed design for spawned plots, from 2026-10-15; single-player live creations shipped in v0.52.0. Everything
after them (asset logic, mechanisms, seats, input, physics authority, replication) is designed in
[**LIVE-CREATIONS.md**](./LIVE-CREATIONS.md).

### Terms

| Term             | Meaning                                                                                    |
|------------------|--------------------------------------------------------------------------------------------|
| Creation         | A plot's build as a whole, static or live (as in saves)                                    |
| Instance         | One placed asset in Core: ID, asset, grid position, rotation, properties                   |
| Static instances | Build mode: frozen AssetHandles under `Instances/Static`, editable with the tools          |
| Spawned / live   | A plot whose creation is simulated, under `Instances/Dynamic`; editing is locked           |
| Abut             | Two collider boxes share part of a face within `Epsilon`; edges and corners don't count    |
| Weld             | An automatic, unbreakable rigid link between two instances whose collider boxes abut       |
| Group            | The instances connected by welds (one union-find component), even a single instance        |
| Assembly         | The `RigidBody3D` built for one group; its ID is the group's lowest instance ID            |
| Member           | An AssetHandle riding its assembly, with its own body out of the physics space             |
| Mechanism        | (Later) Assemblies linked by joints; the unit of ownership                                 |
| Owner            | The peer that simulates a mechanism; the plot's owner by default (network ownership)       |
| Proxy            | An assembly on a peer that doesn't simulate it: frozen, kinematic, moved by received state |
| Hand-off         | Ownership moving to another peer, which resumes simulating from the last received state    |
| Anchor           | (Later) An asset that freezes its assembly while any anchor holds it                       |
| Piece            | (Later) One moving part of an asset (a motor's head); never welds to its siblings          |
| Passenger        | (Later, name TBD) A character seated in a creation                                         |

### Done (plumbing)

- `SetPlotSpawnedAction(bool)` (the owner, or the host via Lua), the floating Spawn / Despawn button (owner only), and
  `set_plot_spawned(is_spawned)`
- The static instances always mirror Core; spawning only hides them and takes them out of the physics space (one
  toggle on `Instances/Static`), so despawning is instant
- The edit lock lives in the actions (`IsSpawned: false`); Core still accepts changes while spawned, since it's only
  state
- An emptied plot despawns (leaving clears its instances first; a disconnect keeps them)
- Spawning moves the local character out of the creation (placement ignores characters), and characters press their
  weight (70 kg) on the simulated body they stand on. Teleports briefly leave the physics space, since Jolt would
  otherwise move a kinematic body through its surroundings and shove what it touched

### Principles

- Core stays pure data, and the static plot is the only persistent and replicated build state. Live state is
  ephemeral: despawning discards it, and saves never see it
- Every peer already has every plot's instances, so spawning sends no geometry; each peer builds the same assemblies
  from Core, in instance ID order
- No cross-machine physics determinism is assumed; live state is streamed, not lockstepped
- No breakable welds, and no dynamic body cap (instance quotas are the limit; the host kicks lag bombers)
- Collisions between creations with different owners may be imperfect (one-sided pushes); no custom physics

### Spawn pipeline (per plot, on every peer)

1. Welds - an edge for each instance pair with abutting collider boxes (`Obb.Abuts`, Baja Builders parity: either box,
   grown by `Epsilon` along one of its axes and shrunk by it along the others, overlaps the other), one box per
   collider, so assets may have several. Neighbors come from an ObbGrid (done: `Welds.Group`)
2. Groups - union-find over the weld edges; each connected component is one rigid group, even a group of one (done)
3. Assembly bodies - a new `RigidBody3D` per group under `Instances/Dynamic`, with each asset's collision shapes
   registered on it through the shape-owner API (`CreateShapeOwner(asset)`, `ShapeOwnerAddShape`,
   `ShapeOwnerSetTransform`) at the asset's offset, reusing the `Shape3D` resources. Godot only counts shapes that are
   direct children of a body, so nesting bodies (or wrapping `Node3D` assets) wouldn't work (done: `Assembly`)
4. Members - each asset is instantiated and parented under its assembly at its offset, with its own body out of the
   physics space; meshes and scripts ride along. Assets stay `RigidBody3D` scenes (done)
5. Mass - each asset's density times its volume by default (overridable later), summed, with the center of mass at the
   mass-weighted average position; Jolt derives the inertia from the shapes. Friction comes from the material later
   (done: `AssetHandle.Density`, water's by default)
6. IDs - each assembly takes its group's lowest instance ID, so peers agree on IDs without sending them (done)
7. Raycasts - a hit's shape index leads back to the exact asset (`ShapeFindOwner`, then `ShapeOwnerGetOwner`)
8. Joints (later) - hinge, piston, and suspension assets link two groups with Jolt joints; assemblies linked by joints
   form a mechanism, the unit of ownership

Despawning frees the assemblies and re-enables the static instances (done). Assemblies that fall below -1024 m are
switched off, not freed.

### Ownership and authority

- The plot owner simulates its assemblies (like Roblox's network ownership); everyone else shows kinematic proxies
  moved by the received state, so local characters can still stand on and collide with them. Each body is simulated
  exactly once
- When the owner leaves, the new owner takes over, with the host as the fallback; an empty plot despawns, so a
  creation never outlives its occupants
- API: host-authoritative, one call to set who simulates what, the host always the fallback
- The host validates incoming state like character updates: only a plot's owner may send its bodies, values must be
  finite and within the world, speeds are capped, and updates are rate limited

### Replication

- One unreliable, sequenced, batched message per plot per tick (a custom RPC with a packed byte payload), not one RPC
  per body; `MultiplayerSynchronizer` doesn't suit thousands of changing bodies
- About 24 bytes per body: ID, position, quantized rotation (smallest three), and quantized linear and angular
  velocity. Sleeping bodies send nothing, and idle keepalives recover from losses
- About 20 Hz, like characters, within a per-client bandwidth budget that favors near and fast bodies and slows far
  ones. 50 moving bodies cost about 24 KB/s per receiver; 1000 loose ones about 480 KB/s, hence welding and the budget
- Owner -> host -> synced peers, as with characters. Proxies interpolate about 100 ms behind, extrapolate briefly with
  velocity on loss, and snap when far off
- Late joins: PlotsSnapshot already carries the spawn state; the latest body states join it, so proxies start in place

### Phases (each shippable)

0. Prerequisites - physics layers (World, Character, Static Instances, Dynamic Instances), a game-wide `Epsilon`
   (1/512 m) for placement and welding, multi-collider assets, the abut test and weld groups (with unit tests), and
   spawning blocked in multiplayer until Phase 2
1. Single-Player - assemblies, members, and mass; a kill height for assemblies; stress-test Jolt with about 5000 blocks
2. Multiplayer with the host as the only simulator - batched state, proxies, interpolation, and late joins; proves the
   wire format
3. Owner authority - the same format, sent by the owner, relayed and validated by the host, with hand-off when the
   owner changes
4. Joints and motors - needs Live asset logic and Input management
5. Polish - bandwidth tuning, characters riding moving creations (platform velocity), and optional ownership hand-off
   on contact between creations

### Known hard parts

- Collisions between creations with different owners (one-sided pushes); acceptable for now
- Joint stability across large mass ratios
- Bandwidth spikes when many loose bodies move at once
- Characters standing on someone else's moving creation

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
