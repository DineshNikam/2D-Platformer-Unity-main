**NEON RUNNER**

Development Plan & Progress Tracker

*Hobby Marathon Project  |  AI-Assisted  |  6 Phases  |  Living Document*

**ALIGNMENT WITH GDD (same vision)**  
The **product vision** matches **NeonRunner_GDD_v1.md §0**: endless neon runner, auto-shoot, movement skill, chunk variety, ramping difficulty. This tracker is **how** we execute; the GDD is **what** we build. When the repo uses a **technical baseline** (URP, template `PlayerController`, `PlatformChunk` + `HorizontalChunkStreamer`), the GDD’s **§0.1 table** is the bridge—plan tasks **extend** baseline toward the full architecture, not restart from scratch.

**HOW TO USE THIS DOCUMENT**

* This is a LIVING document. Update it after every session.

* Status column: TODO \-\> IN PROGRESS \-\> DONE (or BLOCKED / SKIPPED)

* If a task changes scope, log the change in the Modification Log (end of doc)

* Time estimates are rough — mark actual time spent next to each task as you go

* AI prompts column tells you what to ask the AI to generate for that task

| STATUS | COLOR | MEANING |  |  | HOW TO UPDATE |
| ----- | :---: | ----- | ----- | ----- | ----- |
| \[ \] TODO | Dark Blue | Not started |  |  | Leave as-is until you begin the task |
| \[\~\] IN PROGRESS | Dark Green | Currently working |  |  | Change status when you start the task in a session |
| \[✓\] DONE | Bright Green | Completed |  |  | Mark DONE only when tested and working, not just written |
| \[\!\] BLOCKED | Orange | Stuck / dependency missing |  |  | Log the blocker in Notes column and Modification Log |

# **PROJECT OVERVIEW**

## **Timeline Summary**

| PHASE | NAME | EST. TIME | ACTUAL TIME | START DATE | STATUS |
| :---: | ----- | :---: | :---: | ----- | ----- |
| **1** | Foundation | 3–5 hrs | \_\_\_\_\_ hrs | \_\_\_\_\_\_\_\_\_\_ | \[ \] TODO |
| **2** | Core Combat | 4–6 hrs | \_\_\_\_\_ hrs | \_\_\_\_\_\_\_\_\_\_ | \[ \] TODO |
| **3** | Chunk System | 5–8 hrs | \_\_\_\_\_ hrs | \_\_\_\_\_\_\_\_\_\_ | \[ \] TODO |
| **4** | Camera \+ Feel | 3–5 hrs | \_\_\_\_\_ hrs | \_\_\_\_\_\_\_\_\_\_ | \[ \] TODO |
| **5** | Systems \+ Pooling | 4–6 hrs | \_\_\_\_\_ hrs | \_\_\_\_\_\_\_\_\_\_ | \[ \] TODO |
| **6** | Polish \+ Build | 4–6 hrs | \_\_\_\_\_ hrs | \_\_\_\_\_\_\_\_\_\_ | \[ \] TODO |
| **TOTAL** | **Full Game** | **23–36 hrs** | \_\_\_\_\_ hrs |  |  |

*💡 Time estimates assume working sessions of 2–4 hours. AI will accelerate code generation significantly — budget extra time for debugging and integration.*

*⚠ Do not move to the next phase until current phase's MUST HAVE tasks are marked DONE and manually tested.*

## **AI Tool Assignment**

| TASK TYPE | AI TOOL | HOW TO USE IT |
| ----- | ----- | ----- |
| C\# Code generation | Claude / Cursor / ChatGPT | Paste GDD section \+ ask for implementation. Provide existing code for context. |
| Debugging Unity errors | Claude / ChatGPT | Copy full error log \+ relevant code. Always include Unity version. |
| Pixel art sprites | Midjourney / Bing Creator | Use prompt templates from GDD Section 9\. Export as PNG, clean up in Aseprite. |
| SFX generation | ElevenLabs SFX / BFXR | BFXR for retro 8-bit. ElevenLabs for more complex sounds. Export WAV. |
| Music loops | Suno AI / Udio | Prompt: synthwave chiptune loop, 140bpm, 2 min, endless runner game, no vocals |
| Chunk design advice | Claude | Describe difficulty target \+ ask for platform arrangement ideas or enemy placement. |
| Architecture review | Claude | Paste class diagram from GDD \+ ask "does this architecture have issues?" |

# **PHASE 1: FOUNDATION**

*Goal: Get a working player that moves and jumps in a basic scene. No enemies, no shooting. Just movement feeling great.*

**Exit Criteria: Player runs left/right, double-jumps, has coyote time, and feels good on both keyboard and Android touch.**

## **Phase 1 Task Board**

| ID | TASK | EST | PRIORITY | STATUS | AI PROMPT / NOTES |
| :---: | ----- | :---: | :---: | :---: | ----- |
| **P1-01** | Create new Unity 6 project (2D, **URP** — *skip if using this repo*) | 20m | HIGH | \[ \] TODO | **This repo:** Unity 6 + URP (`com.unity.render-pipelines.universal`). Mark DONE when confirmed. |
| **P1-02** | Install Vladislav-EG controller via UPM git URL | 20m | HIGH | \[ \] TODO | **Defer** until migrating off template. Today: `PlayerController.cs` + Rigidbody2D. Git path: `https://github.com/Vladislav-EG/Flexible2DCharacterControllerForUnity.git?path=/Assets/PlatformerController2D` |
| **P1-03** | Set up PlayerControllerStats ScriptableObject | 30m | HIGH | \[ \] TODO | Right-click \> Create \> PlayerControllerStats. Use GDD Section 3.3 values |
| **P1-04** | Build Player hierarchy in scene (see GDD 3.2) | 30m | HIGH | \[ \] TODO | AI: "How do I set up the Vladislav-EG Flexible Controller player hierarchy in Unity 6?" |
| **P1-05** | Configure physics material (No Friction) | 15m | HIGH | \[ \] TODO | Create \> Physics Material 2D. Friction=0, Bounciness=0. Attach to Body collider |
| **P1-06** | Configure ground layer \+ GroundLayer in SO | 15m | MED | \[ \] TODO | Layer: "Ground". Add to PlayerControllerStats GroundLayer mask |
| **P1-07** | Set up Pixel Perfect Camera (1280x720, 32PPU) | 20m | HIGH | \[ \] TODO | Add PixelPerfectCamera component. Install 2D Pixel Perfect package if needed |
| **P1-08** | Generate player placeholder sprite (AI) | 30m | MED | \[ \] TODO | Midjourney: "16x16 pixel art running character, neon purple, dark bg, transparent" |
| **P1-09** | Configure Android build settings | 30m | HIGH | \[ \] TODO | Build Settings \> Android. Set min SDK 29 (Android 10). Enable IL2CPP |
| **P1-10** | Set up Android on-screen controls (Input System) | 45m | HIGH | \[ \] TODO | AI: "Add On-Screen Stick and On-Screen Button for Unity Input System 2D platformer mobile" |
| **P1-11** | Tune movement feel (run speed, jump height) | 45m | HIGH | \[ \] TODO | Playtest with GDD starting values. Adjust until it feels snappy |
| **P1-12** | Test coyote time and jump buffer | 15m | HIGH | \[ \] TODO | Walk off edge, delay jump input — confirm forgiveness window |
| **P1-13** | Build prototype scene (flat ground \+ some platforms) | 30m | MED | \[ \] TODO | Use tilemap. Drag GridForScene prefab. Open Tile Palette in Scene window |
| **P1-14** | PLAYTEST: Movement on keyboard | 20m | HIGH | \[ \] TODO | Does it feel good? Any jank? Fix before continuing |
| **P1-15** | PLAYTEST: Movement on Android device / emulator | 20m | HIGH | \[ \] TODO | Build APK. Install. Test touch controls. Fix any input issues |

## **Phase 1 — AI Prompts Reference**

### **▸ Controller Setup**

* Ask Claude: "I'm using Vladislav-EG Flexible 2D Platformer Controller in Unity 6\. Help me set up the object hierarchy from scratch, including all required components, scripts, and the PlayerControllerStats scriptable object configuration."

* Ask Claude: "What is the correct git URL to install Vladislav-EG Flexible2DCharacterControllerForUnity as a UPM package in Unity 6?"

### **▸ Android Controls**

* Ask Claude: "How do I add touch controls using Unity Input System On-Screen Button and On-Screen Stick for a 2D platformer, connected to the existing InputActionAsset from the Vladislav-EG controller?"

## **Phase 1 — Notes & Session Log**

| DATE | SESSION NOTES |
| ----- | ----- |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |

# **PHASE 2: CORE COMBAT**

*Goal: Player auto-shoots. Bullets travel and hit things. A basic enemy exists, takes damage, and dies with satisfying feedback.*

**Exit Criteria: Player shoots automatically, bullets destroy on hit, one enemy type (Crab) patrols and takes damage.**

## **Phase 2 Task Board**

| ID | TASK | EST | PRIORITY | STATUS | AI PROMPT / NOTES |
| :---: | ----- | :---: | :---: | :---: | ----- |
| **P2-01** | Create IDamageable interface | 15m | HIGH | \[ \] TODO | AI: "Write C\# IDamageable interface for Unity 2D with TakeDamage(float) method" |
| **P2-02** | Create ShootingSystem.cs (auto-fire) | 45m | HIGH | \[ \] TODO | AI: "Write Unity C\# ShootingSystem that auto-fires on a timer, spawns bullets from GunPoint, uses player facing direction" |
| **P2-03** | Create Bullet.cs (travel \+ hit) | 30m | HIGH | \[ \] TODO | AI: "Write Bullet.cs that moves horizontally, hits IDamageable targets, returns to pool on hit or timer expiry" |
| **P2-04** | Create BulletPool using Unity ObjectPool\<T\> | 30m | HIGH | \[ \] TODO | AI: "Implement Unity 6 ObjectPool\<Bullet\> for bullet pooling with Prewarm(20)" |
| **P2-05** | Generate bullet sprite (AI) | 20m | MED | \[ \] TODO | Midjourney: "8x4 pixel art bullet, neon cyan glow, dark bg, transparent, 2D game sprite" |
| **P2-06** | Create EnemyBase.cs abstract class | 30m | HIGH | \[ \] TODO | AI: "Write abstract EnemyBase MonoBehaviour with IDamageable, HP, flash-white coroutine, Die() with pool return" |
| **P2-07** | Create CrabEnemy.cs (patrol \+ mine) | 60m | HIGH | \[ \] TODO | AI: "Implement CrabEnemy extending EnemyBase: patrol between 2 waypoints, drop mine every 2.5s" |
| **P2-08** | Create Mine.cs (proximity trigger) | 30m | MED | \[ \] TODO | AI: "Write Mine.cs: waits 3s, triggers on player proximity 1.2 units, deals 1 damage, destroys self" |
| **P2-09** | Generate Crab enemy sprite (AI) | 30m | MED | \[ \] TODO | Midjourney: "24x16 pixel art crab enemy, neon orange, dark bg, pixel game sprite, top-down facing right" |
| **P2-10** | Create HealthSystem.cs on PlayerBrain | 30m | HIGH | \[ \] TODO | AI: "Write HealthSystem C\# class: max HP, TakeDamage with invincibility frames, Heal, OnDied C\# event" |
| **P2-11** | Connect PlayerBrain to HealthSystem \+ ShootingSystem | 30m | HIGH | \[ \] TODO | Wire up all references in Inspector. Test in play mode. |
| **P2-12** | Generate hit SFX (AI) | 20m | MED | \[ \] TODO | BFXR: Generate "Explosion" and "Hit" sounds. Export WAV |
| **P2-13** | Generate shoot SFX (AI) | 15m | MED | \[ \] TODO | BFXR: Generate "Laser" sound. Short, punchy. |
| **P2-14** | Create AudioManager.cs (basic) | 30m | MED | \[ \] TODO | AI: "Simple Unity AudioManager singleton with PlaySFX(AudioClip) and PlayBGM(AudioClip) methods" |
| **P2-15** | PLAYTEST: Full combat loop (move \+ shoot \+ kill) | 30m | HIGH | \[ \] TODO | Does shooting feel good? Does crab die correctly? Are there any NullRef errors? |

## **Phase 2 — Notes & Session Log**

| DATE | SESSION NOTES |
| ----- | ----- |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |

# **PHASE 3: CHUNK SYSTEM**

*Goal: Level is infinite. Pre-built chunk prefabs scroll and are destroyed behind the player. Difficulty scales as chunks pass.*

**Exit Criteria: 6 different chunks cycle infinitely for 5+ minutes without gaps, crashes, or visual seams.**

## **Phase 3 Task Board**

| ID | TASK | EST | PRIORITY | STATUS | AI PROMPT / NOTES |
| :---: | ----- | :---: | :---: | :---: | ----- |
| **P3-01** | Design Chunk prefab structure (StartPoint, EndPoint) | 30m | HIGH | \[ \] TODO | Create empty Chunk prefab. Add StartPoint and EndPoint child transforms. See GDD Section 7.2 |
| **P3-02** | Create ChunkManager.cs | 60m | HIGH | \[ \] TODO | AI: "Write ChunkManager MonoBehaviour: maintains 3 active chunks, spawns at EndPoint, destroys behind player, tracks chunksPassed" |
| **P3-03** | Build Chunk 1: Flat\_Open\_01 (intro chunk) | 45m | HIGH | \[ \] TODO | Wide flat ground, 1 CrabEnemy spawn point. This is the first chunk player always sees |
| **P3-04** | Build Chunk 2: Gap\_Jump\_01 | 45m | HIGH | \[ \] TODO | Two gaps, 3 platforms. 1 Crab \+ 1 Fish spawn point |
| **P3-05** | Create FishEnemy.cs (formation \+ arc projectile) | 60m | HIGH | \[ \] TODO | AI: "Write FishEnemy extending EnemyBase: follows leader in V-formation, leader fires arc projectile every 3s" |
| **P3-06** | Build Chunk 3: Multi\_Level\_01 | 45m | MED | \[ \] TODO | 3 height levels. 2 Crab \+ 1 FishGroup |
| **P3-07** | Build Chunk 4: Air\_Gauntlet\_01 | 45m | MED | \[ \] TODO | Airborne focus. 3 FishGroups. Minimal ground. |
| **P3-08** | Build Chunk 5: Spike\_Run\_01 | 45m | MED | \[ \] TODO | Add spike hazard sprites. 2 Crab with tight platforms. |
| **P3-09** | Build Chunk 6: Chaos\_01 | 45m | MED | \[ \] TODO | Max enemies. Dense chunk. Should feel overwhelming. |
| **P3-10** | Implement difficulty tier system in ChunkManager | 45m | HIGH | \[ \] TODO | AI: "Add difficulty tier to ChunkManager: tiers 1-4 based on chunksPassed, adjust enemy count and speed multiplier" |
| **P3-11** | Generate Fish enemy sprite (AI) | 30m | MED | \[ \] TODO | Midjourney: "20x12 pixel art fish enemy, neon teal, facing right, dark bg, transparent" |
| **P3-12** | Add EnemyPool for Crab and Fish | 30m | HIGH | \[ \] TODO | Pool size: Crab=6, Fish=15. See GDD Section 10.4 |
| **P3-13** | Add camera left-boundary collider that moves with camera | 20m | HIGH | \[ \] TODO | AI: "Create a script that moves a BoxCollider2D to match camera left edge, preventing player from going left" |
| **P3-14** | PLAYTEST: 5-minute infinite run | 30m | HIGH | \[ \] TODO | Let it run for 5 min. Watch for: gaps between chunks, object leak, frame drops, wrong chunks spawning |
| **P3-15** | PLAYTEST: Difficulty scaling visible? | 20m | HIGH | \[ \] TODO | Is it noticeably harder after chunk 25? After chunk 50? |

## **Phase 3 — Notes & Session Log**

| DATE | SESSION NOTES |
| ----- | ----- |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |

# **PHASE 4: CAMERA \+ GAME FEEL**

*Goal: Camera follows the player perfectly. Every hit event triggers satisfying feedback. The game starts feeling like a real game.*

**Exit Criteria: Camera never goes left, smooth follow works, time freeze and screen shake trigger correctly on all events.**

## **Phase 4 Task Board**

| ID | TASK | EST | PRIORITY | STATUS | AI PROMPT / NOTES |
| :---: | ----- | :---: | :---: | :---: | ----- |
| **P4-01** | Create CameraSystem.cs (right-only follow) | 45m | HIGH | \[ \] TODO | AI: "Write CameraSystem MonoBehaviour: follow player, never decrease camera X, SmoothDamp horizontal, Lerp vertical, apply facing offset" |
| **P4-02** | Configure PixelPerfectCamera component | 20m | HIGH | \[ \] TODO | Resolution 1280x720, PPU 32, CropFrame Stretchfill, GridSnapping ON |
| **P4-03** | Add facing look-ahead offset to camera | 20m | MED | \[ \] TODO | When facing right: \+2 unit offset. Smooth transition between offsets. |
| **P4-04** | Create GameFeel.cs singleton (Freeze \+ Shake) | 45m | HIGH | \[ \] TODO | AI: "Write GameFeel MonoBehaviour singleton with Freeze(duration, timeScale) and Shake(intensity, duration) using coroutines. Use unscaledDeltaTime for shake." |
| **P4-05** | Wire freeze/shake to all hit events (see GDD 4.3 matrix) | 30m | HIGH | \[ \] TODO | Subscribe to PlayerBrain.OnDamaged, enemy TakeDamage calls. Match values in GDD table. |
| **P4-06** | Add bullet trail renderer | 20m | MED | \[ \] TODO | Add TrailRenderer to bullet prefab. Short trail, neon cyan color, fades in 0.1s |
| **P4-07** | Create explosion particle system (enemy death) | 30m | MED | \[ \] TODO | AI: "What are good Unity ParticleSystem settings for a neon pixel explosion effect (burst, small squares, bright colors)?" |
| **P4-08** | Create hit particle (bullet hits enemy, smaller) | 20m | MED | \[ \] TODO | Smaller version of explosion. Star burst shape. |
| **P4-09** | Add white flash coroutine to EnemyBase | 20m | HIGH | \[ \] TODO | AI: "Write C\# coroutine that briefly replaces sprite material with white flash material in Unity 2D" |
| **P4-10** | Add red vignette pulse on player hit | 25m | MED | \[ \] TODO | UI Image with red radial gradient, alpha pulses from 0.7 to 0 over 0.3s |
| **P4-11** | Add invincibility frame visual (player blink) | 20m | MED | \[ \] TODO | Flash SpriteRenderer.enabled on/off during i-frames. 0.1s toggle. |
| **P4-12** | Add main BGM (AI generated loop) | 20m | HIGH | \[ \] TODO | Suno AI prompt: "synthwave chiptune, 140 bpm, intense, endless runner, no lyrics, 2 min loop" |
| **P4-13** | PLAYTEST: Does every hit feel impactful? | 30m | HIGH | \[ \] TODO | Play through. Every bullet hit, every player hit, every death. Feels punchy? Adjust values. |
| **P4-14** | PLAYTEST: Camera feel (no jank, no left movement) | 20m | HIGH | \[ \] TODO | Look-ahead offset transitions smoothly? Camera never dips left? No pixel jitter? |

## **Phase 4 — Notes & Session Log**

| DATE | SESSION NOTES |
| ----- | ----- |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |

# **PHASE 5: SYSTEMS \+ POWER-UPS**

*Goal: Power-ups spawn, are collected, and work correctly. Score system tracks and saves. UI is functional. Everything integrates cleanly.*

**Exit Criteria: All 4 power-ups work. Score displays live. High score persists. No crashes over 10 minutes of play.**

## **Phase 5 Task Board**

| ID | TASK | EST | PRIORITY | STATUS | AI PROMPT / NOTES |
| :---: | ----- | :---: | :---: | :---: | ----- |
| **P5-01** | Create IPowerUp interface \+ PowerUpType enum | 20m | HIGH | \[ \] TODO | AI: "Write C\# IPowerUp interface with Activate(PlayerBrain), Deactivate(), Duration float. Write PowerUpType enum with Shield, Health, DamageBoost, BlastBullets" |
| **P5-02** | Implement ShieldPowerUp.cs | 30m | HIGH | \[ \] TODO | Blocks next 2 hits. Neon ring visual on player. Deactivates after 2 hits absorbed. |
| **P5-03** | Implement HealthPowerUp.cs | 20m | HIGH | \[ \] TODO | Instant: restore 30-50% max HP. Clamp to max. Fire UI update event. |
| **P5-04** | Implement DamageBoostPowerUp.cs | 25m | HIGH | \[ \] TODO | Multiply ShootingSystem damage by 2 for 5s. Change bullet sprite to larger orange variant. |
| **P5-05** | Implement BlastBulletsPowerUp.cs | 35m | HIGH | \[ \] TODO | Enable isBlast flag on ShootingSystem. Bullet.OnHit does OverlapCircle and hits all IDamageable in 2 unit radius. |
| **P5-06** | Create PowerUpSystem.cs (manager) | 30m | HIGH | \[ \] TODO | AI: "Write PowerUpSystem MonoBehaviour: activate/deactivate one IPowerUp at a time, replacing previous if new one collected" |
| **P5-07** | Create PowerUpPickup.cs (world item) | 20m | HIGH | \[ \] TODO | Trigger collider. On player enter: call PowerUpSystem.Activate(type). Return to pool. |
| **P5-08** | Create PowerUpManager.cs (weighted spawn) | 30m | HIGH | \[ \] TODO | AI: "Write weighted random selector in C\# for PowerUpType with weights: Health=35, Shield=30, DamageBoost=20, Blast=15" |
| **P5-09** | Wire power-up drop to enemy death | 20m | HIGH | \[ \] TODO | In EnemyBase.Die(): if Random.value \< dropChance \-\> PowerUpManager.Instance.SpawnAt(pos) |
| **P5-10** | Generate power-up pickup sprites (4 types, AI) | 45m | MED | \[ \] TODO | Midjourney: "16x16 pixel art power-up item, \[cyan shield / green heart / orange fire / pink star\], dark bg, pixel game" |
| **P5-11** | Create ScoreManager.cs | 25m | HIGH | \[ \] TODO | AI: "Write ScoreManager with AddTime(float), AddKill(float), GetScore(), SaveHighScore() using PlayerPrefs" |
| **P5-12** | Wire score increments to gameplay events | 20m | HIGH | \[ \] TODO | Every Update: AddTime(Time.deltaTime \* 10). Every kill: AddKill(enemyHP \* 5). |
| **P5-13** | Create GameManager.cs (state machine) | 35m | HIGH | \[ \] TODO | AI: "Write GameManager with states: StartScreen, Playing, GameOver. Subscribe to PlayerBrain.OnDied to trigger GameOver." |
| **P5-14** | PLAYTEST: All 4 power-ups behave correctly | 30m | HIGH | \[ \] TODO | Test each one. Does one-at-a-time rule work? Does UI update? |
| **P5-15** | PLAYTEST: 10-minute session — no crashes | 20m | HIGH | \[ \] TODO | Use Unity Profiler. Watch for memory leaks. Object pools not growing? |

## **Phase 5 — Notes & Session Log**

| DATE | SESSION NOTES |
| ----- | ----- |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |

# **PHASE 6: POLISH \+ BUILD**

*Goal: The game is complete. UI screens work. Audio plays. The Android APK installs and is playable start-to-finish. Declare it done.*

**Exit Criteria: Player can play from Start Screen through multiple runs with no crashes. Android APK works. Game is "shippable" as a hobby project.**

## **Phase 6 Task Board**

| ID | TASK | EST | PRIORITY | STATUS | AI PROMPT / NOTES |
| :---: | ----- | :---: | :---: | :---: | ----- |
| **P6-01** | Create UIManager.cs (screen management) | 45m | HIGH | \[ \] TODO | AI: "Write UIManager with ShowStartScreen(), ShowHUD(), ShowGameOver(score, high), UpdateHP(int), ShowPowerUpIcon(type, duration)" |
| **P6-02** | Design Start Screen UI (logo \+ start button \+ best score) | 30m | HIGH | \[ \] TODO | Canva AI or Midjourney for logo art. Simple layout. Tap anywhere to start. |
| **P6-03** | Design HUD (HP hearts \+ score \+ power-up icon) | 30m | HIGH | \[ \] TODO | See GDD Section 11.2 layout. Bottom touch controls remain visible. |
| **P6-04** | Design Game Over screen (score \+ high score \+ restart) | 20m | HIGH | \[ \] TODO | Neon overlay. Clear button. Show if new high score. |
| **P6-05** | Add HP heart sprites to HUD (full \+ empty) | 20m | HIGH | \[ \] TODO | Midjourney: "8x8 pixel art heart icon, neon pink, full version and empty version, dark bg" |
| **P6-06** | Add power-up timer bar to HUD | 25m | HIGH | \[ \] TODO | Unity Image with fillAmount. Counts down over powerUp.Duration |
| **P6-07** | Wire UIManager to GameManager state events | 20m | HIGH | \[ \] TODO | OnStateChanged \-\> UIManager shows correct screen. OnScoreChanged \-\> update score text. |
| **P6-08** | Add background art (parallax layers, AI generated) | 45m | MED | \[ \] TODO | Midjourney: "pixel art parallax bg layer: far city / mid buildings / close pipes, neon, dark navy, 1280x720". 3 layers. |
| **P6-09** | Implement simple parallax scroll | 30m | MED | \[ \] TODO | AI: "Write Unity ParallaxBackground script: 3 layers at different scroll speeds, loop seamlessly" |
| **P6-10** | Final audio pass (all events have SFX) | 30m | HIGH | \[ \] TODO | Walk through GDD Section 12.1. Every SFX connected? Volume balanced? |
| **P6-11** | Performance pass: profiler check on Android | 30m | HIGH | \[ \] TODO | Target 60fps on Android 10+. Address any draw call or GC issues. Use Unity Profiler over USB. |
| **P6-12** | Final Android APK build | 30m | HIGH | \[ \] TODO | Build Settings \> Android \> Build. Sign APK (keystore). Test install. |
| **P6-13** | PLAYTEST: Full game loop start-to-finish (3 runs) | 30m | HIGH | \[ \] TODO | Start Screen \-\> Play \-\> Die \-\> Restart x3. Any bugs? Any missing feedback? |
| **P6-14** | Bug fix pass (any issues from final playtest) | 60m | HIGH | \[ \] TODO | Timebox to 1 hour. Log unfixed bugs in Backlog section below. |
| **P6-15** | OPTIONAL: Add settings screen (volume slider) | 45m | LOW | \[ \] TODO | Only if time allows. Basic Canvas panel with AudioMixer volume control. |
| **P6-16** | OPTIONAL: Add parallax animated star/dust background | 30m | LOW | \[ \] TODO | Simple particle system emitting slow-moving points behind gameplay. |
| **P6-17** | DECLARE DONE — celebrate\! | 5m | HIGH | \[ \] TODO | You built a game. With AI. From scratch. That's impressive. |

## **Phase 6 — Notes & Session Log**

| DATE | SESSION NOTES |
| ----- | ----- |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |
| \_\_\_\_\_\_\_\_\_\_ |  |

# **MODIFICATION LOG**

*Record every change to the plan here. Include: what changed, why it changed, and what was affected. This keeps the project history intact.*

*💡 Never delete old entries. If a task is removed, mark it SKIPPED and add a reason here.*

| \# | DATE | TASK/SECTION CHANGED | WHAT CHANGED | REASON / IMPACT |
| :---: | ----- | ----- | ----- | ----- |
| 1 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 2 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 3 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 4 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 5 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 6 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 7 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 8 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 9 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 10 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 11 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 12 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 13 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 14 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |
| 15 | \_\_\_\_\_\_\_\_\_\_ |  |  |  |

# **BACKLOG & KNOWN BUGS**

*Log bugs and future ideas here. Bugs get fixed in Phase 6 or future patches. Ideas go in "Future Scope".*

## **Bug Tracker**

| \# | SEVERITY | BUG DESCRIPTION | PHASE FOUND | STATUS / FIX |
| :---: | ----- | ----- | ----- | ----- |
| 1 |  |  |  |  |
| 2 |  |  |  |  |
| 3 |  |  |  |  |
| 4 |  |  |  |  |
| 5 |  |  |  |  |
| 6 |  |  |  |  |
| 7 |  |  |  |  |
| 8 |  |  |  |  |
| 9 |  |  |  |  |
| 10 |  |  |  |  |

## **Future Scope (Post-Completion Ideas)**

* Boss enemy with HP bar (Phase 7 concept)

* Score multiplier / combo system

* Second enemy type (flying turret?)

* Daily challenge mode (seed-based run)

* Online leaderboard (simple REST API)

* New power-up: Rapid Fire (2x fire rate, 6 seconds)

* Dash mechanic re-enable (from controller features)

* Controller/gamepad support on Android

*NEON RUNNER — Development Plan & Tracker v1.0  |  Living Document — Update Every Session*