# Plan: Stealth AI Sandbox — Ariverse Studio Technical Test

## Context

**The task:** Build a top-down 2D stealth game in Unity where the player reaches an objective without being detected by AI guards. The brief is **CODE: [STEALTH_AI_SANDBOX]**, Internship 2026 final stage. Deadline **Mon 18 May 2026 23:59** (5 days from today 2026-05-13). Estimated effort **18–25 active hours**.

**Why these design choices:** The brief explicitly rewards (1) extensible architecture — adding a new guard state shouldn't require touching existing code, (2) ScriptableObject-based per-guard config (no MonoBehaviour edits to tune), (3) **decoupled guards** (no direct cross-references), and (4) clear README with diagrams. Visual quality and content count are explicitly *not* graded. Reviewer values *"code that can be extended without major rewrites"* and *"reasoning in README, including weakness acknowledgement"*.

**Confirmed scope (locked):**
- Full required core + **both optionals** (hearing radius #5 + guard-to-guard communication #6)
- FSM: **Class-based `IGuardState` polymorphism** + ScriptableObject `GuardProfile` for per-guard tuning
- Vision: **Mesh-based dynamic cone** (Sebastian Lague style — visual mesh wraps around obstacles, doubles as detection geometry)
- Level: **Tilemap floor/walls + sprite obstacles** (free assets / primitives)
- Fail: **Instant game over → Retry/Quit UI**
- Bonuses: **Audio + Tooling + Polish & Juice** (3 categories)
- Tooling: patrol-route Scene editor, vision/hearing gizmos, GuardProfile custom inspector

**Project state:** Fresh Unity **6000.3.10f1** (Unity 6.3), URP 2D, new Input System installed, default `InputSystem_Actions` already contains Move/Crouch/Sprint/Interact. Empty `Assets/Scripts/`. `SampleScene` empty.

---

## Architecture

### Folder layout (under `Assets/Scripts/`)

```
Scripts/
├── Core/
│   ├── GameManager.cs               # win/lose orchestration
│   ├── ServiceLocator.cs            # OR simple singleton refs (decide during impl)
│   └── EventChannels/
│       ├── AlertEventChannelSO.cs   # guard→guards Alert broadcast
│       ├── NoiseEventChannelSO.cs   # player→guards noise broadcast
│       ├── GameStateEventChannelSO.cs # win/lose/pause
│       └── VoidEventChannelSO.cs    # base parameterless channel
├── Player/
│   ├── PlayerController.cs          # input + Rigidbody2D movement, stance state
│   ├── PlayerNoiseEmitter.cs        # raises NoiseEventChannel based on stance+move
│   └── PlayerStanceSO.cs            # SO per stance: speed, noise radius, sprite scale
├── Guard/
│   ├── GuardController.cs           # MonoBehaviour host — owns StateMachine + Context
│   ├── GuardContext.cs              # plain class, passed to every state (refs+blackboard)
│   ├── GuardProfile.cs              # ScriptableObject — all tunables
│   ├── StateMachine/
│   │   ├── IGuardState.cs
│   │   ├── GuardStateMachine.cs
│   │   └── States/
│   │       ├── PatrolState.cs       # = "Idle" patrol loop
│   │       ├── SuspiciousState.cs
│   │       ├── AlertState.cs
│   │       └── SearchingState.cs
│   ├── Perception/
│   │   ├── GuardVision.cs           # FOV detection logic (angle+dist+raycast)
│   │   ├── FieldOfViewMesh.cs       # runtime mesh fan generation (visual cone)
│   │   └── GuardHearing.cs          # subscribes to NoiseEventChannelSO
│   └── Patrol/
│       ├── PatrolRoute.cs           # MonoBehaviour: List<Vector2> waypoints + loop mode
│       └── IPatrolPath.cs           # interface so future path types can plug in
├── World/
│   ├── ObjectiveZone.cs             # 2D trigger; raises win event
│   └── NoiseSource.cs               # generic interactable that emits noise
├── Audio/
│   ├── AudioManager.cs              # one source pool; PlaySfx(SoundCueSO)
│   ├── SoundCueSO.cs                # clip array + pitch/volume ranges
│   └── MusicDirector.cs             # crossfades calm↔tense based on global alert state
├── UI/
│   ├── HUD.cs                       # objective marker, optional noise meter
│   ├── GameOverScreen.cs            # "Caught" — Retry/Quit
│   ├── WinScreen.cs
│   └── MainMenu.cs
└── Editor/
    ├── PatrolRouteEditor.cs         # Scene-view handles for waypoints
    ├── GuardProfileEditor.cs        # grouped/sliders inspector
    └── GuardGizmos.cs               # OnDrawGizmosSelected for vision/hearing/comm radii
```

### Decoupling via SO Event Channels

Guards **never reference each other**. All cross-actor communication flows through `ScriptableObject` event channels assigned in the inspector:

| Channel | Raised by | Listened by | Payload |
|---|---|---|---|
| `NoiseEventChannelSO` | `PlayerNoiseEmitter`, `NoiseSource` | every `GuardHearing` | `{ Vector2 pos, float intensity }` |
| `AlertEventChannelSO` | `AlertState.Enter` of any guard | every `GuardController` | `{ Vector2 alerterPos, Vector2 lastKnownPlayerPos }` |
| `GameStateEventChannelSO` | `GameManager` | UI, AudioManager, MusicDirector | enum `{Playing, Caught, Won}` |

Each channel: `event Action<Payload> OnRaised;` exposed via `Raise(payload)` method. Receivers subscribe on `OnEnable` and unsubscribe on `OnDisable`.

### State Machine

```csharp
public interface IGuardState {
    void Enter(GuardContext ctx);
    void Tick(GuardContext ctx, float dt);   // called from GuardController.Update
    void Exit(GuardContext ctx);
}
```

`GuardStateMachine` holds `current` state. Transitions are decided **inside each state's `Tick`** (state asks `ctx.Vision.CanSeePlayer()` etc., then `ctx.Machine.ChangeState(new AlertState())`). This means adding a new state = new class implementing `IGuardState` + one transition line where it should fire — **zero changes to existing states**.

**`GuardContext`** holds everything states need (no direct MonoBehaviour cast in states):
- refs: `GuardProfile profile`, `Rigidbody2D rb`, `Transform tr`, `GuardVision vision`, `GuardHearing hearing`, `PatrolRoute route`, `AlertEventChannelSO alertChannel`, `Transform playerTr` (set when seen, read-only otherwise)
- blackboard: `Vector2? lastKnownPlayerPos`, `Vector2? suspicionPoint`, `int currentWaypointIdx`, `float stateTimer`

### State behaviours (summary)

- **PatrolState** — moves through `PatrolRoute` waypoints. Transitions: see player → AlertState; hear noise → SuspiciousState (suspicionPoint = noise pos); receive AlertChannel within commRadius → SuspiciousState (suspicionPoint = lastKnownPlayerPos from event).
- **SuspiciousState** — moves toward `suspicionPoint`, rotates head/look slowly. Transitions: see player → AlertState; timer (`profile.suspicionDuration`) expires → PatrolState.
- **AlertState** — raises `AlertEventChannelSO` on Enter (broadcasts last known to other guards). Moves at alertSpeed toward `lastKnownPlayerPos`. If `vision.CanSeePlayer` → updates `lastKnownPlayerPos` each frame. If reaches lastKnown (no LoS) → SearchingState. If player within `caughtRange` AND visible → raises `Caught` via GameStateChannel.
- **SearchingState** — wanders short randomized offsets around `lastKnownPlayerPos`. Transitions: see player → AlertState; timer (`profile.searchDuration`) expires → PatrolState.

### Vision (mesh + detection)

`GuardVision` runs every `FixedUpdate`:
1. **Detection** (lightweight, runs every tick): vector to player → reject if `dist > range` → reject if `angle > fov*0.5` → `Physics2D.Raycast` against `obstacleLayerMask` → if no hit blocks before player, player is visible.
2. **Mesh cone** (visual, runs every frame): cast **N rays** (`profile.coneResolution`, e.g. 60) in a fan covering `fov` degrees. For each ray, raycast against obstacles; ray endpoint = hitPoint or `range * dir`. Build a triangle-fan mesh (origin + endpoints) and assign to `MeshFilter`. Material is a transparent unlit with color tinted by current state (white→yellow→red→orange).
3. Detection result feeds `ctx.lastKnownPlayerPos` while visible.

### Hearing

`PlayerNoiseEmitter` reads current `PlayerStanceSO.noiseIntensity` × movement magnitude every fixed tick. When intensity > 0, raises `NoiseEventChannelSO` with current player position and intensity.

`GuardHearing` subscribes. On event: if `Vector2.Distance(self, noise.pos) <= profile.hearingRadius * noise.intensity`, sets `ctx.suspicionPoint` and asks `ctx.Machine` to enter `SuspiciousState` (only if currently in PatrolState — Alert/Search ignore hearing).

Stances:
- **Crouch**: speed 0.6×, noiseIntensity 0 (silent)
- **Walk** (default): speed 1×, noiseIntensity 0.5
- **Sprint**: speed 1.5×, noiseIntensity 1.0

### Inter-guard communication

`AlertState.Enter` raises `AlertEventChannelSO`. All `GuardController`s subscribe; on event each one self-filters: ignore if `self == alerter` (compare a `Guid` set in Awake, no direct ref) or `Distance > profile.communicationRadius`. If passes filter, sets `ctx.lastKnownPlayerPos = payload.lastKnown` and enters `SuspiciousState`.

### GuardProfile (ScriptableObject — all tunables)

```
[Header("Movement")]      patrolSpeed, suspiciousSpeed, alertSpeed, rotationSpeed
[Header("Vision")]        visionRange, fovAngle, coneResolution
[Header("Hearing")]       hearingRadius
[Header("Communication")] communicationRadius
[Header("Behaviour")]     suspicionDuration, searchDuration, caughtRange,
                          searchWanderRadius
```

### Player

`PlayerController` reads `Move` (Vector2) + `Crouch` (toggle) + `Sprint` (hold). Stance is `enum { Crouch, Walk, Sprint }`; sprint overrides crouch while held. `Rigidbody2D` `MovePosition` for stable physics-aware movement. Sprite-scale tween on crouch toggle for feedback.

### Win / Lose

- **Win**: `ObjectiveZone` 2D trigger → on `OnTriggerEnter2D(player)` raises `GameStateChannel.Raise(Won)`.
- **Lose**: any guard `AlertState` detects `caughtRange` met with LoS → raises `GameStateChannel.Raise(Caught)`.
- `GameManager` listens, freezes time (`Time.timeScale = 0`), shows respective UI panel.

### Audio

- `AudioManager` — pool of `AudioSource`s, `PlaySfx(SoundCueSO)`. Footstep cue triggered by `PlayerNoiseEmitter` at intervals scaled by stance speed.
- `MusicDirector` — two AudioSources (calm + tense), crossfades volume when global alert count goes 0↔>0. Tracks alert count via `AlertEventChannelSO` and an alert-cleared channel (raised when guard leaves Alert/Search).
- SFX cues: footstep_crouch, footstep_walk, footstep_sprint, guard_spot ("!"), guard_lose_sight ("?"), objective_reached, caught_sting.

### Polish & Juice

- **Camera shake**: simple transform-noise on the main camera (3-line helper, no Cinemachine needed), triggered by detection + caught.
- **Hit-stop**: `Time.timeScale = 0` for 0.1s on caught, then unfrozen as UI fades in.
- **Particles**: "!" exclamation prefab spawned above guard on `AlertState.Enter`; "?" on `SuspiciousState.Enter`.
- **UI fades**: CanvasGroup alpha tween for menu transitions.

### Tooling

1. **`PatrolRouteEditor`** — `[CustomEditor(typeof(PatrolRoute))]`. `OnSceneGUI`: draw `Handles.PositionHandle` per waypoint, line strips between them, `+/-` buttons in inspector to add/remove. Shift+Click in scene to append.
2. **`GuardGizmos`** — on `GuardController.OnDrawGizmosSelected`: arc for vision FOV (colored by current state at runtime, neutral in editor), wire circles for hearingRadius + commRadius, label showing current state name above guard.
3. **`GuardProfileEditor`** — `[CustomEditor(typeof(GuardProfile))]`. Grouped foldouts (Movement/Vision/Hearing/Comm/Behaviour). `Slider` attribute equivalents via `EditorGUILayout.Slider` with sensible min/max.

---

## Critical files to be created (most important)

| File | Why it matters |
|---|---|
| `Guard/StateMachine/IGuardState.cs` + `GuardStateMachine.cs` | The whole "extensible without rewriting" pitch hinges on this contract |
| `Guard/GuardContext.cs` | The "states don't reference MonoBehaviours directly" boundary |
| `Guard/GuardProfile.cs` | The "configurable without touching code" requirement |
| `Core/EventChannels/AlertEventChannelSO.cs` + `NoiseEventChannelSO.cs` | The "guards don't reference each other" requirement |
| `Guard/Perception/GuardVision.cs` + `FieldOfViewMesh.cs` | Vision + occlusion (key mechanic, scored heavily) |
| `Player/PlayerController.cs` + `PlayerNoiseEmitter.cs` | Movement + crouch/sprint + noise emission |
| `Editor/PatrolRouteEditor.cs` | Highest-value tooling piece |
| `README.md` at repo root | **Heavily weighted** — must include state-machine diagram, vision/hearing/comm explanation, reflection section |

---

## Time budget (target ~23h, leaves buffer in 25h cap)

| Phase | Hours |
|---|---|
| Project setup, input wiring, player movement + crouch + sprint | 1.5 |
| Tilemap level + obstacles + objective zone | 2.0 |
| Event channels (SO) scaffolding | 0.5 |
| State machine scaffolding + PatrolState + GuardProfile SO | 2.0 |
| GuardVision detection logic + cone math | 1.5 |
| FieldOfViewMesh runtime mesh generation | 2.0 |
| Suspicious/Alert/Searching states + lastKnown logic | 2.5 |
| Hearing system + noise events | 1.5 |
| Inter-guard communication via AlertChannel | 1.0 |
| Win/Lose + GameManager + UI screens | 2.0 |
| Audio (AudioManager + cues + MusicDirector crossfade) | 2.0 |
| Polish (camera shake, hit-stop, particles, UI fades) | 1.5 |
| Tooling (PatrolRouteEditor + GuardGizmos + GuardProfileEditor) | 2.5 |
| README + state machine diagram + comm/vision diagrams | 1.5 |
| Build .exe, smoke-test, package ZIP, repo cleanup | 1.0 |
| **Total** | **~23h** |

Daily cadence over 5 remaining days (13–17 May): roughly 4.5h/day, sub-25h cap.

---

## Verification (end-to-end)

**Build:**
- `File → Build Settings → Windows x64 → Build` outputs `.exe`.
- Compress to `MalvinLeonardo_STEALTH_AI_SANDBOX_GameProgrammerIntern_20260517.zip` (or actual submit date).

**Playthrough scenarios (manual smoke test):**
1. **Patrol golden path**: load scene, observe both guards looping their patrol routes for 60 sec without state changes.
2. **Vision detection**: walk into vision cone → guard enters Alert (cone turns red), camera shakes, "!" particle pops, music switches to tense.
3. **Occlusion**: stand behind a tilemap wall inside the FOV angle/range → guard does NOT detect (vision cone wraps around wall and stops at obstacle).
4. **Hearing**: sprint outside vision → nearest guard enters Suspicious and walks to last sprint position.
5. **Crouch**: crouch (C) and move past behind cover within hearingRadius → guard ignores (silent).
6. **Last known position**: get spotted, then break LoS by hiding behind a wall → guard moves to where it last saw you, not to your current position.
7. **Communication**: alert guard A → guard B (within commRadius) enters Suspicious and converges on last known position; guard C (outside commRadius) keeps patrolling.
8. **Searching**: after Alert, hide for full searchDuration → guard wanders around lastKnownPos, then returns to patrol.
9. **Caught**: let guard reach you while in Alert → hit-stop + fade → Game Over UI with Retry/Quit.
10. **Win**: reach objective zone → Win UI.

**Architecture verification:**
- Add a dummy 5th state (`InvestigatePropState`) implementing `IGuardState`. Confirm it compiles + can be transitioned to without modifying any existing state class. Delete after verification.
- Duplicate `GuardProfile` SO, tweak `visionRange` from 6→10, assign to one guard only → confirm only that guard's cone is longer (no shared static state).
- Remove one guard from scene → game still runs, no NullRefs from the other guard (decoupling check).

**README diagrams to include:**
- State machine transition diagram (Patrol↔Suspicious↔Alert↔Searching) with trigger labels (see player, hear noise, alert received, timer, reach point).
- Vision cone math (angle + dist + raycast occlusion) — text + small ASCII or sketch.
- Communication flow (Guard A → AlertChannel SO → all subscribed Guards → distance filter).

---

## Risks & weakness acknowledgements (to write up in README "Reflection")

- **Mesh cone perf** under many guards: each guard casts ~60 rays/frame. Mitigation: cap guard count in the level (~3–4); document this. If perf issue surfaces, drop to `FixedUpdate` cadence or reduce `coneResolution`.
- **No NavMesh / pathfinding**: guards use straight-line movement toward targets. Won't navigate around obstacles intelligently. Acknowledge in README; layout level so patrol routes + last-known positions are line-of-sight reachable.
- **Hearing is event-only, not continuous**: a stationary guard won't "build up" suspicion from continuous noise. Documented limitation, simpler model.
- **No save/load**: not implemented (not in scope per choices).
