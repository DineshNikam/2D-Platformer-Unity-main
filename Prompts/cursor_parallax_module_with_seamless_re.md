# Parallax module with seamless repetition
_Exported on 5/4/2026 at 02:59:46 GMT+5:30 from Cursor (3.2.11)_

---

**User**

need to create parallax effect with this two images in current scene
create parallax module with seamles repeattion and grannular controll
you can use MCP if required

---

**Cursor**

Exploring the project structure and scene setup so we can add a parallax module with seamless tiling and per-layer control.



Implementing `ParallaxSeamlessLayer` and wiring the Level scene via MCP. Checking how scripts are added to the project:



Creating the parallax script and attempting to wire the scene via MCP. Sprite size ~27.52 world units at PPU 100 — we'll expose scale so we can fill the camera.



Patching the scene YAML to assign sprites and tuned parallax values. MCP couldn't serialize `Sprite` references via path patches.



Here’s what was added and how it behaves.

## `ParallaxSeamlessLayer` (`Assets/Scripts/ParallaxSeamlessLayer.cs`)

**Behavior (runs in `LateUpdate` at order 200, after the camera):**

1. **Camera lock** – Each frame the root’s **X** matches the follow camera’s **X** (defaults to `Camera.main` if `cameraTransform` is empty). **Y** is `_anchorY + camera.y × verticalScrollFactor`, and **Z** is `worldZ`. That keeps the backdrop in view while you run to the right.

2. **Seamless repeat** – Two child objects (`ParallaxTileA` / `ParallaxTileB`) get the same sprite. Their **local X** is driven by  
   `offset = positiveMod(camera.x × horizontalScrollFactor, tileWidth)`  
   with tiles at `-offset` and `-offset + tileWidth`. That wraps every `tileWidth` world units so one copy slides in as the other exits, with no gaps as long as the **left and right edges of the art match** when butted together. If the PNG edges don’t line up, you’ll still see a periodic seam until the image is edited (or you use tileable art).

3. **Granular controls (per layer in the Inspector):**
   - **horizontalScrollFactor** – Strength of the sliding wrap vs camera X (farther layers usually **smaller** than near).
   - **verticalScrollFactor** – Subtle vertical parallax from camera Y.
   - **spriteWorldScale** – Local scale on both tiles (covers orthographic size ~10 and wide sprites ~27.5u wide).
   - **sortingOrder** / **sortingLayerName** – Draw order vs gameplay sprites.
   - **anchorYOffset** – Fine-tune vertical placement.
   - **worldZ** – Fixed Z for the root.

## Scene wiring (`Level.unity`)

Under root **`ParallaxBackground`**:

| Object            | Sprite        | Scroll | Sort   | Scale   |
|------------------|---------------|--------|--------|---------|
| `Parallax_BG_Far`  | `Assets/png/BG/BG Layer1.png` | `0.04` | `-120` | `(1.8, 1.8)` |
| `Parallax_BG_Near` | `Assets/png/BG/BG Layer2.png` | `0.11` | `-85`  | `(1.8, 1.8)` |

Tweak **horizontal scroll** and **scale** in Play Mode to taste; leave **cameraTransform** empty to use the main camera.

**Note:** MCP couldn’t assign `Sprite` references through the component API, so those references were set by editing the scene YAML. After opening the project, use **Assets → Refresh** if Unity doesn’t pick up the scene immediately.

**Art expectation:** Seamless **code** wrapping is correct; a **visual** seam only disappears if the image tiles cleanly at its sides (your descriptions noted the PNGs might not be perfectly tileable). If you see a hard line, adjust the artwork or try a small horizontal scale/offset in the sprite import.

