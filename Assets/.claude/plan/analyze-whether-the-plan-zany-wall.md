# Implementation Status Report — Stealth AI Sandbox

## Context

The plan file at [Assets/.claude/plan/i-need-you-to-frolicking-pie.md](Assets/.claude/plan/i-need-you-to-frolicking-pie.md) describes the full Ariverse Studio internship technical test architecture (FSM guards, mesh vision cone, SO event channels, hearing, comms, tooling, audio, polish). This report verifies what is already implemented vs. what remains, by reading the repo state.

**TL;DR:** All C# code (33 scripts) and the README are done. Zero Unity Editor wiring has been done — the scene is empty, no ScriptableObject instances exist, no prefabs, no level.

---

## What IS Implemented

### Code (all 33 scripts present under `Assets/Scripts/`)

| Plan Area | Files Verified |
|---|---|
| Event Channels | [AlertEventChannelSO.cs](Assets/Scripts/Core/EventChannels/AlertEventChannelSO.cs), [NoiseEventChannelSO.cs](Assets/Scripts/Core/EventChannels/NoiseEventChannelSO.cs), [GameStateEventChannelSO.cs](Assets/Scripts/Core/EventChannels/GameStateEventChannelSO.cs) |
| Core | [GameManager.cs](Assets/Scripts/Core/GameManager.cs), [CameraShake.cs](Assets/Scripts/Core/CameraShake.cs) |
| Guard FSM | [IGuardState.cs](Assets/Scripts/Guard/StateMachine/IGuardState.cs), [GuardStateMachine.cs](Assets/Scripts/Guard/StateMachine/GuardStateMachine.cs), [PatrolState.cs](Assets/Scripts/Guard/StateMachine/States/PatrolState.cs), [SuspiciousState.cs](Assets/Scripts/Guard/StateMachine/States/SuspiciousState.cs), [AlertState.cs](Assets/Scripts/Guard/StateMachine/States/AlertState.cs), [SearchingState.cs](Assets/Scripts/Guard/StateMachine/States/SearchingState.cs) |
| Guard Core | [GuardController.cs](Assets/Scripts/Guard/GuardController.cs), [GuardContext.cs](Assets/Scripts/Guard/GuardContext.cs), [GuardProfile.cs](Assets/Scripts/Guard/GuardProfile.cs) |
| Guard Perception | [GuardVision.cs](Assets/Scripts/Guard/Perception/GuardVision.cs), [FieldOfViewMesh.cs](Assets/Scripts/Guard/Perception/FieldOfViewMesh.cs), [GuardHearing.cs](Assets/Scripts/Guard/Perception/GuardHearing.cs) |
| Patrol | [PatrolRoute.cs](Assets/Scripts/Guard/Patrol/PatrolRoute.cs), [IPatrolPath.cs](Assets/Scripts/Guard/Patrol/IPatrolPath.cs) |
| Player | [PlayerController.cs](Assets/Scripts/Player/PlayerController.cs), [PlayerNoiseEmitter.cs](Assets/Scripts/Player/PlayerNoiseEmitter.cs), [PlayerStanceSO.cs](Assets/Scripts/Player/PlayerStanceSO.cs) |
| World | [ObjectiveZone.cs](Assets/Scripts/World/ObjectiveZone.cs), [NoiseSource.cs](Assets/Scripts/World/NoiseSource.cs) |
| Audio | [AudioManager.cs](Assets/Scripts/Audio/AudioManager.cs), [MusicDirector.cs](Assets/Scripts/Audio/MusicDirector.cs), [SoundCueSO.cs](Assets/Scripts/Audio/SoundCueSO.cs) |
| UI | [HUD.cs](Assets/Scripts/UI/HUD.cs), [GameOverScreen.cs](Assets/Scripts/UI/GameOverScreen.cs), [WinScreen.cs](Assets/Scripts/UI/WinScreen.cs), [MainMenu.cs](Assets/Scripts/UI/MainMenu.cs) |
| Editor Tooling | [PatrolRouteEditor.cs](Assets/Scripts/Editor/PatrolRouteEditor.cs), [GuardProfileEditor.cs](Assets/Scripts/Editor/GuardProfileEditor.cs) |

### Documentation
- [README.md](README.md) is fully written: controls, win/lose, feature checklist, state-machine ASCII diagram, vision cone diagram, hearing table, comm flow, known limitations, reflection, AI tools disclosure.

### Minor deviations from the plan (intentional / acceptable)
- **`GuardGizmos.cs`** doesn't exist as a separate file — gizmos are embedded directly in [GuardController.cs](Assets/Scripts/Guard/GuardController.cs) via `OnDrawGizmos`/`OnDrawGizmosSelected` (also in [NoiseSource.cs](Assets/Scripts/World/NoiseSource.cs) and [PatrolRoute.cs](Assets/Scripts/Guard/Patrol/PatrolRoute.cs)). Functionally equivalent.
- **`VoidEventChannelSO.cs`** (base parameterless channel) not implemented — concrete channels carry their own payloads, so the base is unused.
- **`Core/ServiceLocator.cs`** not implemented — the original plan said "OR simple singleton refs (decide during impl)"; simpler path was chosen.

---

## What Is NOT Implemented (Unity-side wiring only)

Verified by reading [SampleScene.unity](Assets/Scenes/SampleScene.unity): the scene contains only **Main Camera** and **Global Light 2D**. No prefabs anywhere (`Assets/**/*.prefab` returns no files). No `.asset` files except Unity defaults (URP / Renderer2D / DefaultVolumeProfile).

### Missing in scene
- No Player GameObject
- No Guard GameObjects (need at least 2)
- No PatrolRoute GameObjects with waypoints
- No level geometry (Tilemap floors, walls)
- No Obstacle colliders on an `Obstacle` physics layer
- No ObjectiveZone
- No GameManager / AudioManager / MusicDirector / HUD instances
- No Canvas / GameOverScreen / WinScreen / MainMenu
- No NoiseSource interactables (optional)

### Missing ScriptableObject asset instances (none exist on disk)
- `AlertEventChannelSO` instance (e.g. `Assets/_Game/Channels/AlertChannel.asset`)
- `NoiseEventChannelSO` instance
- `GameStateEventChannelSO` instance
- `GuardProfile` instance(s) — at least one, e.g. `DefaultGuardProfile.asset`
- `PlayerStanceSO` instances — three (Crouch / Walk / Sprint)
- `SoundCueSO` instances — footsteps, guard spot, suspicious sting, caught, objective_reached

### Missing project-level setup
- No `Obstacle` physics layer defined (the vision raycast filters against this layer)
- No materials (vision cone needs a transparent unlit material)
- No audio clips imported (footsteps, stings, music)
- Build Settings: `SampleScene` may not be in the scenes list

---

## Manual Setup You Need To Do

Sequenced for fastest path to a playable build. Estimated 6-9 hours.

### 1. Project Settings (~10 min)
- **Layers**: Project Settings → Tags and Layers → add user layer named **`Obstacle`**. (Vision raycast filters on this name.)
- **Physics2D**: Project Settings → Physics 2D → make sure Player and Guard layers can collide with Obstacle.
- **Input Actions**: open [Assets/InputSystem_Actions.inputactions](Assets/InputSystem_Actions.inputactions); confirm/add `Move` (Vector2), `Crouch` (Button), `Sprint` (Button). The PlayerController exposes `InputActionReference` fields — you'll drag these into the inspector once the Player GameObject exists.

### 2. Create ScriptableObject assets (~15 min)
Right-click in `Assets/_Game/Channels/` (create folder):
- **Create → Stealth → Channels → Alert Event Channel** → `AlertChannel.asset`
- **Create → Stealth → Channels → Noise Event Channel** → `NoiseChannel.asset`
- **Create → Stealth → Channels → GameState Event Channel** → `GameStateChannel.asset`

In `Assets/_Game/Profiles/`:
- **Create → Stealth → Guard Profile** → `DefaultGuardProfile.asset` (fill all sliders)
- **Create → Stealth → Player Stance** ×3 → `CrouchStance.asset` (speed 0.6, noise 0), `WalkStance.asset` (speed 1, noise 0.5), `SprintStance.asset` (speed 1.5, noise 1.0)

In `Assets/_Game/Audio/Cues/` (after importing clips):
- **Create → Stealth → Sound Cue** for each: footstep_walk, footstep_sprint, guard_spot, guard_suspicious, caught_sting, objective_reached. (Crouch is silent — no cue.)

Exact menu paths depend on the `[CreateAssetMenu]` attributes — check each SO script for the actual `menuName`.

### 3. Build the Level (~90 min)
- GameObject → 2D Object → Tilemap → Rectangular (creates Grid + Tilemap)
- Paint floor tiles
- Add a second Tilemap child for walls; set its Layer to `Obstacle`; add Tilemap Collider 2D + Composite Collider 2D (Rigidbody2D static)
- Place a few sprite obstacles (squares) on `Obstacle` layer as well

### 4. Build the Player (~30 min)
- Empty GameObject `Player`. Add: SpriteRenderer (assign any sprite), Rigidbody2D (Dynamic, gravityScale 0, freeze rotation Z), CircleCollider2D, **PlayerController**, **PlayerNoiseEmitter**.
- On PlayerController: drag the three PlayerStance SOs, drag input actions, set `walkStance` as default.
- On PlayerNoiseEmitter: drag the `NoiseChannel` SO.
- Set tag `Player`.

### 5. Build a Guard Prefab (~60 min)
- Empty `Guard`. Add SpriteRenderer (face up — sprite forward = transform.up), Rigidbody2D (Dynamic, gravity 0, freeze rotation), CircleCollider2D, **GuardController**, **GuardVision**, **GuardHearing**.
- Child GameObject `Vision` with MeshFilter + MeshRenderer (transparent unlit material) + **FieldOfViewMesh** — assign mesh filter ref to GuardVision.
- On GuardController: drag DefaultGuardProfile, AlertChannel, GameStateChannel, the PatrolRoute (next step).
- On GuardHearing: drag NoiseChannel.
- Drag to `Assets/_Game/Prefabs/Guard.prefab`.
- Place 2-3 instances in scene.

### 6. Patrol Routes (~20 min)
- Empty GameObject `PatrolRoute_A` with **PatrolRoute** component. With it selected, use the Scene-view editor (PatrolRouteEditor.cs) — Shift+Click to append waypoints, drag handles to position.
- Assign to Guard #1.
- Repeat for Guard #2.

### 7. Objective + GameManager (~20 min)
- Empty `Objective` with BoxCollider2D (IsTrigger), **ObjectiveZone** → drag GameStateChannel.
- Empty `GameManager` with **GameManager** + **CameraShake** → drag GameStateChannel, MainCamera ref.

### 8. UI (~60 min)
- Canvas (Screen Space Overlay) + EventSystem
- Child panels with CanvasGroup: `HUD`, `GameOverScreen`, `WinScreen`, `MainMenu` — add their scripts; wire Retry/Quit buttons.
- Drag GameStateChannel into each.

### 9. Audio (~40 min)
- Empty `AudioManager` with **AudioManager** + pool of AudioSources.
- Empty `MusicDirector` with **MusicDirector** + two AudioSources (calm, tense) — drag AlertChannel.
- Drag SoundCue SOs into PlayerNoiseEmitter (footsteps), AlertState/SuspiciousState references (or wherever the code expects them — check each script).

### 10. Build Settings + Smoke Test (~30 min)
- File → Build Settings → drag SampleScene to scenes list → Windows x64 → Build.
- Walk through all 10 scenarios in the plan's "Playthrough scenarios" checklist.

---

## Verification

When wiring is complete, confirm:
1. Press Play → no NullReferenceException in Console.
2. Player moves with WASD; crouch (C) shrinks sprite; sprint (Shift) is faster.
3. Guard cone mesh renders and wraps around walls.
4. Walking into a guard's cone → cone turns red, "!" appears, music shifts.
5. Hiding behind a wall after detection → guard goes to last seen point, then searches, then resumes patrol.
6. Sprint outside vision near a guard → guard enters Suspicious and investigates.
7. Crouch past a guard within hearingRadius → no reaction.
8. Two guards within commRadius → alerting one alerts the other; one outside doesn't react.
9. Reach objective → Win screen. Caught by guard → Game Over screen with Retry/Quit.

If a script's inspector shows an unassigned field, that's the manual wiring step it needs — every `[SerializeField]` field in the scripts is a wire point.
