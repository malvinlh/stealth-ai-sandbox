# Stealth AI Sandbox — Project Audit & Gap Report

## Context

This is the Ariverse Studio Game Programmer Intern 2026 final-stage technical test (`STEALTH_AI_SANDBOX`). Brief: top-down 2D stealth sandbox where the player reaches an objective without being detected by patrolling guards. Deadline **Mon 18 May 2026 23:59** — today is **2026-05-16**, so ~2 days remain. The candidate (Malvin Leonardo) has already written all C# scripts and most Unity wiring; this audit verifies what's actually present in the working tree against the PDF requirements, flags real defects, and recommends balancing.

Stack: Unity 6000.3.10f1, URP 2D, New Input System, ScriptableObject-driven config, event-channel decoupling.

---

## 1. What is implemented correctly

### Core game requirements (PDF §CORE REQUIREMENTS)

| # | Requirement | Status | Evidence |
|---|---|---|---|
| 1 | Top-down movement + crouch/sneak modifier | ✅ | [PlayerController.cs](Assets/Scripts/Player/PlayerController.cs) walk 3 m/s × stance.speedMultiplier (Crouch 0.6 / Walk 1.0 / Sprint 1.5); stances wired in SampleScene at lines 5819–5821 |
| 2 | ≥2 guards w/ configurable patrol | ✅ | Two `Guard.prefab` instances in `Guards` parent (SampleScene); `PatrolRoutes` parent contains two `PatrolRoute` components ([SampleScene.unity#L6964](Assets/Scenes/SampleScene.unity#L6964), [#L7306](Assets/Scenes/SampleScene.unity#L7306)); each guard instance has a `patrolRoute` property override |
| 3 | Vision cone with occlusion | ✅ | [GuardVision.cs](Assets/Scripts/Guard/Perception/GuardVision.cs) does FOV+distance+raycast; [FieldOfViewMesh.cs](Assets/Scripts/Guard/Perception/FieldOfViewMesh.cs) builds a real-time mesh fan via `coneResolution` raycasts against `obstacleLayerMask` |
| 4 | Explicit FSM (no nested if-else) | ✅ | [IGuardState.cs](Assets/Scripts/Guard/StateMachine/IGuardState.cs) + [GuardStateMachine.cs](Assets/Scripts/Guard/StateMachine/GuardStateMachine.cs); one class per state in `States/` — `PatrolState`, `SuspiciousState`, `AlertState`, `SearchingState` |
| 5 | (Optional) hearing radius | ✅ | [GuardHearing.cs](Assets/Scripts/Guard/Perception/GuardHearing.cs) subscribes to `NoiseEventChannelSO`; effective radius scales by `noise.intensity` |
| 6 | (Optional) inter-guard communication | ✅ | [AlertEventChannelSO.cs](Assets/Scripts/Core/EventChannels/AlertEventChannelSO.cs) broadcast; [GuardController.cs:78–92](Assets/Scripts/Guard/GuardController.cs#L78) filters by GUID + `communicationRadius`. No direct guard→guard references |
| 7 | Last known position (not teleport) | ✅ | [AlertState.cs:47–54](Assets/Scripts/Guard/StateMachine/States/AlertState.cs#L47) steps toward `LastKnownPlayerPos`; [SearchingState.cs](Assets/Scripts/Guard/StateMachine/States/SearchingState.cs) wanders the area |
| 8 | Win / Lose conditions | ✅ | [ObjectiveZone.cs](Assets/World/ObjectiveZone.cs) raises `GameState.Won`; [GameManager.cs:59–82](Assets/Scripts/Core/GameManager.cs#L59) handles caught |

### Technical "very good to have" (PDF italics)

- ✅ FSM is extensible — new state = new class + one `ChangeState(...)` call.
- ✅ All per-guard tuning lives in [GuardProfile.cs](Assets/Scripts/Guard/GuardProfile.cs) SO; states/perception read everything via `ctx.Profile`. Zero MonoBehaviour edits needed to retune.
- ✅ Guards never hold direct refs to each other — communication runs entirely through SO event channels.

### Bonus / polish features delivered

- Mesh-based dynamic vision cone (visual + raycast geometry)
- Hit-stop + camera shake on caught ([CameraShake.cs](Assets/Scripts/Core/CameraShake.cs), [GameManager.cs:74–80](Assets/Scripts/Core/GameManager.cs#L74))
- Dynamic music crossfade calm↔tense driven by `AlertEventChannelSO` ([MusicDirector.cs](Assets/Scripts/Audio/MusicDirector.cs))
- SFX pool with round-robin + steal-oldest ([AudioManager.cs:58–74](Assets/Scripts/Audio/AudioManager.cs#L58))
- HUD noise meter ([HUD.cs](Assets/Scripts/UI/HUD.cs)), GameOver/Win screens with CanvasGroup fades
- MainMenu scene + scene-flow wiring (build settings: MainMenu slot 0, SampleScene slot 1)
- Editor tooling: [PatrolRouteEditor.cs](Assets/Scripts/Editor/PatrolRouteEditor.cs) (Shift+Click waypoint append, drag handles), [GuardProfileEditor.cs](Assets/Scripts/Editor/GuardProfileEditor.cs) (grouped sliders), [UIBuilder.cs](Assets/Scripts/Editor/UIBuilder.cs) (`Stealth → Build Scene UI` menu — procedural canvas builder)
- Guard runtime gizmos (vision arc, hearing/comm radius, state label)

---

## 2. Defects and gaps — must fix before submission

These are concrete issues a reviewer would notice. Ordered by severity.

### A. CRITICAL — Guard prefab tagged `Player`

[Assets/Prefab/Guard.prefab:20](Assets/Prefab/Guard.prefab#L20) — `m_TagString: Player`.

Why it matters: five places in the codebase do `GameObject.FindWithTag("Player")` or `CompareTag("Player")`:
- [GuardController.cs:31](Assets/Scripts/Guard/GuardController.cs#L31) — guard searches for player. With this tag, a guard may find itself or another guard instead of the Player.
- [ObjectiveZone.cs:11](Assets/Scripts/World/ObjectiveZone.cs#L11) — **a guard walking over the objective trigger would fire the Win state.** That's a guaranteed cheese the reviewer will hit.
- [NoiseSource.cs:21](Assets/Scripts/World/NoiseSource.cs#L21), [HUD.cs:25 / :45](Assets/Scripts/UI/HUD.cs#L25) — also misroute.

Fix: change Guard prefab `m_TagString` to `Untagged` (or add a `Guard` tag) and re-save the prefab.

### B. HIGH — Guard prefab on Layer 0 (Default)

[Assets/Prefab/Guard.prefab:18](Assets/Prefab/Guard.prefab#L18) — `m_Layer: 0`. Guards collide with each other on the default physics layer and (depending on `obstacleLayerMask` membership of layer 0) could occlude their own / each other's vision rays.

Fix: assign Guard prefab to a dedicated `Guard` layer (the project's `TagManager.asset` defines layers `Player`, `Guard`, `Obstacle` per the wiring audit) and exclude Guard from each other's `obstacleLayerMask` in `DefaultGuardProfile.asset`. The profile currently uses `m_Bits: 8` (layer 3 — Obstacle), which is correct *if* obstacles live on layer 3.

### C. HIGH — Only one SoundCueSO asset exists; AudioManager has 8 slots pointing to it

Filesystem has exactly one cue: `Assets/_game/audio/cues/DefaultSoundCue.asset`. Per the scene audit, AudioManager fields `footstepWalkCue`, `footstepCrouchCue`, `footstepSprintCue`, `guardSpotCue`, `guardSuspiciousCue`, `guardLoseSightCue`, `objectiveReachedCue`, `caughtStingCue` ([AudioManager.cs:13–20](Assets/Scripts/Audio/AudioManager.cs#L13)) all reference the same `DefaultSoundCue.asset`. Audio files exist (`footstep_walk.mp3`, `footstep_sprint.mp3`, `guard_spot.wav`, `guard_suspicious.mp3`, `caught_sting.mp3`, `objective_reached.wav`) but no SoundCueSO wraps them.

Fix: create individual `SoundCueSO` assets per audio file under `Assets/_game/audio/cues/`, populate each with its `AudioClip`, and reassign on AudioManager in SampleScene. (Crouch deliberately silent → that cue can stay unassigned; emitter doesn't call it.)

### D. MEDIUM — `footstepInterval` identical across stances

All three stances have `footstepInterval: 0.4` ([CrouchStance.asset:17](Assets/_game/profiles/CrouchStance.asset#L17), [WalkStance.asset:17](Assets/_game/profiles/WalkStance.asset#L17), [SprintStance.asset:17](Assets/_game/profiles/SprintStance.asset#L17)). Sprint covers more distance per footstep than walk, so the audio cadence won't feel right. Tunable per stance via the SO — see Balancing section.

### E. LOW — `WinScreen` doesn't fade in

[WinScreen.cs](Assets/Scripts/UI/WinScreen.cs) just `SetActive`s, whereas the GameOver path fades CanvasGroup alpha. Cosmetic asymmetry. README boasts "CanvasGroup UI fades" — easy to deliver on.

### F. LOW — README mismatch

README claims the `GameOver` lose condition triggers only on Alert + catch range; verify the AlertState catch math actually matches that description after the tag fix. (Once Guard finds the *real* Player, this should already work — but worth a smoke test.)

### G. INFO — Submission-format checklist

PDF §FORMAT SUBMISSION still requires:
1. **Build to .exe** (Windows x64 Standalone) and ZIP as `MalvinLeonardo_STEALTH_AI_SANDBOX_GameProgrammerIntern_YYYYMMDD.zip`.
2. Upload ZIP to Google Drive (link in the form).
3. Submit the form `forms.gle/tMncGNiqmkSbkrUp7`.
4. Email `hiring@ariversestudio.com` subject `[TES TEKNIS] Game Programmer - Malvin Leonardo`, body with name/position/confirmation.
5. If repo is private, invite `alizulfikar032@gmail.com` and `ziednymubarok1117@gmail.com`.

Not code work but easy to miss.

---

## 3. Game-balancing recommendations

**This is the critical section — the current numbers are tuned for a "1 unit = 1 metre" scale that this project does NOT use.** All values below are ScriptableObject fields, so no code edits.

### Scale facts (from the actual scene)

- Main Camera `orthographic size: 1` ([SampleScene.unity:6570](Assets/Scenes/SampleScene.unity#L6570)) → camera shows **2.00 wu tall × ~3.56 wu wide** at 16:9.
- Tilemap cell size **0.16 wu** ([SampleScene.unity:210](Assets/Scenes/SampleScene.unity#L210)) → **1 wu ≈ 6.25 tiles**, camera shows roughly **12 × 22 tiles**.
- Patrol routes span ~1 wu (~6 tiles each); the playable map is roughly **−1.5 to 1.5 wu in X (~19 tiles wide)** and **−0.7 to 0.3 wu in Y (~6 tiles tall)**, i.e. the entire map fits inside roughly one screen.
- The two patrol routes are LEFT (x ≈ −1.5 … −0.5) and RIGHT (x ≈ 0.5 … 1.4); the player must thread the gap at x ≈ 0.

### Why every current number is wrong

| Field | Current (wu) | In tiles | In screens (camera = 2 wu tall) | Effect |
|---|---|---|---|---|
| `visionRange` | 6 | 37.5 | 3.0 screens / ~2× map width | guard sees through the entire map |
| `hearingRadius` | 4 | 25 | 2.0 screens | guard hears anything anywhere |
| `communicationRadius` | 8 | 50 | 4.0 screens / 2.5× map width | every alert cascades to every guard, instantly |
| `caughtRange` | 0.8 | 5 | half a screen | guard "catches" you from across the room |
| `searchWanderRadius` | 3 | 18.75 | 1.5 screens | searcher wanders to the other side of the map |
| `patrolSpeed` 2 wu/s | — | 12.5 tiles/s | crosses screen vertically in 1.0 s | sprint-speed patrol |
| `alertSpeed` 4 wu/s | — | 25 tiles/s | crosses screen vertically in 0.5 s | uncatchable rocket |
| `walkSpeed` 3 wu/s | — | 18.75 tiles/s | crosses screen vertically in 0.67 s | player teleports |

Rule of thumb: divide every linear value (distance, speed) by ~**5–6×** to match this map's scale. Times (durations, intervals) stay in seconds.

### Recommended player stances (`Assets/_game/profiles/*.asset`)

`PlayerController.walkSpeed` is currently **3 wu/s** at [SampleScene.unity:5822](Assets/Scenes/SampleScene.unity#L5822). **Lower to `0.6` wu/s** (= ~3.75 tiles/s, takes ~3 s to cross the screen vertically — feels deliberate but not sluggish for a stealth game).

| Stance | speedMultiplier | resulting wu/s | resulting tiles/s | noiseIntensity | footstepInterval | rationale |
|---|---|---|---|---|---|---|
| Crouch | 0.6 (keep) | 0.36 | 2.25 | 0.0 (keep) | **0.55 s** | slower cadence sells "sneaking heavily" |
| Walk | 1.0 (keep) | 0.6 | 3.75 | 0.5 (keep) | **0.40 s** | keep |
| Sprint | 1.5 (keep) | 0.9 | 5.6 | 1.0 (keep) | **0.22 s** | rapid cadence so sprint sounds fast AND emits noise more often (intentional risk/reward) |

Stance ratios stay the same — only base `walkSpeed` changes. That way the original design intent (sprint > walk > crouch, with crouch silent) is preserved.

### Recommended Guard profile (`DefaultGuardProfile.asset`)

| Field | Current | **Recommended** | In tiles | Why |
|---|---|---|---|---|
| `patrolSpeed` | 2 | **0.35** | 2.2 tiles/s | slightly slower than player walk → player can outpace by walking |
| `suspiciousSpeed` | 2.5 | **0.45** | 2.8 tiles/s | matches player walk |
| `alertSpeed` | 4 | **0.7** | 4.4 tiles/s | between player walk (0.6) and sprint (0.9): sprint barely escapes — the core stealth feel |
| `rotationSpeed` | 180 | **150** | — | unit is °/s; 150 is snappy but not godlike |
| `visionRange` | 6 | **0.7** | 4.4 tiles | guard sees ~4 tiles ahead — meaningful cone, not omniscient |
| `fovAngle` | 90 | **75** | — | tighter cone → blind spots behind shoulder become real cover |
| `coneResolution` | 60 | 30 – 60 | — | 60 is fine at 2 guards; drop to 30 if FPS dips with more guards |
| `obstacleLayerMask` | bit 3 (Obstacle) | keep, verify | — | confirm tilemap collider is on layer 3 |
| `hearingRadius` | 4 | **0.45** | 2.8 tiles | sprinting player (intensity 1.0) heard from ~3 tiles; walker (0.5) from ~1.4 tiles; crouch silent |
| `communicationRadius` | 8 | **1.2** | 7.5 tiles | covers ~⅔ map width — alerts cascade meaningfully but not automatically |
| `suspicionDuration` | 3 | **2.5** | — | tight map → keep timers short so player isn't punished by long lockouts |
| `searchDuration` | 6 | **5** | — | as above |
| `caughtRange` | 0.8 | **0.12** | 0.75 tile | roughly one sprite-width — feels like a touch |
| `searchWanderRadius` | 3 | **0.35** | 2.2 tiles | wander a few tiles around last-known, not across the whole map |
| `waypointArriveThreshold` | 0.15 | **0.03** | 0.2 tile | proportional precision; otherwise guard "snaps to" waypoint visibly |

### Quick sanity checks after retuning

- Player walks the full map width (3 wu / 0.6 wu/s) in 5 s — good for "tense traversal".
- Guard patrol of ~1 wu (the existing route) at 0.35 wu/s takes ~3 s end-to-end → predictable rhythm the player can learn.
- Alert guard at 0.7 wu/s vs player sprint at 0.9 wu/s: 0.2 wu/s gap means a sprinter just barely pulls away — every 5 s of pursuit, gap widens by 1 tile. Tense but winnable.
- Vision range 0.7 wu against camera height 2 wu = cone reaches **35 % of screen height**. Player at screen edge can see incoming detection.
- Communication 1.2 wu means a guard on the LEFT route entering Alert will *not* automatically trigger the RIGHT-route guard (they are ~2 wu apart at closest) — but if both converge near the centre, they will. Good emergent behaviour.

### Difficulty modes (cheap data-only Poin Plus)

Duplicate `DefaultGuardProfile.asset`:

| Param | Easy | Default | Hard |
|---|---|---|---|
| `visionRange` | 0.55 | 0.7 | 0.9 |
| `fovAngle` | 65 | 75 | 95 |
| `hearingRadius` | 0.35 | 0.45 | 0.6 |
| `communicationRadius` | 0.8 | 1.2 | 1.6 |
| `alertSpeed` | 0.6 | 0.7 | 0.8 |
| `suspicionDuration` | 3.5 | 2.5 | 1.5 |
| `searchDuration` | 4 | 5 | 7 |

A small `DifficultySelector` MonoBehaviour on MainMenu writes a chosen `GuardProfile` reference to a shared SO that each Guard reads on `Start()`. Net code addition: ~40 lines.

### Suggested also (Player only)

`PlayerController.walkSpeed = 0.6` is in [SampleScene.unity:5822](Assets/Scenes/SampleScene.unity#L5822) — change in the scene, not in code. Confirm via Play mode that the rigidbody movement still feels responsive (Unity 6 `rb.linearVelocity` is set directly — no smoothing artefacts at low speeds).

---

## 4. Suggested order of work for the remaining ~2 days

Strictly **bug fixes first**, then optional polish. None of these need code changes except where noted.

1. **Fix Guard prefab tag** — `Player` → `Untagged` (5 min). Smoke-test: walk into Objective with a guard → must NOT win.
2. **Fix Guard prefab layer** — assign `Guard` layer; verify `obstacleLayerMask` still equals `Obstacle` only (5 min).
3. **Create distinct SoundCueSO assets** for footstep_walk, footstep_sprint, guard_spot, guard_suspicious, caught_sting, objective_reached (≈15 min). Reassign in AudioManager.
4. **Per-stance `footstepInterval`** values (2 min).
5. **Initial balancing pass** with the recommended GuardProfile values (10 min, playtest).
6. *(Optional, if time)* Easy/Hard profile variants + MainMenu dropdown — code change required: small `DifficultySelector` MonoBehaviour writes a chosen `GuardProfile` to a shared SO reference loaded into each Guard on Start. (~45 min)
7. *(Optional)* WinScreen CanvasGroup fade-in to match GameOver (~10 min, edit `WinScreen.cs`).
8. **Build to .exe**, ZIP with required naming, upload to Drive, submit form, send email (~30 min).

---

## 5. Critical files reference

If implementing the fixes:
- [Assets/Prefab/Guard.prefab](Assets/Prefab/Guard.prefab) — tag + layer fix
- [Assets/_game/audio/cues/](Assets/_game/audio/cues/) — create new SoundCueSO assets here
- [Assets/Scenes/SampleScene.unity](Assets/Scenes/SampleScene.unity) — reassign cue references on AudioManager GameObject
- [Assets/_game/profiles/CrouchStance.asset](Assets/_game/profiles/CrouchStance.asset), [WalkStance.asset](Assets/_game/profiles/WalkStance.asset), [SprintStance.asset](Assets/_game/profiles/SprintStance.asset) — footstepInterval
- [Assets/_game/profiles/DefaultGuardProfile.asset](Assets/_game/profiles/DefaultGuardProfile.asset) — balancing
- [Assets/Scripts/UI/WinScreen.cs](Assets/Scripts/UI/WinScreen.cs) — optional fade-in
- [README.md](README.md) — update if behaviour changes (especially if difficulty modes are added)

## 6. Verification (end-to-end)

1. **Tag/layer fix.** Drag a guard sprite onto the objective zone in Edit Mode → enter Play → kill the player → guard wanders onto objective → confirm WinScreen does NOT appear.
2. **Patrol smoke test.** Press Play → both guards loop their assigned routes, never overlapping; vision cones render and clip to obstacles.
3. **Hearing.** Crouch through a guard's hearing radius → no reaction. Walk through → reaction at ~2m. Sprint through → reaction at ~4m.
4. **State flow.** Sprint past a guard's vision cone briefly → guard enters Alert, runs to last-known position; lose LOS → guard enters Searching for ~6s → returns to Patrol.
5. **Comms.** Trigger Alert on one guard; another guard within 8m enters Suspicious moving to last-known pos.
6. **Win/Lose UI.** Reach objective → WinScreen. Get caught (touched by Alert guard) → GameOver fades in, retry/main-menu buttons work.
7. **Audio.** Different SFX play for walk vs sprint footsteps, guard spot, guard suspicious, caught sting, objective reached. Music crossfades to tense when any guard is Alert and back to calm shortly after.
8. **Build.** `File → Build Settings → Build` → run the `.exe` → all the above still work outside the Editor.
