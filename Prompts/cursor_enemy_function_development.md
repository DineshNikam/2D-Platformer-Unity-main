# Enemy function development
_Exported on 5/4/2026 at 02:59:38 GMT+5:30 from Cursor (3.2.11)_

---

**User**

@@Assets/DOc/NeonRunner_GDD_v1.md @Assets/DOc/NeonRunner_PlanTracker_v1.md 
Lets start creating enemy related function 
in this chat we will create common health and related architecture first/karpathy-guidelines
then we will move on to creating crab enemy 


---

**Cursor**

Reviewing the design docs and existing code patterns so we can add a minimal, consistent health architecture for enemies.



Searching the codebase for combat-related code and script organization.



Implementing GDD-aligned `IDamageable` and a reusable `Health` component. Refactoring `HealthManager` into a UI/display layer synced to player `Health`. Wiring `GameManager` to `Health.Died` and completing health pickups.



Removing the incorrect pragma block and the unused `HalfHeartSprite`/`using System`; verifying Unity API for finding the player.



Here’s what was added and how it fits the GDD and Phase 2 tasks (P2‑01 / P2‑10 direction).

### Assumptions
- **`Health`** lives on anything that receives damage (`IDamageable`): player and future enemies share the same component.
- **Player HUD** stays on **`HealthManager`**, which only **mirrors** `Health` instead of owning a separate HP int (fixes the old “need an extra hit at 0” flow and matches **max 5 discrete HP / full–empty hearts** from §3.5).

### What was implemented

1. **`IDamageable`** (`Assets/Scripts/Combat/IDamageable.cs`)  
   Single entry point: **`TakeDamage(float amount)`**, as in GDD §4.2 bullets.

2. **`Health`** (`Assets/Scripts/Combat/Health.cs`)  
   - **`MaxHp`** / **`CurrentHp`**, **`IsAlive`**, **`IsInvincible`**  
   - **`TakeDamage`**: skips while invulnerable; HP clamped at 0; fires **`Damaged`** (damage, remaining), then **`Died`** once at 0  
   - **Invulnerability** uses **`Time.realtimeSinceStartup`** so future hit-stop (`timeScale` → 0) doesn’t stall i-frames  
   - **`Heal`**, **`RestoreFullHp`**, **`SetMaxHp`** for tuning/pickups/power-ups  

3. **`HealthManager`**  
   Finds the player’s **`Health`**, subscribes to **`Damaged`** for burst VFX + heart refresh; hearts drive off **`Health.CurrentHp`** with **`Health.MaxHp`** capping how many slots count as “in use”.

4. **`GameManager`**  
   Subscribes to **`Health.Died`** and calls existing **`Death()`** (no duplicate call from `HealthManager`).

5. **`pickup`**  
   **`pickupType.health`** now restores **+1 HP** via **`Health.Heal(1f)`** (type already existed; behavior was missing).

### How it works (short)
Callers (bullets, mines, hazards) only need **`IDamageable`**: `GetComponent<IDamageable>()` / `TryGetComponent` and **`TakeDamage`**. The **`Health`** component owns numbers, i-frames, and death; UI and game flow listen to **events** so we don’t hard‑wire bullets to `GameManager`.

### What you need in Unity (MCP didn’t return editor data here)
1. **Add `Health` to the player root** (same object as `PlayerController` is fine).  
2. Inspector: **Max Hp = 5**, **Invincibility Duration After Hit = 1.2** (GDD §3.5).  
3. **Heart row**: use **5** heart images if you want one icon per HP; `HealthManager` maps “full vs empty” from current HP and caps by **`MaxHp`**.

### Next (crab, in a follow-up)
- **`EnemyBase`** can require **`Health`**, or implement **`IDamageable`** and forward to `Health`.  
- Bullets use **`TryGetComponent<IDamageable>`** and **`TakeDamage(damage)`** as in the GDD snippet.

If you want, we can do the crab in this thread next: **`EnemyBase` + `CrabEnemy`** wired to this **`Health`**.

