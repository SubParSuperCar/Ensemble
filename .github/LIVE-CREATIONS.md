# Live Creations (design)

The design for everything after single-player spawning (v0.52.0): asset logic, mechanisms, seats, input, physics
authority, replication, aerodynamics, and performance. It extends
[**Spawning and live physics**](./ROADMAP.md#spawning-and-live-physics-plan) in the roadmap, whose terms table this
uses. Decisions are numbered **(D#)**; the table at the end lists them, resolved or open. The phases are a guide, not a
contract: they bend when something unforeseen comes up.

---

## 1. Principles

- **Core stays pure.** Live state (bodies, ownership, inputs, seats) is ephemeral: it lives in Scripts and Replication,
  and despawning discards it. Only build state (instances and their properties) is persistent and replicated by
  actions.
- **The engine is generic; assets are specific.** Scripts knows `AssetHandle`, `AssetComponent`, `Assembly`, and a few
  interfaces, never a concrete asset. Concrete assets depend on the engine, never the reverse, and NsDepCop enforces
  it, kept strict and up to date.
- **Pay only for what you use.** Assets opt in to engine services through interfaces. A block with no logic costs no
  ticks, checks, or allocations, and an asset that opts in gets exactly what it asked for, nothing more.
- **One interop call per body, not per asset.** Per-tick work reads each assembly's state once, works in C#, and
  writes once (as the ConstructTool's cached `ObbGrid` did for placement).
- **The network is the most important optimization.** Live state is bit-packed and quantized, and only what changed
  is sent.
- **Nodes added at run time get explicit names,** never Godot's generated ones.
- **Baja Builders parity for behavior,** improved where Godot allows.

## 2. Asset authoring

### Where asset scripts live

- Engine-provided, reusable components stay in `Scripts/Assets/Components/` (e.g., `Paintable`, later `Seat`,
  `Airfoil`, `Anchor`).
- Asset-specific scripts sit beside their scene in `build_assets/`: `build_assets/Mechanics/Motor.tscn` and
  `Motor.cs`. Godot requires a script's file name to match its class, and asset scenes are PascalCase **(D1)**, so
  every `X.cs` pairs with its `X.tscn`. `AssetManager` only registers scenes, so the scripts don't disturb its scan.
- With the first asset script, `EnsembleGame.csproj` gets a rule nesting each script under its scene in Rider, as
  `.axaml.cs` nests under `.axaml` (`DependentUpon="%(Filename).tscn"`, with the scenes as `None` items so they can
  parent), and `config.nsdepcop` a rule that nothing but `EnsembleRoot.BuildAssets.*` may reference it.

### Behaviors: components or subclasses

Reusable behaviors are `AssetComponent` nodes dropped under the handle and configured by exports; unique assets
(motor, servo, piston) subclass `AssetHandle`. Both get `Initialize` once their creation exists, and a handle lives in
one mode (build or live) for its whole life.

### Multi-piece assets (motors, hinges, pistons, suspension)

An asset can have several rigid **pieces** that move independently, like a motor's base and rotor.

- The handle's direct colliders form piece 0. Each `AssetPiece` node (a `Node3D` child) is another piece, with its own
  direct colliders and meshes.
- Welding works per piece: the weld graph's nodes become (instance ID, piece index), and an asset's pieces never weld
  to each other (as in BB, where motors kept their head and base apart).
- At spawn, each `AssetPiece` is reparented into its own piece's assembly (the handle rides piece 0's), and the
  handle keeps references to its pieces. Components resolve references in `Initialize`, after reparenting, so the
  tree is never walked later.
- A joint component on the asset (e.g., `Hinge`) links the pieces' assemblies with a Godot joint at an authored anchor
  (a `Marker3D`).
- If a loop of welds puts both pieces in one assembly, the joint would join a body to itself; it's skipped with a
  warning, as BB builds simply locked up.

## 3. Asset logic at runtime

### Ticks

- Opt-in interfaces, not base classes, so a wing component can sit on any asset:
  - `ISimulationTicked.SimulationTick(in SimulationTick tick)` - physics logic (motors, forces, aerodynamics). Runs
    only on the peer simulating the asset's mechanism, at 100 Hz, from one loop per plot in instance-ID order. Not
    simulating means not ticked, so assets never check ownership themselves.
  - Plain Godot `_Process` for visuals on every peer (lights, effects). Normal Godot callbacks stay available, but
    heavy per-tick work belongs in the simulation tick.
- The tick argument carries the infrastructure (body state, force accumulator, input, delta), so an asset implements
  one method and writes no plumbing.
- `PlotHandle` collects the ticked handles and components once at spawn, so 1000 motors cost one engine callback plus
  1000 C# calls per tick.

### State and forces

- `SimulationTick` exposes the assembly's state, read once per tick per assembly: transform, linear and angular
  velocity, center of mass.
- Forces go into a per-assembly C# accumulator (`tick.AddForce(force, position)`, `AddTorque`), flushed with one
  `ApplyForce` / `ApplyTorque` per assembly per tick. A thousand wings make a thousand C# additions, not a thousand
  interop calls.
- Joint motors are driven through their joint (target velocity, max torque), set only when a value changes.

### Ownership-dependent state

Live asset state (a servo's target, a motor's throttle) is derived from inputs and physics, so a new simulator rebuilds
it on hand-off. Anything that must survive a hand-off travels in the replication stream (section 8); nothing else is
promised.

## 4. Mechanisms and joints

Most mechanisms only make sense with input, so they come after it; free hinges and springs (suspension) don't need it
and may come earlier. Some glitching may be accepted at first.

- A **mechanism** is a set of assemblies linked by joints (union-find over joints, after welding). It is the unit of
  simulation ownership, since a joint can't span two machines.
- Joint mapping in Godot with Jolt (to verify in a prototype):

| Asset      | Joint                                      | Driven by                                             |
|------------|--------------------------------------------|-------------------------------------------------------|
| Hinge      | `HingeJoint3D`, free                       | -                                                     |
| Motor      | `HingeJoint3D` with its motor              | Input sets the target velocity; max torque from props |
| Servo      | `HingeJoint3D` with its motor and limits   | A P-controller in the simulation tick toward a target |
| Piston     | `Generic6DOFJoint3D`, one linear axis      | A linear motor toward an extension target             |
| Suspension | `Generic6DOFJoint3D`, a linear spring axis | Stiffness and damping from props                      |

- Strengths are asset properties (non-underscore, so the future AttrTool edits them), scaled for the default density
  (125 kg/m3).
- Joint stability suffers at large mass ratios (a 15 kg wheel on a 3 t car is 200:1). Mitigations: material densities
  for wheels, more Jolt solver iterations, and testing early.

## 5. Input

- **Controller:** each spawned plot has at most one, chosen by occupant order among the plot's occupants sitting in
  control seats, not by who sat down first. The owner is always the oldest occupant, so a seated owner always has
  control. Players who don't occupy the plot never get control. No controller means no input.
- **Input state:** the controller's pressed keys (Godot `Key`s; mouse buttons stay with the camera), from a fixed
  table of up to 64 bindable keys (letters, digits, arrows, Space, Shift, ...), as a 64-bit mask. Every send carries
  the whole state, so presses and releases are found by comparing with the previous mask: no start / stop bits or
  toggles, and a lost packet can't invert a key.
- **Bindings live on assets,** as editable properties suffixed `Key` (e.g., a motor's `forwardKey`, `reverseKey`), so
  the future AttrTool knows to show them as key pickers. Values are Godot `Key` codes as integers: compact in saves and
  packets, shown by name in the UI (harder to hand-edit in JSON saves, an acceptable cost). Assets read
  `tick.Input.IsPressed(Key.Q)`.
- **Flow:** the controller samples its mask each physics tick (empty while the UI sinks input) and sends it to the
  host whenever it changes, plus a few times a second while any key is held, so a lost packet heals itself and a key
  can't stick. It's a custom RPC, not an action: it changes too often for the action rate limiter, and needs no
  validation beyond "the sender is the plot's current controller", which the host checks.
- **Who receives it (D8):** only the creation's **simulator**, via the host; it drives the physics (which way motors
  spin), not the plot owner as such. An asset whose look depends on input replicates that state itself (e.g., a glass
  that turns from water to wine sends a bool to the other peers), and visuals that follow physics (a spinning rotor)
  need nothing extra, since the proxies already move.
- While seated, the controller's character doesn't walk; the camera keeps mouse look and follows the seat.
- **Leaving a seat (D2):** the `character_seat_leave` action, defaulting to Space, consumed only when no seated asset
  binds Space; otherwise the action's own key.

## 6. Seats and characters

- A `Seat` component makes any asset a seat; its `HasControls` export makes it a control seat (the Chair and Control
  Chair assets).
- **Sitting (D3):** on touch, as with Roblox's seats in BB, with a short cooldown after leaving so you don't sit right
  back down. **Anyone may sit in any seat (D4),** but only the plot's occupants can control (section 5).
- **While seated,** the character's body leaves the physics space (the same hop as teleports), is parented to the
  seat's piece at its sit point, and its controller stops moving it. A seated character adds no weight, so a creation
  behaves the same with or without riders.
- **Leaving:** the body re-enters the space beside the seat (pushed clear of the creation like at spawn), and regains
  control.
- **Anything that moves or removes a seated character leaves the seat first:** teleports (H reset, `tp_char`,
  despawn's return to the plot, spawn clearing), despawning, the seat's assembly being killed, the occupant leaving or
  being kicked from the plot. `PlayerHandle.Teleport` calls `LeaveSeat()` before the hop, so every caller gets it.
- **Replication:** a seated character replicates its seat (plot ID, instance ID) instead of its position; every peer
  places it on its own copy of the seat, so it rides proxies smoothly.

### Characters versus creations (research)

Characters are kinematic: creations can't push them, a creation hitting one stops as if it hit a wall, and a walking
character may shove a creation (see the Jolt kinematic note). The goal is the reverse: creations push characters, and
characters never disturb creations. Godot's layers and masks can't make that one-way (a pair collides if either side's
mask matches), and a collision exception would also hide creations from the character's own motion queries, so it
would fall through what it stands on. Options to prototype later:

- **Dynamic characters** (Roblox's model): pushed naturally and ride creations by friction; a large rework of
  `CharacterController`.
- **Kinematic characters that yield:** detect creations moving into the character each step and move it out of the
  way first, so contact rarely builds.
- Accept today's behavior until vehicles make it matter (the current choice). Players deliberately blocking others'
  creations could later be handled by moderation (e.g., per-plot blocklists).

## 7. Physics authority

### Model

- Every mechanism has exactly one **simulator** peer. Everyone else shows proxies: frozen kinematic bodies moved by
  received state, which local characters still collide with and stand on.
- **Default:** a mechanism's simulator is its plot's owner. Every peer computes the default itself, so nothing is
  sent unless an override exists.
- **Overrides:** the host alone sets them (`SetSimulator(plotId, mechanismId, peerId)`), and the host is always the
  fallback. Overrides are live state, so they're RPCs from the host, not Core actions.
- **Epochs:** each plot has an ownership epoch, bumped on every change of any of its simulators and sent with each
  change. State packets carry it, and peers drop packets with an old epoch or from the wrong sender, so a late packet
  from a previous simulator can never move a body.

### Hand-off

1. The host decides (owner left, override set or cleared), bumps the epoch, and broadcasts the new simulator(s).
2. The old simulator, on receipt, freezes its bodies into proxies.
3. The new simulator unfreezes its proxies from their latest received state (position, rotation, velocities) and
   starts sending. A ~100 ms rewind is invisible at walking speeds and acceptable at driving speeds.
4. Until the change reaches everyone, both may simulate briefly; the epoch keeps only the new one's packets.

### Edge cases

- **Owner leaves:** the plot's new owner becomes the default simulator. An empty plot despawns (existing rule), so the
  host fallback only matters for overrides and for owners that stop sending (the host takes over after a timeout).
- **Owner lags or cheats:** the host validates like character updates (finite values, inside the world, speed caps,
  rate limits) and can override.
- **Despawn mid-hand-off:** every peer frees the creation, and pending packets for it are dropped (no such creation).
- **Late joiners:** a live-state snapshot from the host (like `CharactersSnapshot`, at a larger scale) carries each
  spawned plot's epoch, overrides, seated characters, controller input, and the latest state of every assembly, moving
  or asleep, so proxies start exactly in place.
- **Lost packets for sleeping bodies:** a body's last update before sleeping may be lost, so sleeping assemblies get a
  slow round-robin keepalive within a fixed per-plot budget (e.g., 32 assemblies a second), healing stale proxies
  without scaling bandwidth with creation size.

### Shared simulators (later)

Collisions between bodies with different simulators are one-sided. An `Area3D` around each mechanism (Jolt areas are
cheap) reports when two mechanisms with different simulators get close; the host then gives both to one simulator
(the host, or one of the owners) until they separate, with hysteresis. The override API above already supports it.

## 8. Replication

- One unreliable-ordered RPC per plot per tick from the simulator (channel 2; characters use 1), relayed by the host
  to synced peers, as with characters.
- **Bit-packed, not byte-aligned.** Header: plot ID, epoch, sequence. Per moving assembly: ID (the group's lowest
  instance ID), position (quantized, e.g., to 1/512 m relative to the plot), rotation (smallest three, about 10 bits
  each), and linear and angular velocity (quantized). Roughly 20-30 bytes per moving assembly before further tricks
  (sending only changed fields, lower rates for far or slow bodies). Exact bit budgets are set in Phase 2.
- Sleeping assemblies send nothing beyond the keepalive round-robin (section 7).
- Proxies interpolate ~100 ms behind, extrapolate briefly with velocity on loss, and snap when far off. Snaps use the
  space hop (see the Jolt kinematic note), or a snapping proxy would shove nearby bodies.
- Input masks: unreliable full masks on change plus a few repeats a second while held, controller -> host ->
  simulator.
- Seats: reliable sit / leave messages; seated characters stop sending positions.

## 9. Aerodynamics and other forces (later)

- An `Airfoil` component on any asset: area (from its collider bounds by default, overridable), a lift curve
  (linear up to a stall angle, then falling), and drag. Each simulation tick it computes the local airflow from the
  assembly's state (`v + w x r`) and adds lift and drag at its position to the accumulator: a few dozen float
  operations, no interop.
- Thrusters and propellers work the same way: input-driven forces at a point.
- Buoyancy (from material densities, section 11) and optional global air drag fit the same accumulator.
- None of this costs anything for assets without these components.

## 10. Performance

Measured on an i7-9700K, 2000 loose instances (2000 assemblies, the worst case):

| Step                                      | Before (ms) | After a2a576a (ms) |
|-------------------------------------------|-------------|--------------------|
| Prep (read instances, disable static set) | 32          | 33                 |
| Weld groups                               | 29          | 29                 |
| Instantiate members                       | 78          | 78                 |
| Enter the tree (bodies, render instances) | ~140        | 99                 |
| Register shapes                           | 36          | 5                  |
| Initialize components                     | 15          | 22                 |
| Total                                     | ~335        | ~300               |

Welded creations are cheaper (one 2000-member assembly: ~280 ms). The rest is mostly Godot's own cost per node
(~12 us to enter the tree), so the remaining wins are structural:

- Spawning works in one frame, which is simplest (nothing happens mid-build). Spreading it over a few frames (build
  assemblies in batches with frozen bodies, release together) would trade one hitch for several short frames, if the
  hitch ever matters.
- Keep asset scenes lean: every node costs on every spawn, and members can't drop their nodes.
- Measure the static set's disabling separately (the 33 ms prep includes disabling 2000 static bodies).

At runtime: the batched simulation tick, force accumulators, cached body state, and sleeping bodies keep per-tick
work proportional to what moves and thinks, not to what exists.

## 11. Material bank (done)

- A `MaterialBank` node under Main with a static `Instance` (no `Globals` accessor), exporting an array of
  `AssetMaterial` resources edited in the inspector. An asset's `_materialId` indexes it; `-1` (matching `Sentinels`)
  means none, the plain color. Painted materials are shared per material and color (`Paints`), so painted geometry
  still batches.
- **(D5)** An `AssetMaterial` holds the look (`Look`, any `BaseMaterial3D` template, its albedo tinted by the
  instance's color) and the physics (`Density`, `Friction`, `Bounce`), so `Paintable` and the mass calculation read the
  same entry: rocks sink, wood floats, rubber grips, and ice slips. An instance without a material uses its asset's
  `Density`.
- **(D6)** ID 0 is the debug checkerboard (`resources/materials/Debug.tres`, today's look), so existing
  `_materialId = 0` instances keep their look, and new plain assets default to `-1`.
- An assembly has one physics material (Godot sets them per body), blended from its members' (mass-weighted friction,
  minimum bounce). Wheels get grip by being their own assemblies on joints.

## 12. Phases

| Version | Track       | Content                                                                                   |
|---------|-------------|-------------------------------------------------------------------------------------------|
| 0.52.x  | Polish      | Token cost by instance count, `dmp_creation`, spawn speedups, the material bank (done)    |
| 0.53.0  | Multiplayer | Phase 2 with the full authority model, but the host as the only simulator; proxies,       |
|         |             | bit-packed wire format, epochs, keepalives, late joins                                    |
| 0.54.0  | Multiplayer | Phase 3: plot owners simulate; hand-off; host validation and timeouts                     |
| 0.55.0+ | Asset logic | Simulation ticks and accumulators, seats, input masks, then pieces and joints (Hinge,     |
|         |             | Motor, Servo, Piston, Suspension), then Airfoil and other forces                          |

## 13. Decisions

| #  | Decision                         | Resolution                                                        |
|----|----------------------------------|-------------------------------------------------------------------|
| D1 | Asset scene naming               | PascalCase scenes; `X.cs` beside `X.tscn`, nested in Rider        |
| D2 | Leaving a seat                   | `character_seat_leave` (Space), unless a seated asset binds Space |
| D3 | How to sit                       | On touch (BB parity), with a cooldown after leaving               |
| D4 | Who may sit                      | Anyone in any seat; only plot occupants control                   |
| D5 | Texture bank or material bank    | Material bank: look plus physics                                  |
| D6 | Material ID 0                    | Debug checkerboard; plain assets default to -1                    |
| D7 | Multiplayer or asset logic first | Multiplayer first                                                 |
| D8 | Who receives input               | The simulator only; assets replicate input-driven looks           |
