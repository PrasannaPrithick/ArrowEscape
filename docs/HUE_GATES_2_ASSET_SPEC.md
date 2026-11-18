# HUE GATES — Image & Asset Spec (how to create every image)

Companion to `HUE_GATES_1_MECHANIC_SPEC.md`. Use it with ChatGPT image generation (or any image tool), a free background remover, and a resize script. Prompts are ready to paste.

---

## 0. Workflow that keeps the art consistent

1. **Make one style sheet first.** Paste the *Global style block* (section 1) plus the *Style sheet prompt* (section 2). Keep the best result. Attach it as a reference to every later request: "Match the attached style sheet exactly."
2. **Generate in batches, not one by one.** Ask for a row or grid of items in one image (e.g. all 5 arrows). One image = one consistent look.
3. **Ask for a transparent PNG.** If the tool can't, generate on a flat solid background and remove it (Photopea is free; `rembg` is a free open-source script).
4. **Slice and resize** to the sizes in the asset tables. Export PNG, sRGB.
5. **Name files exactly** as listed (section 11), drop them in the folders, and view them in the game on a real phone before approving.
6. **Re-check the checklist** (section 12) once per batch.

Text inside AI images is unreliable. **Never ask for words or numbers in sprites.** Put all text in the engine with the font below.

**Cheaper and crisper option for arrows and gates:** they are simple geometry. Ask Cursor to draw them as SVG or procedural shapes (rounded arrow, fill color, soft gradient, optional glow). Use AI images for icons, shop art, backgrounds, logo, store graphics and gallery pictures.

## 1. Global style block (paste at the top of every prompt)

```
Mobile puzzle game art. Flat vector style with soft glow, rounded shapes, smooth gradients, no outlines, no text, no letters, no numbers, no watermark, no characters. Calm, clean, minimal. Deep navy base (#14172B). Only these accent colors: coral #FF6B6B, sun yellow #FFC94D, mint #3DDC97, sky blue #4DA3FF, violet #9B6BFF. Light comes from the top-left. Consistent corner radius across everything. No real brands or logos.
```

## 2. Style sheet prompt (do this first)

```
[Global style block]
Create a style sheet on a deep navy background showing: a 3x3 mini grid of dark rounded tiles with colored arrows in the five accent colors, a glowing colored border strip, two hearts (full and empty), three round buttons, a rounded pill button in mint, and a small panel with rounded corners. Everything in one consistent style.
```

## 3. Palette and type

| Name | Hex | Color-blind symbol |
|---|---|---|
| coral | `#FF6B6B` | ● circle |
| sun | `#FFC94D` | ▲ triangle |
| mint | `#3DDC97` | ■ square |
| sky | `#4DA3FF` | ◆ diamond |
| violet | `#9B6BFF` | ★ star |
| Background | `#14172B` | |
| Panel | `#1E2342` | |
| Text | `#F4F6FF` | |

Font: **Fredoka** (Google Fonts, open license) for all text.

## 4. Board and gameplay sprites

| File | Size | Notes |
|---|---|---|
| `cell_bg.png` | 128×128 | dark rounded tile `#1E2342`, subtle inner highlight |
| `arrow_coral.png` `arrow_sun.png` `arrow_mint.png` `arrow_sky.png` `arrow_violet.png` | 256×256 | bold rounded arrow **pointing up**, ~70% of the canvas; the game rotates it for other directions |
| `arrow_ready_glow.png` | 256×256 | soft white ring/halo, tinted in the engine |
| `gate_segment.png` | 128×48 | light grey rounded bar with a faint glow, tinted per color in the engine |
| `gate_flash.png` | 128×48 | brighter version used for pulse/denied flash |
| `board_frame.png` | 256×256, 9-slice (32 px border) | dark frame around the grid |
| `symbol_circle.png` `symbol_triangle.png` `symbol_square.png` `symbol_diamond.png` `symbol_star.png` | 64×64 | white, simple, for the color-blind setting |

**Prompt — arrows (one batch):**
```
[Global style block]
A horizontal row of 5 identical bold rounded arrows pointing straight UP, evenly spaced with wide gaps, on a transparent background. Colors left to right: coral #FF6B6B, sun yellow #FFC94D, mint #3DDC97, sky blue #4DA3FF, violet #9B6BFF. Same shape, size and lighting on all five. Soft gradient, subtle glow. Each arrow fills about 70% of a square.
```

**Prompt — cell tile and gate pieces:**
```
[Global style block]
On a transparent background, three separate items with wide gaps: (1) a dark rounded square tile #1E2342 with a faint inner highlight, (2) a short horizontal rounded light-grey bar with a faint glow, (3) the same bar but brighter with a stronger glow.
```

**Prompt — symbols:**
```
A row of 5 simple white flat symbols on a transparent background, evenly spaced: circle, triangle, square, diamond, five-point star. Bold, rounded corners, identical visual weight.
```

## 5. UI sprites

| File | Size |
|---|---|
| `heart_full.png`, `heart_empty.png` | 128×128 |
| `btn_undo.png`, `btn_hint.png`, `btn_restart.png` | 192×192 (round, icon inside, no number) |
| `badge_count.png` | 64×64 (small round chip; number drawn by the engine) |
| `btn_primary.png` (mint) / `btn_secondary.png` (navy) | 256×96, 9-slice |
| `panel.png` | 256×256, 9-slice, `#1E2342`, soft border |
| `toast.png` | 256×96, 9-slice |
| `progress_frame.png`, `progress_fill.png` | 512×48 |
| `star_full.png`, `star_empty.png` | 96×96 |

**Prompt — hearts and buttons:**
```
[Global style block]
On a transparent background with wide gaps: a glowing coral heart, an empty dark heart outline-less silhouette of the same shape, then three round buttons side by side: an orange-sun button with a curved undo arrow icon, a yellow button with a light bulb icon, a violet button with a circular restart icon. Icons are simple white or dark glyphs, no text.
```

**Prompt — pills, panel, progress:**
```
[Global style block]
On a transparent background with wide gaps: a wide mint rounded pill button (empty, no text), a wide dark navy rounded pill button (empty), a rounded dark panel with a thin soft border, a long thin empty progress bar track, and a matching glowing mint fill bar.
```

**Prompt — stars:**
```
[Global style block]
Two five-point rounded stars on a transparent background: one filled glowing sun yellow, one empty dark navy.
```

## 6. Icons (all 96×96, white glyph on transparent, rounded, same stroke weight)

`icon_settings`, `icon_play`, `icon_gallery`, `icon_shop`, `icon_lock`, `icon_check`, `icon_flame`, `icon_calendar`, `icon_close`, `icon_sound_on`, `icon_sound_off`, `icon_haptic`, `icon_back`.

```
A sheet of 13 simple icons on a transparent background in a 5-column grid, all the same size and stroke weight, rounded ends, white: settings gear, play triangle, picture frame, shopping bag, padlock, check mark, flame, calendar, close X, speaker on, speaker muted, vibration waves, back chevron. No text.
```

## 7. Backgrounds and effects

| File | Size | Notes |
|---|---|---|
| `bg_main.png` | 1080×1920 | navy gradient with very faint large soft shapes; must stay quiet behind the grid |
| `bg_result.png` | 1080×1920 | slightly brighter variant for the Level Complete screen |
| `fx_confetti_1…6.png` | 64×64 | small flat pieces in the five colors (rect, circle, triangle, ribbon) |
| `fx_spark.png` | 64×64 | soft white four-point sparkle |
| `fx_ring.png` | 256×256 | thin white ring, used for gate pulses |

```
[Global style block]
A vertical mobile background, 9:16: smooth deep navy gradient from #14172B at the top to #1B2044 at the bottom, with a few very faint, large, soft-edged rounded shapes in violet and sky blue at 6% opacity. Calm and quiet, no focal point, no text.
```
```
[Global style block]
On a transparent background, a grid of small flat confetti pieces: rectangles, circles, triangles and ribbons in the five accent colors, plus one soft white four-point sparkle and one thin white ring. Wide gaps between items.
```

## 8. Shop icons (256×256, transparent)

`shop_remove_ads.png`, `shop_hints.png`, `shop_undos.png`, `shop_pictures.png`, `shop_starter_bundle.png`

```
[Global style block]
Five square icons in a row on a transparent background, each in its own rounded colored badge: (1) a speaker/megaphone with a diagonal "no" slash in coral, no text, (2) a glowing light bulb in sun yellow, (3) a curved undo arrow in sky blue, (4) a small picture frame with a tiny mountain in mint, (5) a gift box with a bow in violet and sun yellow. Same size, same style.
```

## 9. Gallery pictures (10×10 pixel images)

Each picture is **10×10 pixels, max 8 colors**, split into 20 reveal pieces of 5 pixels (see mechanic spec section 9). Subjects for the first 12 pictures (change freely): fox, temple, lotus, peacock, elephant, tiger, owl, banyan tree, boat, sunrise over mountains, rocket, coffee cup.

**Two ways to make them**
- **A. Image tool, then convert.** Prompt below, then run the script.
- **B. Hand-designed grids.** A 10×10 grid with a short palette is small enough to write as data; ask Claude to write each picture as a 10×10 grid of palette letters and convert it directly. This is often cleaner than shrinking an AI image.

**Prompt (route A):**
```
Pixel art of a [SUBJECT], centered, front view, drawn on a 16x16 pixel grid with large chunky square pixels, flat colors, maximum 8 colors, no anti-aliasing, no outline blur, no gradients, plain solid dark background, no text.
```

**Convert script** (`tools/make_picture.py`, needs `pip install pillow`):
```python
from PIL import Image
import json, random, sys

def make(src, name, seed=1, colors=8):
    im = Image.open(src).convert("RGB")
    w, h = im.size; s = min(w, h)
    im = im.crop(((w - s) // 2, (h - s) // 2, (w + s) // 2, (h + s) // 2))
    small = im.resize((10, 10), Image.Resampling.BOX)
    small = small.quantize(colors=colors, method=Image.Quantize.MEDIANCUT).convert("RGB")
    pixels = [["#%02x%02x%02x" % small.getpixel((x, y)) for x in range(10)] for y in range(10)]
    order = list(range(100)); random.Random(seed).shuffle(order)
    pieces = [order[i * 5:(i + 1) * 5] for i in range(20)]   # pixel index = y*10 + x
    json.dump({"name": name, "size": 10, "pixels": pixels, "pieces": pieces},
              open(f"{name}.json", "w"))
    small.resize((320, 320), Image.Resampling.NEAREST).save(f"{name}_preview.png")

if __name__ == "__main__":
    make(sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 1)
```
Usage: `python make_picture.py fox_source.png fox 7` → `fox.json` + `fox_preview.png`. Check the preview; if the subject is not recognizable at 10×10, use route B.

## 10. Store assets

| Asset | Size | Notes |
|---|---|---|
| Google Play icon | 512×512 PNG | final version of the app icon |
| Google Play feature graphic | 1024×500 | no important content near the edges |
| Phone screenshots | 1080×1920 (9:16), 2-8 images | gameplay, level complete, gallery, shop, a hard board |
| Apple icon | 1024×1024 | no transparency, no pre-rounded corners |
| Apple screenshots | check App Store Connect for the currently required iPhone sizes | do this before iOS submission |

**App icon prompt:**
```
[Global style block]
App icon, square 1024x1024, full-bleed deep navy background with a subtle radial glow. One bold rounded mint-green glowing ring (a gate) in the center with a thick coral arrow passing through it diagonally. Simple, readable at 48 px, no text, no border.
```
**Feature graphic prompt:**
```
[Global style block]
Wide banner 1024x500, deep navy background, left half: a tilted 5x5 grid of dark tiles with colored arrows in the five accent colors, with glowing colored gates around the edges; right half left mostly empty for a title added later. No text.
```
**Screenshots:** take them from the real game on a phone. Add the caption text in Figma/Canva (free) over the top, not inside the AI image.

## 11. Folder layout and naming

```
Assets/Art/Board/     cell_bg, arrow_*, arrow_ready_glow, gate_*, board_frame, symbol_*
Assets/Art/UI/        heart_*, btn_*, badge_count, panel, toast, progress_*, star_*
Assets/Art/Icons/     icon_*
Assets/Art/FX/        fx_*
Assets/Art/Backgrounds/  bg_*
Assets/Art/Shop/      shop_*
Assets/Gallery/       <name>.json (+ <name>_preview.png)
Store/                icon_512, feature_1024x500, screenshots/
```
Lowercase, underscores, no spaces. Colors are always spelled `coral | sun | mint | sky | violet`.

## 12. Consistency checklist (per batch)

- Same corner radius, glow strength and light direction on every sprite.
- Only the five accent colors plus navy/white; no stray colors.
- No text, letters or numbers baked into any image.
- Transparent PNGs have clean edges (no white or navy halo).
- Arrows readable at 43 dp (the 8×8 cell size); symbols readable at 24 dp.
- Contrast: every arrow color is clearly visible on `#1E2342`.
- Test on a real phone in bright light.

## 13. Budget (estimates, in rupees)

| Item | Cost |
|---|---|
| Image generation (existing ChatGPT plan) | already covered |
| Background removal (Photopea / `rembg`), Fredoka font, Figma/Canva free tier | ₹0 |
| Sound effects: free CC0 packs (check each license) | ₹0 |
| Optional: freelancer polishing the app icon and feature graphic | about ₹1,500-2,500 |
| Optional: a second month of an image tool for retries | about ₹1,500-2,000 |

Check the current commercial-use terms of whichever image tool you use before publishing.
