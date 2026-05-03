# Neon Runner title bloom and setup script
_Exported on 5/4/2026 at 02:58:48 GMT+5:30 from Cursor (3.2.11)_

---

**User**

this is screen shot of Menu Scene 
Add bloom effect on NEON RUNNER  TMP text background
MAke and editor script to varify the packages and setup the implementation
after implemetation tell me success test cases

---

**Cursor**

Exploring the project's render pipeline and menu setup so we can add TMP bloom support and an editor script to verify packages and automate setup.



Implementing the editor setup: the project uses URP with post-processing disabled on the Menu camera, and the Canvas is Screen Space Overlay — UI won't receive URP bloom until we switch to Screen Space - Camera. Creating an editor script that verifies URP (via manifest + active pipeline), applies that Canvas change, enables camera post-processing, adds a global Volume with Bloom, and boosts the title TMP with HDR outline/glow so bloom has something to pick up.



Cleaning up the script: removing dead code, fixing URP/Volume APIs, and correcting TMP material handling.



Fixing mistaken TMP/Volume API usage: simplifying material refresh and volume profile assignment.



Improving title TMP discovery (prefer `Title`, fallback to text containing "NEON"/"RUNNER") and fixing selection + null-safe bloom lookup.



Here is what was added and how to use it.

## What was implemented

New editor utility: `Assets/Editor/NeonRunnerMenuBloomSetup.cs`.

**Menu:** **Neon Runner → Menu → Setup TMP Title Bloom (URP)** — runs the full setup.  
**Neon Runner → Menu → Verify Bloom Prerequisites (Packages + URP)** — checks manifest + active URP only.

### Why Screen Space — Camera

Bloom in URP runs in **post-processing on the camera**. A **Screen Space — Overlay** canvas is drawn **after** that, so the title never enters the HDR buffer and **cannot bloom**. The script switches your **Menu** canvas to **Screen Space — Camera**, assigns **Main Camera**, and fixes a root **(0,0,0) scale** when needed so layout stays valid.

### What the setup does

1. **Packages** — Reads `Packages/manifest.json` for `com.unity.render-pipelines.universal` (and notes URP core / UI).
2. **URP** — Ensures **Graphics → Scriptable RP** is a **UniversalRenderPipelineAsset** and logs `supportsHDR`.
3. **Scene** — Opens `Assets/Scenes/Menu.unity` (after save prompt if needed).
4. **Canvas** — **Overlay → Screen Space — Camera**, assigns main camera, clamps **plane distance**.
5. **Camera** — Enables **Render Post Processing** on `UniversalAdditionalCameraData`.
6. **Volume** — Creates or reuses **`NeonRunner_Menu_GlobalVolume`** (Default layer) with profile **`Assets/Settings/NeonRunner_MenuBloomProfile.asset`** and a **Bloom** override (threshold ~0.75, intensity ~0.65, scatter ~0.85, white tint).
7. **TMP title** — Targets **`Title`** **or** any `TMP_Text` whose string contains **NEON** or **RUNNER** (so your “NEON RUNNER” line works). Applies a **four-corner HDR-style gradient**, **HDR outline**, and enables **GLOW_ON** on the **font material** (`_GlowColor` / `_GlowOuter`) so bloom has bright regions to pick up.

Tune look in **`NeonRunner_MenuBloomProfile`** (Bloom thresholds/intensity) and on the **Title** TMP (outline, gradient, glow material).

---

## Success test cases (manual)

1. **Verify menu**  
   Run **Verify Bloom Prerequisites**. You should see URP active, manifest URP line present, and TMP available; no blocking error dialog.

2. **Run setup**  
   Run **Setup TMP Title Bloom**. Dialog should finish with a short report (scene opened, canvas mode changed, camera PP on, volume/profile, TMP path). Console log should show the same summary.

3. **Scene objects**  
   In **Menu**: **Canvas** = **Screen Space — Camera**, **Event Camera** = **Main Camera**. **Main Camera** has **Post Processing** enabled. A root object **`NeonRunner_Menu_GlobalVolume`** exists (global Volume, profile assigned).

4. **Assets**  
   `Assets/Settings/NeonRunner_MenuBloomProfile.asset` exists and, in the Inspector under **Bloom**, overrides are checked and non-default.

5. **Game view**  
   Enter Play Mode on **Menu**: the title should show a **noticeable halo** around bright letters (cyan/magenta-ish from outline + gradient + bloom). Turning **Bloom** intensity to **0** on the volume profile should **remove** that halo — confirms bloom is driving the effect, not only TMP styling.

6. **Regression UI**  
   Buttons (**Play / Settings / Quit**) still receive clicks and graphics look correct after the canvas mode change.

If anything fails to compile (e.g. `Volume.sharedProfile` renamed in your Unity minor version), say which Unity exact version you use and we can align the serialized field names.

---

**User**

Assets\Editor\NeonRunnerMenuBloomSetup.cs(454,13): error CS1061: 'TMP_Text' does not contain a definition for 'colorMode' and no accessible extension method 'colorMode' accepting a first argument of type 'TMP_Text' could be found (are you missing a using directive or an assembly reference?)

Assets\Editor\NeonRunnerMenuBloomSetup.cs(457,13): error CS1061: 'TMP_Text' does not contain a definition for 'fontColorGradient' and no accessible extension method 'fontColorGradient' accepting a first argument of type 'TMP_Text' could be found (are you missing a using directive or an assembly reference?)



---

**Cursor**

Checking the script and fixing TMP API usage: `colorMode` and `fontColorGradient` may exist only on `TextMeshProUGUI` in this project's TMP version.



Using SerializedObject for gradient fields — in this TMP/Unity version those members aren't exposed on the `TMP_Text` type the editor sees (package split). Serializing `m_enableVertexGradient`, `m_colorMode`, and `m_fontColorGradient` matches TMP's inspector and compiles everywhere.



Here’s what was wrong and what we changed.

### Cause
With your Unity / TMP setup, **`colorMode` and `fontColorGradient` are not public on `TMP_Text`** (often due to API differences between the UI/TMP assemblies). The compiler only sees the surface that type exposes.

### Fix
Those values are still on the component in YAML; the Inspector edits them as **`m_enableVertexGradient`**, **`m_colorMode`**, and **`m_fontColorGradient`**. The script now sets them through **`SerializedObject`** in **`ApplyVertexGradientSerialized`**, so nothing depends on those public properties.

Behavior stays the same: vertex gradient on, four-corner mode, magenta-ish top / blue-ish bottom. Outline and glow are unchanged (`outlineWidth` / `outlineColor` still use `TMP_Text`, which already compiled for you).

Recompile in Unity; **`CS1061`** for those two symbols should be gone. If gradient doesn’t appear, open the title TMP in the Inspector and confirm **Color Gradient** is enabled—if not, tell me your exact Unity version and we can match any renamed serialized fields.

---

**User**

when I play scen e I dont see it 
using MCP please check scene 
also tell me how I can manually varify if its working or not

---

**Cursor**

Checking the Menu scene via Unity MCP: locating tool schemas, then inspecting camera, canvas, volume, and the title TMP.



MCP confirms the Menu scene wiring: post-processing enabled, Screen Space Camera, volume + profile assigned, Bloom active. Bloom may still look invisible because the threshold (0.75) filters UI pixels if they don't read as bright enough. Lowering bloom threshold and raising intensity so the effect is unmistakable.



## MCP check (Unity Editor · `Assets/Scenes/Menu.unity`)

What’s in the scene right now matches a valid URP bloom setup:

| Check | MCP result |
|--------|-------------|
| **Main Camera · `UniversalAdditionalCameraData`** | `renderPostProcessing` = **true** |
| **Main Camera · `Camera`** | `allowHDR` = **true** |
| **Canvas** | `renderMode` = **Screen Space — Camera**, `worldCamera` = **Main Camera**, `planeDistance` = **100** |
| **Global volume** | `NeonRunner_Menu_GlobalVolume` · `Volume` **`isGlobal` = true**, **`weight` = 1**, **`priority` = 10** |
| **Volume asset** | `sharedProfile` → **`Assets/Settings/NeonRunner_MenuBloomProfile.asset`** |
| **Profile** | **Bloom** override **active**, threshold / intensity / scatter overridden |
| **Layer** | Volume object **`layer` = 0** (Default) — matches camera **`volumeLayerMask` value 1** (Default only) ✓ |
| **Title TMP** (`Canvas/Title`) | Text **NEON RUNNER**, **outlineWidth ≈ 0.28**, bright outline |

So the problem isn’t missing camera PP or overlay canvas anymore — it’s mostly **how strong / selective bloom is**.

TMP + default UI shaders often produce **moderate luminance**. With **Bloom threshold = 0.75**, a lot of that never counts as “bright,” so bloom can look **off** even though everything is wired.

I’ve **lowered threshold and raised intensity** in `Assets/Settings/NeonRunner_MenuBloomProfile.asset` (**threshold 0.35**, **intensity 1**, **scatter 0.92**) and synced the same defaults in `NeonRunnerMenuBloomSetup.cs`. **Asset Database refresh was run via MCP** so Unity should reload the profile.

After you **switch focus to Unity** (or reopen the profile), hit **Play** again on Menu — the halo should be much easier to see.

---

## How to **manually** verify bloom (step‑by‑step)

1. **A/B on the Volume asset**  
   - Select **`Assets/Settings/NeonRunner_MenuBloomProfile.asset`**.  
   - Toggle the **Bloom** override **Off** vs **On** (or drag **Intensity** 0 ↔ 1) while **Game view** shows the menu.  
   - If nothing changes → PP isn’t running for that view/camera (rare given MCP). If something changes → bloom is live; tune threshold/intensity.

2. **`UniversalAdditionalCameraData` on Play**  
   - Select **Main Camera**, enter **Play**.  
   - Confirm **Rendering → Post Processing** stays **checked** during play.

3. **Frame Debugger** (best “is bloom running”)  
   - **Window → Analysis → Frame Debugger**.  
   - Enable, **pause on current frame**.  
   - Look for draws / passes whose names mention **Bloom** / **Uber Post** / **Post Processing** **after** the main scene (and canvas) renders.  
   - If those passes never appear → renderer/camera/player settings blocking PP.

4. **`Graphics` tier / RP**  
   - **Edit → Project Settings → Graphics**: **Scriptable Render Pipeline** = your **URP** asset.

5. **Confirm you’re actually running Menu**  
   - **Play** with **Menu** open in the Editor, or confirm **first scene** in **Build Profiles / Build Settings** is **Menu**.

6. **`Game` view**, not **`Scene`**  
   - Post effects are authored for **Game** view rendering; judge bloom there.

If you’ve done the A/B on the Bloom override and **still** see zero change, say so and we can narrow it to Frame Debugger vs a platform/player setting — but MCP’s live scene state plus the weaker threshold fix is the most likely explanation for “everything set up but I don’t see it.”

---

**User**

make only the title bloom and not the other texts 

and animate bloom effect  

---

**Cursor**

Implementing a runtime controller that keeps button labels below the bloom threshold, duplicates the Volume profile so we don't edit assets at runtime, and animates bloom parameters while optionally pulsing the title's glow/outline.



Adding a runtime controller that dims menu button TMPs (keeping them below the bloom threshold), duplicates the Volume profile so we can animate bloom safely at runtime, pulses Bloom intensity/scatter on the cloned profile, and pulsing title glow. Wiring it from the existing editor setup.



Here’s what was added and how it behaves.

### Why “only title” with URP  
URP **Bloom runs on the whole frame**, not per UI element. The reliable approach is: **drive button labels below the bloom threshold** (muted color, no glow, outline off) while the **Title** keeps HDR-style gradient/outline/glow. That way only bright regions from the headline pick up bloom.

### New runtime script: `Assets/Scripts/NeonRunnerMenuTitleBloomController.cs`

- Runs early (`DefaultExecutionOrder(-50)`): **dims every `TMP_Text` under your `Canvas` except `Title`** (and anything parented under `Title`).  
  - Tweaks on the **`Canvas`** (or Inspector): **`Menu Button Text Color`**, **`Menu Button Outline Width`**.
- Finds **`NeonRunner_Menu_GlobalVolume`** by default (override name if yours differs).
- **Clones** the Volume’s profile in `Awake` so play mode **never writes** your `NeonRunner_MenuBloomProfile.asset`. On destroy / scene unload it **restores** the original asset reference on the Volume.
- **Animates bloom** via the clone: **`Bloom Intensity`** and **`Scatter`** pulse with a sine (speed + min/max in Inspector).
- **Animates title-only glow** (`_GlowOuter` on the title’s **font material**), with its own phase offset so it doesn’t march in lockstep with global bloom.

### Editor wiring  
Re-run **`Neon Runner → Menu → Setup TMP Title Bloom (URP)`** and it will add / rewire **`NeonRunnerMenuTitleBloomController`** on the **Canvas**, with **`targetCanvas`**, **`titleText`**, and **`bloomVolume`** filled in.

If you already ran setup once, either run it again **or** add the component yourself on **Canvas** and assign `Title` TMP + bloom `Volume`.

### Inspector tuning (pulse strength)

| Field | Purpose |
|--------|---------|
| `Bloom Pulse Speed` | Pulses/sec (scaled by sine) |
| `Bloom Intensity Min / Max` | Global bloom pulse range |
| `Bloom Scatter Min / Max` | How wide the halo breathes |
| `Glow Outer Min / Max` | Title material halo pulse |
| `Glow Phase Offset Radians` | Desync bloom vs glow |
| `Menu Button Text Color` | Lower = safer “no halo” on buttons |

### Manual verification

1. Enter **Play**: title **halo pulses**; **PLAY / SETTINGS / QUIT** stay **steady** with no halo.  
2. Drag **`Bloom Intensity Min`** → **0**: if **everything** goes flat dull, bloom is wired; bump min/max until only the title visibly blooms.  
3. Disable **`Animate Title Glow`** and confirm only Volume-driven bloom pulses (if TMP glow isn’t on your shader, bloom animation still runs).

Bloom is **post-processing**: judge in the **Game** view, not the Scene view overlay.

