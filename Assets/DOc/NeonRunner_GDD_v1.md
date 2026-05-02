**NEON RUNNER**

Game Design Document  —  v1.0

*Hobby Marathon Project  |  2D Pixel Platformer \+ Auto Shooter*

Engine: Unity 6 LTS  |  Platform: Android \+ Windows  |  AI-Assisted Dev

## **0\. UNIFIED PRODUCT VISION (SOURCE OF TRUTH)**

This GDD and the Plan Tracker describe the **same game**: a fast **endless 2D neon platformer** where **shooting is automatic** and **skill is movement**—double jumps, reading chunk layouts, dodging, and survival pressure that ramps over time. **Pre-built chunk prefabs** are assembled so runs vary; **score** reflects time alive and kills; **power-ups** stay simple (one active at a time). Everything below refines that vision. When code or scope diverges, update **Section 0.1** and the Plan Tracker **Modification Log** in the same pass.

### **0.1 Technical baseline vs target architecture**

| Topic | **Baseline (current repo)** | **Target (full GDD)** |
| ----- | ----- | ----- |
| Render pipeline | **Universal Render Pipeline (URP)** | Same (docs may say “2D URP”) |
| Player control | Template **PlayerController** (Rigidbody2D, double jump, mobile \+ keyboard) | **Vladislav-EG** Flexible 2D controller (see §3.1)—migrate when Phase 1 tasks justify it |
| World flow | **Player moves** through the level; **chunks recycle on X** (`PlatformChunk`, `HorizontalChunkStreamer`)—**horizontal streaming only** (no vertical chunk shifting) | Full **ChunkManager**: weighted prefab pick, **StartPoint / EndPoint**, `chunksPassed`, difficulty tiers; optional **auto-scroll** feel via camera tuning |
| Camera | **Cinemachine** can follow the player (already in project) | Custom **CameraSystem** (§8) is an acceptable swap if we want pixel-perfect rules without Cinemachine |
| Combat / AI | To be built per §4–§5 | As specified |

**Design intent:** Baseline and target are **compatible**. Baseline proves chunk cycling and feel early; ChunkManager and richer camera rules **layer on** without throwing away `PlatformChunk` width / recycling ideas.

---

# **1\. GAME OVERVIEW**

## **1.1 Concept Statement**

Neon Runner is a fast-paced, endless 2D pixel-art platformer where the player character automatically fires forward while the player focuses entirely on movement, survival, and positioning. The **level is endless** by **streaming pre-built chunk prefabs** along the run direction (player moves through them, or—later—paired with stronger camera / scroll semantics). Chunks are chosen or recycled with **escalating difficulty** over run progress. A run ends when the player dies; score is based on survival time and enemy eliminations.

**Genre:** 2D Pixel Platformer \+ Auto Shooter (Endless / Roguelite-lite)

**Platform:** Android (primary), Windows (testing)

**Engine:** Unity 6 LTS (**Universal Render Pipeline / 2D URP** in this repo)

**Controller asset (target):** Flexible 2D Platformer Controller by Vladislav-EG (MIT License). **Current:** template `PlayerController` in this repo until migrated.

**Development Model:** AI-assisted (all art, code, audio, design generated/supported by AI)

## **1.2 Core Pillars**

| PILLAR | DESCRIPTION |
| ----- | ----- |
| **Constant Action** | The player is always shooting. No pause, no reload, no choice — the gun fires itself. The only input that matters is movement. |
| **Movement Skill** | Surviving is about platforming mastery: double jumps, coyote time, reading chunk layouts, and dodging projectiles. Movement IS the game. |
| **Impact Feedback** | Every hit must feel powerful: screen shake, time freeze, particle bursts, flashes. Tactile satisfaction drives replayability. |
| **Replayability** | Chunk-based procedural assembly ensures no two runs are identical. Pseudo-random weighted selection adds variance while ensuring quality. |

## **1.3 Design Goals vs Target Feelings**

| DESIGN GOAL | PLAYER FEELING | MEASUREMENT |
| ----- | ----- | ----- |
| Auto-shoot removes complexity | "I can focus on movement" | No shooting input required at all |
| Chunk difficulty ramps | "It's getting harder but fair" | Chunk weights shift at defined thresholds |
| Power-ups feel impactful | "That changed the run\!" | Each power-up has visible, measurable effect |
| Death feels the player's fault | "I should have jumped earlier" | No RNG instant-kills without telegraphing |

# **2\. CORE GAME LOOP**

## **2.1 Game Loop (Detailed)**

The following describes every state the game can be in, including transitions. This is the authoritative runtime flow.

| STATE | ENTRY CONDITION | WHAT HAPPENS | EXIT CONDITION |
| ----- | ----- | ----- | ----- |
| Start Screen | App launched | Show logo, start button, best score | Player taps Start |
| Spawn Player | Start pressed | Player instantiated at origin; camera locked; chunk 0 spawned | Spawn complete |
| Gameplay | Spawn complete | Player moves, shoots, enemies spawn, chunks stream, score increments | Player HP \<= 0 |
| Hit Reaction | Player takes damage | Screen shake, time freeze, invincibility frames, HP deducted | Freeze ends |
| Game Over | HP reaches 0 | Death animation, freeze, show final score, restart prompt | Player taps Restart |
| Restart | Restart tapped | Destroy all live objects, reset score, re-enter Spawn Player | Always \-\> Spawn Player |

## **2.2 Score System**

* Base score \= survival time in seconds x 10

* Kill bonus \= enemy health x 5 (awarded on enemy death)

* Power-up collection \= \+50 flat bonus

* Score displayed live on-screen; high score persisted via PlayerPrefs

*💡 Score multipliers (x1.5, x2) are OPTIONAL scope and should only be added in Phase 6 if time allows.*

# **3\. PLAYER SYSTEM**

## **3.1 Integration: Vladislav-EG Flexible 2D Controller (target)**

**Target:** The Flexible 2D Platformer Controller (MIT License)—horizontal movement with walk/run modes, multi-jump, coyote time, jump buffering, and polished ground feel—installed as a UPM package via Git URL.

**Current repo:** A **template PlayerController** lives at `Assets/Scripts/PlayerController.cs` (Rigidbody2D, double jump, ground check, PC \+ mobile). Use it as **Phase 1 stand-in** until Plan Tracker P1-02–P1-04 land. The tuning targets in §3.3 apply to **either** implementation.

*💡 Installation (when migrating): Unity Package Manager \> + \> Install from git URL \> `https://github.com/Vladislav-EG/Flexible2DCharacterControllerForUnity.git?path=/Assets/PlatformerController2D`*

## **3.2 Required Object Hierarchy**

Player (Root)  
  ├── Components: Rigidbody2D, PlayerController, CollisionChecker,  
  │              PhysicHandler2D, ColliderSpriteResizer, InputReader  
  ├── PlayerSprite (SpriteRenderer)  
  └── Colliders (Empty)  
       ├── Body (CapsuleCollider2D)   \<- main physics  
       └── Feet (BoxCollider2D)        \<- ground detection

## **3.3 Movement Parameters (PlayerControllerStats SO)**

| PARAMETER | VALUE | RANGE | NOTES |
| ----- | :---: | :---: | ----- |
| Default Movement Mode | Run | Walk / Run | Always run — no walk toggle needed for this game |
| Run Speed | 15 | 12–20 | Tune during Phase 1 playtesting |
| Run Acceleration | 150 | 100–200 | Snappy feel; avoid floaty start |
| Run Deceleration | 150 | 100–200 | Match acceleration for symmetric response |
| Max Jump Height | 3.5 | 3–5 | Allows clearing 2-tile gaps |
| Max Jumps | 2 | 1–3 | Double jump only; no triple jump |
| Coyote Time | 0.12s | 0.08–0.18s | Forgiveness window after walking off edge |
| Jump Buffer | 0.15s | 0.1–0.2s | Pre-input accepted before landing |
| Reset Jumps On Wall | OFF | On / Off | No wall-jump mechanic in scope |
| Dash | Disabled | — | Not in scope; can re-enable in future update |
| Crouch | Disabled | — | Not in scope |

## **3.4 Facing / Direction System**

* Facing is determined by last non-zero horizontal input

* Facing value: \-1 (Left) or \+1 (Right)

* Used by: ShootingSystem (bullet spawn direction), CameraSystem (look-ahead offset)

* Sprite flip is handled automatically by the controller's SpriteRenderer flip logic

*💡 The controller's InputReader handles direction. Our PlayerBrain reads controller.FacingDirection (or equivalent public property) each frame.*

## **3.5 Health System**

| PROPERTY | VALUE / BEHAVIOUR | IMPLEMENTATION NOTE |
| ----- | ----- | ----- |
| Max HP | 5 (adjustable via Inspector) | Exposed on PlayerBrain ScriptableObject or Inspector field |
| Invincibility Frames | 1.2 seconds after any damage | IEnumerator with WaitForSeconds; disable collider or set flag |
| Death Condition | HP \<= 0 | Fire OnPlayerDied event \-\> GameManager handles transition |
| HP UI | Heart icons (max 5 shown) | Sprite swapped: full / empty based on current HP |

## **3.6 Android Input Handling**

The controller uses Unity's Input System. For Android, we create a virtual on-screen layout:

* Left D-Pad area: maps to Move Left / Move Right (Vector2 input)

* Jump button (right side): maps to Jump action

* No shoot button: auto-fire, player cannot control it

* No dash/crouch buttons exposed in UI

*💡 Use Unity's On-Screen Stick and On-Screen Button components linked to the existing PlayerInputActions asset. Customise key bindings in Assets/PlatformerController2D/Runtime/Actions/PlayerInputActions.*

# **4\. COMBAT SYSTEM**

## **4.1 Shooting System (ShootingSystem.cs)**

Auto-fire is the central design decision — the player never presses a shoot button. This keeps mobile controls minimal and lets the game focus on platforming.

| PARAMETER | VALUE | DETAIL |
| ----- | :---: | ----- |
| Fire Rate | 0.25s (4 shots/sec) | Exposed in Inspector; adjust per power-up or difficulty |
| Bullet Speed | 18 units/sec | Must clear screen width before despawn. At 1280px / 32PPU \= 40 units wide, bullet lives \~2.2s |
| Bullet Damage | 1 (base) | Doubled by Damage Boost power-up |
| Bullet Direction | Horizontal only | Uses PlayerBrain.FacingDirection. No diagonal or vertical shooting. |
| Bullet Spawn Point | GunPoint transform | Child empty object on player sprite, offset to barrel tip |
| Bullet Lifetime | 3 seconds | Auto-return to pool after 3s regardless of hit |
| Destroy On Hit | First hit only | Bullet does not pierce. AoE Blast Bullet (power-up) is exception. |

## **4.2 Bullet.cs — Behaviour Detail**

public class Bullet : MonoBehaviour {  
    // Set by ShootingSystem on Init()  
    public float speed;  
    public float damage;  
    public bool isBlast;  // AoE variant  
    public int direction; // \+1 or \-1

    void Update() \=\> transform.Translate(Vector2.right \* direction \* speed \* Time.deltaTime);

    void OnTriggerEnter2D(Collider2D col) {  
        if (col.TryGetComponent\<IDamageable\>(out var target)) {  
            target.TakeDamage(damage);  
            if (isBlast) DoAoE(); // radius check around hit point  
            ReturnToPool();  
        }  
    }  
}

*💡 Use IDamageable interface on both Enemy and (optional) destructible props. This decouples bullet from specific target types.*

## **4.3 Hit Feedback Matrix**

| EVENT | TIME FREEZE | SCREEN SHAKE | VISUAL EFFECT |
| ----- | :---: | :---: | ----- |
| Bullet hits enemy | 0.05s @ 0.1 scale | None | Enemy flash white \+ particle burst |
| Enemy dies | 0.08s @ 0.0 scale | Micro shake (0.05) | Explosion particles, neon glow pop |
| Player hit | 0.2s @ 0.0 scale | Medium (0.3, 0.15s) | Player flash red, vignette pulse |
| Player death | 0.5s @ 0.0 scale | Strong (0.6, 0.4s) | Explosion, fade to game over overlay |
| Power-up collected | 0.04s @ 0.2 scale | None | Ring expand \+ icon pop |

*💡 Time freeze \= Time.timeScale set to value for duration, then restored to 1\. Implement via coroutine in GameFeel.cs singleton.*

# **5\. ENEMY SYSTEM**

## **5.1 Enemy Design Rules**

* Every enemy must be readable within 1 second: silhouette, movement pattern, and threat must all be obvious

* Every enemy has a distinct silhouette — no two enemies should be confusable at a glance

* Enemies must telegraph their attacks with a visible wind-up (at minimum 0.3s)

* Enemy HP is low — this game is about survival movement, not sustained combat

## **5.2 Enemy Type Definitions**

### **▶ CRAB — Ground Patrol**

| ATTRIBUTE | VALUE / DESCRIPTION |
| ----- | ----- |
| Sprite | Squat, wide body. Neon orange carapace. Clearly a ground unit. |
| HP | 2 hits |
| Movement | Patrol between two waypoints on the current platform. Flips direction on edge detection or waypoint reach. |
| Attack | Every 2.5s: pauses briefly (0.3s wind-up), then drops a proximity mine directly below itself. |
| Mine Behaviour | Mine sits on ground for 3s, then detonates on player proximity (1.2 unit radius). Does 1 damage \+ small push force. |
| Drop Chance | 25% power-up on death |
| AI State Machine | PATROL \-\> PLACE\_MINE \-\> PATROL (loop). No chase state. |

### **▶ FISH GROUP — Formation Flyer**

| ATTRIBUTE | VALUE / DESCRIPTION |
| ----- | ----- |
| Sprite | Slim, elongated. Neon teal/blue. Airborne unit — clearly a flyer. |
| HP | 1 hit (fragile — compensated by numbers) |
| Spawn Count | 3–5 per spawn event |
| Formation | V-formation or line. Leader fish at front; followers offset by 0.8 units. |
| Movement | Horizontal sine-wave oscillation. Group moves toward player X position slowly. |
| Attack | Leader fires arc projectile every 3s (parabolic trajectory). Deals 1 damage. Followers do not fire. |
| Drop Chance | 15% power-up on leader death; 0% on follower death |
| AI State Machine | FORM \-\> APPROACH \-\> FIRE \-\> FORM (loop) |

## **5.3 Enemy Spawning Rules**

* Enemies are placed in chunk prefabs at design time — no runtime random placement

* Spawn points are EnemySpawn trigger volumes; enemy type set per-spawn-point

* Max concurrent enemies: 12 (pool size). If pool exhausted, spawn is skipped (not queued).

* Difficulty scaling increases spawn density in later chunks (see Section 7\)

## **5.4 Enemy AI Architecture**

// Base class \- all enemies inherit this  
public abstract class EnemyBase : MonoBehaviour, IDamageable {  
    public float maxHP;  
    protected float currentHP;  
    public float powerUpDropChance \= 0.2f;

    public virtual void TakeDamage(float amount) {  
        currentHP \-= amount;  
        StartCoroutine(FlashWhite(0.1f));  
        if (currentHP \<= 0\) Die();  
    }

    protected virtual void Die() {  
        GameFeel.Instance.Freeze(0.08f, 0f);  
        ParticleManager.Instance.PlayAt("EnemyExplode", transform.position);  
        if (Random.value \< powerUpDropChance) PowerUpManager.Instance.SpawnAt(transform.position);  
        ScoreManager.Instance.AddKill(maxHP);  
        EnemyPool.Instance.Return(this);  
    }  
}

# **6\. POWER-UP SYSTEM**

## **6.1 System Rules**

* Only ONE power-up can be active at a time

* Picking up a new power-up while one is active: new one replaces old immediately

* No stacking, no queue

* Active power-up displayed in UI (icon \+ timer bar)

## **6.2 Power-Up Catalogue**

| NAME | ICON COLOR | DURATION | EFFECT DETAIL |
| ----- | :---: | :---: | ----- |
| Shield | Cyan | Until 2 hits absorbed | Blocks next 2 damage instances. Visual: neon ring around player. No HP is deducted when shield absorbs. |
| Health | Green | Instant | Restore 30–50% of max HP (randomized). Cannot overheal past max. UI hearts update immediately. |
| Damage Boost | Orange | 5 seconds | Bullet damage x2. Bullet sprite changes to larger neon orange variant. UI timer bar visible. |
| Blast Bullets | Pink/Purple | 8 seconds | Each bullet deals AoE explosion (2 unit radius) on first hit. Hits all enemies in radius. Visual: star burst. |

## **6.3 Spawn Logic**

* Primary source: enemy death (20% base chance)

* Secondary source: chest props in chunks (guaranteed if opened)

* Weighted random selection: Health 35% | Shield 30% | Damage Boost 20% | Blast 15%

* At difficulty level 5+: Health weight drops to 20%, Blast rises to 25%

*💡 Implement PowerUpManager as singleton with weighted random selector. Spawn a pickup GameObject with trigger collider; player walks into it to collect.*

## **6.4 PowerUpSystem.cs Architecture**

public class PowerUpSystem : MonoBehaviour {  
    private IPowerUp activePowerUp;

    public void Activate(PowerUpType type) {  
        activePowerUp?.Deactivate(); // replace existing  
        activePowerUp \= PowerUpFactory.Create(type);  
        activePowerUp.Activate(player);  
        UI.ShowPowerUpIcon(type, activePowerUp.Duration);  
    }

    public void Deactivate() {  
        activePowerUp?.Deactivate();  
        activePowerUp \= null;  
        UI.HidePowerUpIcon();  
    }  
}

# **7\. CHUNK SYSTEM**

## **7.0 Implementation stages (aligned with repo)**

| Stage | Behaviour | Code direction |
| :---: | ----- | ----- |
| **A — Horizontal streamer (v1)** | A fixed pool of chunk **instances**; as the player moves on **X**, segments far **behind** (or **ahead** if they walk back) are **repositioned** end-to-end. **Y/Z** are **not** streamed—**horizontal platforming focus only** for this stage. | `PlatformChunk` + `HorizontalChunkStreamer` (`Assets/Scripts/`) |
| **B — Full ChunkManager** | **Weighted** chunk selection, **no immediate repeat**, **StartPoint / EndPoint** workflow, **`chunksPassed`** driving §7.4 tiers. Can build on or replace Stage A internals. | `ChunkManager` (see §10.2); Plan Tracker Phase 3 |

Until Stage B ships, treat **`chunksPassed`** / distance as something the streamer (or a thin counter) can expose for difficulty.

## **7.1 What Is a Chunk?**

A chunk is a pre-built Unity prefab for one **horizontal** segment of the run (commonly **~25–60** world units depending on tilemap). `PlatformChunk` on the prefab root defines or **measures** that span so instances **tile without gaps** when recycled.

## **7.2 Chunk Prefab Structure**

Chunk (Root GameObject)  
 ├── StartPoint        (Transform) \<- where previous chunk's EndPoint connects  
 ├── EndPoint          (Transform) \<- next chunk spawns here  
 ├── Platforms         (parent)  
 │    ├── Platform\_A  (Tilemap or SpriteRenderer)  
 │    └── Platform\_B  
 ├── EnemySpawns       (parent)  
 │    ├── EnemySpawn\_01 (trigger: EnemyCrab at X,Y)  
 │    └── EnemySpawn\_02 (trigger: FishGroup at X,Y)  
 ├── PowerUpSpawns     (parent, optional)  
 │    └── ChestSpawn\_01  
 └── Props             (parent)  
      ├── BG\_Pillar  
      └── Hazard\_Spikes (optional)

*💡 **Stage A:** the root must include **PlatformChunk** (width measured or set manually). **StartPoint / EndPoint** become mandatory when you implement **Stage B** snapping.*

## **7.3 Chunk manager behaviour (target + Stage A)**

* Keep a small pool of active chunks (**3+**): behind \| current \| ahead.

* **Trigger workflow (Stage B):** On **EndPoint** crossed, spawn the next chunk ahead; despawn chunk(s) far behind the player.

* **Streamer workflow (Stage A):** Same pool idea; recycle the **leftmost or rightmost** instance using **`PlatformChunk` width**—no **EndPoint** required if layouts and widths line up.

* **Selection:** Weighted pseudo-random with **no immediate repeat** once ChunkManager owns prefab choice.

* **Progress:** Track **`chunks_passed`** / distance for §7.4 difficulty scaling.

## **7.4 Difficulty Tiers**

| TIER | CHUNKS | ENEMY COUNT | SPAWN RATE MOD | SPEED MOD |
| ----- | :---: | ----- | ----- | :---: |
| 1 – Intro | 0–10 | 1–2 per chunk | 1.0x (baseline) | 1.0x |
| 2 – Rising | 11–25 | 2–3 per chunk | 1.2x | 1.1x |
| 3 – Hard | 26–50 | 3–5 per chunk | 1.5x | 1.25x |
| 4 – Brutal | 51+ | 5–8 per chunk | 2.0x | 1.4x |

*💡 Speed mod applies to **enemy movement speed**. **Chunk scroll speed** applies only if we implement **auto-scrolling** or moving geometry; in **player-driven** Stage A, treat **speed mod** as enemy-only until scroll is added.*

## **7.5 Chunk Pool Composition (Phase 1 Target)**

| CHUNK NAME | DIFFICULTY TIER | LAYOUT NOTES | ENEMY SLOTS |
| ----- | :---: | ----- | ----- |
| Flat\_Open\_01 | 1 | Wide flat ground, no gaps | 1 Crab patrol |
| Gap\_Jump\_01 | 1–2 | 2 gaps, 3 platforms | 1 Crab, 1 Fish |
| Multi\_Level\_01 | 2 | 3 height levels, ramps | 2 Crab, 1 Fish group |
| Air\_Gauntlet\_01 | 3 | Moving platforms, fish-heavy | 3 Fish groups |
| Spike\_Run\_01 | 3–4 | Floor spikes, tight platforms | 2 Crab \+ mines |
| Chaos\_01 | 4 | Dense enemies, narrow paths | Max enemies |

# **8\. CAMERA SYSTEM**

## **8.1 Camera Rules**

| RULE | IMPLEMENTATION |
| ----- | ----- |
| Only moves RIGHT | Camera X is clamped monotonically (**or** Cinemachine + confiner/extension enforcing the same rule). |
| Left boundary enforced | Player cannot exit left edge of camera viewport. Left boundary collider moves with camera. |
| Smooth follow | SmoothDamp on X, Lerp on Y toward player \+ facing offset—**or** equivalent Cinemachine damping until **CameraSystem.cs** replaces it. |
| Look-ahead offset | When facing right: \+2 unit offset. When facing left: \+0.5 unit (minimal, since camera doesn't go left) |
| Vertical adjustment | Lerp toward player Y with dampening. Clamp to level bounds (min/max Y defined per chunk). |
| Pixel snap | PixelPerfectCamera component handles snapping. Camera position rounded to pixel grid each frame. |

## **8.2 Camera Settings**

* Component: PixelPerfectCamera (Unity 2D Pixel Perfect package)

* Reference Resolution: 1280 x 720

* PPU: 32

* Crop Frame: Stretchfill (maintain aspect, add black bars)

* Grid Snapping: Enabled

* **Cinemachine:** Present in the repo and **may** be used for follow/confiner **until** custom **CameraSystem.cs** matches §8.1 exactly.

# **9\. ART SYSTEM (AI PIXEL PIPELINE)**

## **9.1 Style Definition**

* Pixel art with neon glow overlays (not traditional pixel art — neon-cyberpunk theme)

* Dark backgrounds (\#0D0D1A or similar) with bright neon foreground elements

* Consistent color palette per enemy type (see Section 5\)

* All sprites: PPU \= 32, Point filter mode, no compression

## **9.2 AI Art Generation Workflow**

| ASSET TYPE | AI TOOL | PROMPT TEMPLATE |
| ----- | ----- | ----- |
| Character sprites | Midjourney / Bing Image Creator | "16x16 pixel art \[subject\], neon cyberpunk palette, dark bg, transparent, spritesheet" |
| Enemy sprites | Midjourney / Bing Image Creator | Same template, specify enemy type and neon color |
| Tile sets | Midjourney / Lospec palette tools | "16x16 tileset, platform game, neon purple/teal, seamless, dark background" |
| Backgrounds | Midjourney | "pixel art parallax background, cyberpunk city, dark navy, neon lights, 1280x720" |
| Particles / FX | Unity VFX Graph / hand-tweaked | Generate base sprite sheet, animate in Unity |
| UI elements | Canva AI / Midjourney | "pixel art UI panel, health bar, neon cyan border, dark bg, game HUD" |
| Audio (SFX) | ElevenLabs SFX / BFXR | Describe: "8-bit laser shot, punchy, neon shooter game" |
| Music | Suno AI / Udio | "synthwave chiptune loop, 140bpm, intense, neon runner game, no vocals, 2 min" |

*⚠️ All AI-generated art must be reviewed for quality. Pixel art at 16x16 often needs manual cleanup in Aseprite. Budget 30–60 min per sprite for cleanup.*

## **9.3 Sprite Sizes**

| ASSET | PIXEL SIZE | WORLD SIZE @ 32PPU |
| ----- | :---: | :---: |
| Player | 16x24 px | 0.5 x 0.75 units |
| Crab enemy | 24x16 px | 0.75 x 0.5 units |
| Fish enemy | 20x12 px | 0.625 x 0.375 units |
| Bullet (base) | 8x4 px | 0.25 x 0.125 units |
| Tile unit | 32x32 px | 1 x 1 unit |

# **10\. TECHNICAL ARCHITECTURE**

## **10.1 System Map (Dependency Graph)**

The following describes all major systems and their relationships. Arrows indicate dependency (A → B means A depends on B or calls into B).

┌─────────────────────────────────────────────────────────────────────┐  
│                         GAME MANAGER                               │  
│  (State machine: StartScreen / Gameplay / GameOver / Restart)      │  
└───────┬────────────────────────────────────────────────────────────┘  
        │  
   ┌────▼──────┐    ┌───────────────┐    ┌──────────────────┐  
   │ PlayerBrain│───▶│ ShootingSystem │───▶│   BulletPool     │  
   │ (Central) │    └───────────────┘    └──────┬───────────┘  
   └─┬──┬──┬───┘                                │  
     │  │  │                                     ▼  
     │  │  └──▶ PowerUpSystem             ┌──────────┐  
     │  └─────▶ HealthSystem              │  Enemy   │  
     └────────▶ FlexibleController        │ (via IDamageable)│  
                (Vladislav-EG pkg)         └──┬───────┘  
                                              │  
   ┌──────────────┐                          │  
   │ ChunkManager │──▶ ChunkPool ──▶ Chunk ──┘  
   │ (Difficulty) │         └──▶ EnemyPool  
   └──────────────┘

   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐  
   │ CameraSystem │───▶│ PlayerBrain  │    │  GameFeel    │  
   │ (PixelPerf.) │    │ (position)   │    │ (Freeze/Shk) │  
   └──────────────┘    └──────────────┘    └──────────────┘

   ┌──────────────┐    ┌──────────────┐  
   │ ScoreManager │    │  UIManager   │  
   │ (PlayerPrefs)│    │ (HUD/Screens)│  
   └──────────────┘    └──────────────┘

## **10.2 Full Class Diagram**

CLASS: GameManager  
  \-state: GameState (enum: Start, Playing, GameOver)  
  \+StartGame()  
  \+GameOver()  
  \+RestartGame()  
  \+OnPlayerDied() \[subscribes to PlayerBrain.OnDied event\]

CLASS: PlayerBrain : MonoBehaviour  
  \-controller: PlayerController \[ref to Vladislav pkg\]  
  \-shooting: ShootingSystem  
  \-health: HealthSystem  
  \-powerUps: PowerUpSystem  
  \+FacingDirection: int { get }  
  \+GunPoint: Transform  
  event OnDied  
  \+ApplyDamage(float amount)

CLASS: HealthSystem  
  \-currentHP: float  
  \-maxHP: float  
  \-isInvincible: bool  
  \+TakeDamage(float) \-\> bool (returns true if died)  
  \+Heal(float percent)  
  \+StartInvincibility(float duration)

CLASS: ShootingSystem : MonoBehaviour  
  \-fireRate: float  
  \-timer: float  
  \-bulletPool: BulletPool  
  \-player: PlayerBrain  
  \+void Update() \[auto-fires on timer\]  
  \+void SetDamageMultiplier(float mult)  
  \+void SetBlastMode(bool on)

CLASS: Bullet : MonoBehaviour, IPoolable  
  \+speed: float  
  \+damage: float  
  \+direction: int  
  \+isBlast: bool  
  \+Init(dir, dmg, blast)  
  \+OnHit(IDamageable target)  
  \+ReturnToPool()

INTERFACE: IDamageable  
  \+TakeDamage(float amount)

CLASS: EnemyBase : MonoBehaviour, IDamageable (abstract)  
  \#currentHP, maxHP: float  
  \#powerUpDropChance: float  
  \+TakeDamage(float) \[virtual\]  
  \#Die() \[virtual\]  
  \#FlashWhite(float duration)

CLASS: CrabEnemy : EnemyBase  
  \-patrolSpeed: float  
  \-mineDropInterval: float  
  \-State: enum {Patrol, PlaceMine}  
  \+Update() \-\> patrol \+ mine placement logic

CLASS: FishEnemy : EnemyBase  
  \-formationOffset: Vector2  
  \-isLeader: bool  
  \-fireInterval: float  
  \+Update() \-\> formation move \+ arc fire

CLASS: ChunkManager : MonoBehaviour  
  \-activeChunks: Queue\<Chunk\>  
  \-chunksPassed: int  
  \-difficultyTier: int  
  \-chunkPool: List\<ChunkData\>  
  \+SpawnNextChunk()  
  \+DestroyBehindChunk()  
  \+GetDifficultyTier(): int

*Stage A note: recycling logic may live in `HorizontalChunkStreamer` until this class exists; keep one authoritative “chunks passed” counter for §7.4.*

CLASS: PlatformChunk : MonoBehaviour *(Stage A — in repo)*  
  \+Width { get }  
  \+GetLeftX(), GetRightX(), GetCenterX()  
  \+SetHorizontalCenter(float worldCenterX)

CLASS: HorizontalChunkStreamer : MonoBehaviour *(Stage A — in repo)*  
  Recycles instantiated prefabs along X; locks chunk Y/Z for horizontal-only milestone.

CLASS: PowerUpSystem : MonoBehaviour  
  \-activePowerUp: IPowerUp  
  \+Activate(PowerUpType)  
  \+Deactivate()

INTERFACE: IPowerUp  
  \+Activate(PlayerBrain)  
  \+Deactivate()  
  \+Duration: float { get }

CLASS: GameFeel : MonoBehaviour \[Singleton\]  
  \+Freeze(duration, timeScale)  
  \+Shake(intensity, duration)

CLASS: ObjectPool\<T\> \[Generic\]  
  \-pool: Queue\<T\>  
  \+Get(): T  
  \+Return(T)  
  \+Prewarm(int count)

CLASS: CameraSystem : MonoBehaviour  
  \-target: PlayerBrain  
  \-minX: float \[monotonically increasing\]  
  \+FollowTarget()  
  \+ApplyFacingOffset()  
  \+ClampToBounds()

CLASS: ScoreManager : MonoBehaviour  
  \-currentScore: float  
  \-highScore: float \[PlayerPrefs\]  
  \+AddTime(float delta)  
  \+AddKill(float enemyHP)  
  \+GetFinalScore(): float

CLASS: UIManager : MonoBehaviour  
  \+ShowStartScreen()  
  \+ShowHUD()  
  \+ShowGameOver(score, highScore)  
  \+UpdateHP(int current)  
  \+ShowPowerUpIcon(type, duration)

## **10.3 Event System**

Systems communicate via C\# events to avoid tight coupling. The following events are defined:

| EVENT | FIRED BY | SUBSCRIBERS |
| ----- | ----- | ----- |
| PlayerBrain.OnDied | HealthSystem | GameManager, UIManager, ScoreManager |
| PlayerBrain.OnDamaged(float) | HealthSystem | UIManager (HP update), GameFeel (shake) |
| ScoreManager.OnScoreChanged(float) | ScoreManager | UIManager (live score display) |
| ChunkManager.OnChunkPassed(int) | ChunkManager | ChunkManager (difficulty update) |
| PowerUpSystem.OnActivated(type) | PowerUpSystem | UIManager (icon show) |
| PowerUpSystem.OnDeactivated | PowerUpSystem | UIManager (icon hide) |

## **10.4 Object Pooling**

All frequently spawned objects use pooling to avoid GC pressure. Unity 6 includes a built-in Object Pool — use UnityEngine.Pool.ObjectPool\<T\>.

| POOL | SIZE | NOTES |
| ----- | :---: | ----- |
| BulletPool | 20 bullets | At 4/sec, bullets live 3s max \= 12 concurrent max. 20 gives headroom. |
| EnemyPool (Crab) | 6 | Max 3 active chunks x 2 crabs \= 6 |
| EnemyPool (Fish) | 15 | Fish groups spawn 5 at a time x 3 chunks |
| ChunkPool | 3 | 3 active chunks maintained |
| ParticlePool | 30 | Reuse explosion/hit particle systems |

# **11\. UI SYSTEM**

## **11.1 Screen Inventory**

| SCREEN | ELEMENTS | PRIORITY |
| ----- | ----- | ----- |
| Start Screen | Game logo, "TAP TO START" button, best score display | MUST HAVE |
| HUD (Gameplay) | HP hearts, live score, power-up icon \+ timer bar | MUST HAVE |
| Game Over Screen | Final score, high score, "RESTART" button | MUST HAVE |
| Settings Screen | Volume slider, control layout toggle | OPTIONAL |
| Mode Select | Endless only (for now) | OPTIONAL |

## **11.2 HUD Layout (1280x720 Canvas)**

┌─────────────────────────────────────────────────────────┐  
│ ♥ ♥ ♥ ♥ ♥           SCORE: 00000        \[POWER-UP\] ▓▓▓ │  
│                                                         │  
│                   \< GAME WORLD \>                       │  
│                                                         │  
│  \[LEFT\]  \[LEFT\]                    \[JUMP\]              │  
│  \[ ◀ \]   \[ ▶ \]                   \[  ▲  \]              │  
└─────────────────────────────────────────────────────────┘  
   ← Android touch controls (bottom 20% of screen) →

* Canvas: Screen Space \- Overlay

* Scale Mode: Scale With Screen Size, reference 1280x720

* Touch controls use On-Screen Button and On-Screen Stick from Unity Input System

# **12\. AUDIO SYSTEM**

## **12.1 Audio Assets Required**

| ASSET | TYPE | AI SOURCE | PRIORITY |
| ----- | :---: | ----- | ----- |
| Shoot SFX | Short SFX | ElevenLabs SFX / BFXR | MUST HAVE |
| Enemy Hit SFX | Short SFX | ElevenLabs SFX | MUST HAVE |
| Explosion SFX | Short SFX | BFXR / ElevenLabs | MUST HAVE |
| Player Hurt SFX | Short SFX | ElevenLabs SFX | MUST HAVE |
| Power-up Collect SFX | Short SFX | BFXR | MUST HAVE |
| Jump SFX | Short SFX | BFXR | MUST HAVE |
| Main Theme Loop | Music (2–3 min loop) | Suno AI / Udio | MUST HAVE |
| Game Over Jingle | Short music | Suno AI | OPTIONAL |

## **12.2 Audio Manager (Simple)**

* AudioManager singleton with AudioSource references: BGM, SFX\_A, SFX\_B, SFX\_C

* SFX are fire-and-forget. Concurrent SFX use a round-robin AudioSource pool (3 sources)

* BGM loops seamlessly. Volume exposed in Settings

* No FMOD / Wwise — Unity native AudioSource only

# **13\. GAME FEEL IMPLEMENTATION**

## **13.1 GameFeel.cs — Complete Implementation Guide**

public class GameFeel : MonoBehaviour {  
    public static GameFeel Instance;  
    private Camera cam;  
    private Coroutine freezeCoroutine;  
    private Coroutine shakeCoroutine;

    // ─── TIME FREEZE ───────────────────────────────────────────  
    public void Freeze(float duration, float timeScale \= 0f) {  
        if (freezeCoroutine \!= null) StopCoroutine(freezeCoroutine);  
        freezeCoroutine \= StartCoroutine(DoFreeze(duration, timeScale));  
    }  
    IEnumerator DoFreeze(float dur, float ts) {  
        Time.timeScale \= ts;  
        yield return new WaitForSecondsRealtime(dur);  
        Time.timeScale \= 1f;  
    }

    // ─── SCREEN SHAKE ───────────────────────────────────────────  
    public void Shake(float intensity, float duration) {  
        if (shakeCoroutine \!= null) StopCoroutine(shakeCoroutine);  
        shakeCoroutine \= StartCoroutine(DoShake(intensity, duration));  
    }  
    IEnumerator DoShake(float intensity, float duration) {  
        Vector3 originalPos \= cam.transform.localPosition;  
        float elapsed \= 0;  
        while (elapsed \< duration) {  
            float x \= Random.Range(-1f, 1f) \* intensity;  
            float y \= Random.Range(-1f, 1f) \* intensity;  
            cam.transform.localPosition \= new Vector3(x, y, originalPos.z);  
            elapsed \+= Time.unscaledDeltaTime;  
            yield return null;  
        }  
        cam.transform.localPosition \= originalPos;  
    }  
}

# **14\. SCOPE & SUCCESS CRITERIA**

## **14.1 MUST HAVE (Minimum Viable Game)**

| FEATURE | SUCCESS CONDITION |
| ----- | ----- |
| Player movement (left/right/jump) | Smooth on both keyboard and Android touch with no jank |
| Auto-shooting | Bullets fire 4/sec automatically in facing direction |
| Crab and Fish enemies | Both behave correctly, deal damage, and die properly |
| Chunk system (6+ chunks) | **Horizontal** chunk pool recycles **without gaps or seams** for 5+ minutes (Stage A streamer **or** Stage B ChunkManager) |
| Difficulty scaling | Noticeable increase between Tier 1 and Tier 4 |
| Power-up system (4 types) | All 4 work correctly, one-at-a-time rule enforced |
| Hit feedback | Freeze \+ shake present on all hit events |
| Score \+ Game Over | Score displayed, high score persisted, restart works |
| Camera system | Camera follows smoothly, never goes left |
| Android build | Installs and plays on Android 10+ without crashes |

## **14.2 OPTIONAL (Nice-to-Have)**

* Boss enemy (Phase 6+)

* Combo multiplier system

* Settings screen with audio volume

* Animated start screen

* Parallax scrolling background

* Leaderboard (local, not online)

* Second music track

## **14.3 OUT OF SCOPE (This Version)**

* Online multiplayer

* In-app purchases

* Cloud save

* Story mode

* Controller/gamepad support on Android (keyboard \+ touch only)

— END OF DOCUMENT —

*NEON RUNNER GDD v1.0  |  Hobby Marathon Project  |  All AI-Assisted*