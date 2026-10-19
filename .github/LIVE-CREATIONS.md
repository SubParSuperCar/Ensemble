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
- `EnsembleGame.csproj` nests each script under the same-named `.tscn` or `.scn` beside it in the IDE, as `.axaml.cs`
  nests under `.axaml` (the scenes are `None` items, so they can parent). With the first asset script,
  `config.nsdepcop` gets a rule that nothing but `EnsembleRoot.BuildAssets.*` may reference it.

### Behaviors: components or subclasses

Reusable behaviors are `AssetComponent` nodes dropped under the handle and configured by exports; unique assets (motor,
servo, piston) subclass `AssetHandle`. Both get `Initialize` once their creation exists, and a handle lives in
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

- Strengths are asset properties (non-underscore, so the future PropertyTool edits them), scaled for the default
  density (125 kg/m3).
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
  the future PropertyTool knows to show them as key pickers. Values are Godot `Key` codes as integers: compact in saves
  and packets, shown by name in the UI (harder to hand-edit in JSON saves, an acceptable cost). Assets read
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
- **Default:** a creation's simulator is its plot's owner, else the host. The host alone decides and broadcasts each
  change with its epoch, rather than every peer deriving it: one departure can change the owner and clear an override
  at once, and peers handling those in another order would count different epochs.
- **Overrides:** the host alone sets them, per plot for now (`PlotHandle.SetSimulatorOverride`, Lua
  `set_plot_simulator`), and the host is always the fallback. An override lasts until the plot's owner changes, the
  plot despawns, or its peer leaves. Overrides are live state, so they're RPCs from the host, not Core actions.
  Per-mechanism overrides come with mechanisms.
- **Epochs:** each plot has an ownership epoch. Every peer bumps it on spawning and despawning; hand-offs take the
  host's. State packets carry it, and peers drop packets with another epoch or from the wrong sender (the host only
  accepts the simulator's), so a late packet from a previous simulator can never move a body.

### Hand-off

1. The host decides (owner left, override set or cleared, timeout), bumps the epoch, and broadcasts the new simulator
   with it (`RpcSetSimulator`, reliable, to synced peers).
2. The old simulator, on receipt, freezes its bodies into proxies where they are; they hold still until the new
   simulator's states arrive.
3. The new simulator unfreezes its proxies from their latest received state, brought forward to the present, with its
   velocities; bodies that were at rest start asleep, so a hand-off doesn't wake a whole creation. Then it streams.
4. Remaining proxies forget their timeline (another simulator's sequence numbers mean other moments).
5. Until the change reaches everyone, both may simulate briefly; the epoch keeps only the new one's packets.

### Edge cases

- **Owner leaves:** the plot's new owner becomes the default simulator. An empty plot despawns (existing rule), so the
  host fallback only matters for overrides and for owners that stop sending.
- **Simulator stops sending:** simulators send at least a header-only heartbeat a second, and the host takes over a
  creation it heard nothing about for 3 s, as an override (so a flaky connection can't flip it back and forth).
- **Owner lags or cheats:** sessions are invite-only, so checks stay light: the codec only carries finite, bounded
  values (+-16 km, +-256 m/s); the host decodes every packet before relaying it (plot, epoch, at most one packet's
  states) and rate limits relays to 1.5 times what the creation's size lets a simulator send.
- **Despawn mid-hand-off:** every peer frees the creation, and pending packets for it are dropped (no such creation).
- **Late joiners:** `PlotsSnapshot` carries each plot's epoch, simulator, and the latest state of every assembly,
  moving or asleep, so proxies start exactly in place (later: seated characters and controller input).
- **Lost packets for sleeping bodies:** a body's last update before sleeping may be lost, so sleeping assemblies get a
  slow round-robin keepalive within a fixed per-plot budget (e.g., 32 assemblies a second), healing stale proxies
  without scaling bandwidth with creation size.
- **Support despawning (open):** a body handed off at rest starts asleep, and nothing wakes it if what held it up
  despawns: a tower propped against a creation that despawned as its owner left kept leaning on nothing. Waking the
  bodies near a despawning creation would fix it.

### Arbitration: shared simulators (0.55.0, done)

Collisions between bodies with different simulators are one-sided: each simulator sees the other creation as a
kinematic proxy, an immovable wall that arrives late. So when two creations with different simulators get close, the
host gives both to one simulator until they separate, and their collisions become real physics on one machine.

- **Detection, on the host only:** it already has every creation, as proxies or as bodies it simulates, and it
  decides. Every 1/8 s, while two or more plots are spawned, it puts a sphere around each assembly, its bounding
  sphere (half its colliders' bounds diagonal) times 1.5, so the hand-off happens before contact rather than during
  it. Each pair of creations is tested by their spheres' overall bounds first, then only the spheres within the other
  creation's bounds, stopping at the first touching pair (`ArbitrationSpheres`). Proxies' poses are known without
  asking Godot, so only bodies the host simulates cost a read each.
- **Why not areas:** Godot areas were measured first, on 1,462 loose cubes. An `Area3D` per assembly made Jolt track
  every overlap within the creation itself (settled physics went from 0.8 ms to as much as 22 ms a frame), and one
  area per plot, with a sphere shape per assembly, rebuilt its whole compound shape whenever a sphere moved (up to
  260 ms a frame while the pile settled). The tests in C# cost about 0.05 ms a frame on the same pile.
- **Groups:** creations whose spheres overlap link their plots, and linked plots join with union-find
  (`Arbitration.GetLeaders`), so three creations in a pile share one simulator. Each group goes to the simulator its
  creation with the most assemblies has apart from arbitration (**D9**), which already carries the heaviest load, so
  the work stays spread across players rather than piling onto the host; the others get it as their arbitrated
  simulator, through the same broadcast as any hand-off.
- **Release with hysteresis:** a link holds for 2 s after its creations part, and arbitration moves a plot at most
  once every 2 s, so creations jostling at the edge don't flap between machines.
- **Precedence:** a manual or timeout override beats arbitration, which beats the plot's owner. Despawning ends a
  plot's links at once, and a departing simulator loses the arbitrations it held; an owner change only changes whom
  its group goes to.
- **Cost:** nothing in single-player or with fewer than two spawned plots. Otherwise, creations far apart cost a
  bounds test each review, and 1,462 bodies the host simulates cost about 0.4 ms a review, mostly reading their
  transforms.

## 8. Replication

- One unreliable-ordered RPC per plot per tick from the simulator (channel 2; characters use 1), relayed by the host
  to synced peers, as with characters.
- **Bit-packed, not byte-aligned.** Header: plot ID, epoch, sequence. Per moving assembly: ID (the group's lowest
  instance ID), position (1/512 m, relative to the plot), rotation (smallest three), and linear and angular velocity
  (see the bit budget in section 12).
- An assembly's origin is its center of mass, so its position and velocities describe the point it rotates about (with
  the plot's origin, proxies extrapolating a rolling ball swung it meters off course).
- Sleeping assemblies send nothing beyond the keepalive round-robin (section 7).
- **Budget:** at most 256 states per plot per stream (about 130 KB/s per receiver); past it, awake assemblies take
  turns, each sent less often, and bodies that just fell asleep go first.
- **Proxies replay 100 ms behind:** each state is stamped with the simulator's moment (sequence numbers advance every
  stream tick, a whole number of physics ticks) on a local timeline that follows the earliest arrivals. Proxies follow
  a Hermite curve through the buffered positions and velocities (straight across impacts, so nothing dips into what
  it hit), spin rotations by their angular velocities, extrapolate at most 1/10 s when a state is late, and snap when
  far off. Snaps use the space hop (see the Jolt kinematic note), or a snapping proxy would shove nearby bodies.
- **Testing:** Lua `set_net_sim(latency_ms?, jitter_ms?, loss?)` delays (in order) and drops this peer's incoming
  live state, creations and characters, to try poor connections without outside players.
- Input masks: unreliable full masks on change plus a few repeats a second while held, controller -> host ->
  simulator.
- Seats: reliable sit / leave messages; seated characters stop sending positions.

## 9. Aerodynamics and other forces (later)

- An `Airfoil` component on any asset: area (from its collider bounds by default, overridable), a lift curve (linear up
  to a stall angle, then falling), and drag. Each simulation tick it computes the local airflow from the
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

Welded creations are cheaper (one 2000-member assembly: ~280 ms). The rest is mostly Godot's own cost per node (~12 us
to enter the tree), so the remaining wins are structural:

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

| Version | Track       | Content                                                                                |
|---------|-------------|----------------------------------------------------------------------------------------|
| 0.52.x  | Polish      | Token cost by instance count, `dmp_creation`, spawn speedups, the material bank (done) |
| 0.53.0  | Multiplayer | Phase 2 with the full authority model, but the host as the only simulator; proxies,    |
|         |             | bit-packed wire format, epochs, keepalives, late joins                                 |
| 0.54.0  | Multiplayer | Phase 3: plot owners simulate; hand-off; host validation and timeouts (done)           |
| 0.55.0  | Multiplayer | Arbitration: creations close to each other share a simulator (section 7) (done)        |
| 0.56.0+ | Asset logic | Simulation ticks and accumulators, seats, input masks, then pieces and joints (Hinge,  |
|         |             | Motor, Servo, Piston, Suspension), then Airfoil and other forces                       |

### 0.53.0 steps (Phase 2: multiplayer, the host simulating; done)

1. **Authority model:** `PlotHandle.SimulatorPeerId` (the host for now), `IsSimulatedLocally`, and `SimulationEpoch`,
   bumped on spawn and despawn on every peer. The simulator releases its assemblies; everyone else freezes theirs as
   kinematic proxies (`Assembly.IsSimulated`). Overrides wait for 0.54.0, which needs them.
2. **Codec:** `BitWriter` / `BitReader`, `AssemblyState`, and `CreationPackets`, with round-trip tests.
3. **Streaming:** `PlotHandle.Replication.cs`: awake assemblies at 20 Hz on channel 2, chunked below the MTU; the
   host accepts only the plot's simulator and relays to synced peers. Proxies extrapolated briefly with the streamed
   velocities and eased toward that, snapping with the space hop (replaced by a 100 ms replay in 0.54.0).
4. **Sleep keepalives:** a final at-rest state when an assembly falls asleep, then about 40 sleeping assemblies a
   second in turn.
5. **Late joins:** `PlotsSnapshot` carries every plot's epoch (spawned or not, or it would drift at the next spawn) and
   each creation's states. It's part of `PlotsSnapshot` rather than its own snapshot, since snapshots restore in
   registration order and the creation must restore after its plot spawns.
6. **Unblocked and tested:** headless host and clients, the host simulating a tipping tower, a client's proxy
   matching it, a late joiner seeing it in place, and a respawn keeping every epoch in step.

Bit budget per moving assembly (204 bits, about 25.5 bytes; header: plot ID 8 bits, epoch 16, sequence 16, count 16):

| Field            | Encoding                                                          | Bits |
|------------------|-------------------------------------------------------------------|------|
| Assembly ID      | The group's lowest instance ID                                    | 16   |
| Position         | Fixed point relative to the plot origin: 1/512 m steps, +-16384 m | 72   |
| Rotation         | Smallest three: 2-bit index, 3 x 10 bits                          | 32   |
| Linear velocity  | 1/32 m/s steps, +-256 m/s (the host's speed cap)                  | 42   |
| Angular velocity | 1/32 rad/s steps, +-256 rad/s (a fast wheel spins past 100 rad/s) | 42   |

### 0.54.0 steps (Phase 3: plot owners simulate; done)

1. **Proxy accuracy:** assemblies centered on their mass, and proxies replaying 100 ms behind (section 8).
2. **Simulator resolution:** the host picks the override, else the owner's peer, else itself, and broadcasts each
   change with its epoch; late joiners get the simulator in `PlotsSnapshot`.
3. **Hand-off:** `Assembly.HandOff` (section 7), tested with an owner leaving mid-fall.
4. **Overrides:** per plot, host only, Lua `set_plot_simulator`; `dmp_creation` shows the simulator and epoch.
5. **Validation and rate limits:** the host decodes before relaying and caps relays at 1.5 times the allowance.
6. **Timeout:** heartbeats and the host's 3 s takeover, tested by freezing a simulator's process and resuming it.
7. **Budget:** 256 states per plot per stream, in turns past it.
8. **Poor connections:** `NetworkConditions` and Lua `set_net_sim`.

### 0.55.0 steps (arbitration; done)

1. **Bounds:** `Assembly.Bounds`, its colliders' bounds in its own space, measured as it's built.
2. **Detection:** the host's sphere tests every 1/8 s (`ArbitrationSpheres`, with tests against brute force), linking
   plots whose creations are close and holding each link 2 s after they part.
3. **Groups:** `Arbitration.GetLeaders` (union-find; the most assemblies, then the lowest ID), with tests against a
   brute-force flood.
4. **Resolution:** an override, else the arbitrated simulator, else the owner, else the host; `dmp_creation` shows
   whether a creation is arbitrated.
5. **Tested** with headless peers: towers overlapping at spawn join; the leaning one topples away and its neighbor
   returns 2 s later; respawning rejoins; a leaving simulator releases at once; and towers falling into each other
   collide on one machine, ending in the same pose on every peer.

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
| D9 | Who simulates an arbitrated pair | The simulator of the creation with the most assemblies            |
