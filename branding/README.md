# NAudio logo

The logo SVGs live in the repository root. This folder holds the generator and
the parameters that define the wordmark, so the logo can be rebuilt or tweaked.

| File | What it is |
|---|---|
| `naudio-icon.svg` | The icon: curly braces around a peak meter. |
| `naudio-logo.svg` | The **2026 logo**: icon + wordmark at weight 500, tracking −0.010em. |
| `naudio-logo-dark.svg`, `naudio-icon-dark.svg` | The 2026 logo and the icon for dark backgrounds (grey `#EDEDED`). |
| `naudio-logo-2015.svg` | The **2015 logo**: an exact vector recreation of the original artwork (weight 300, tracking 0). |
| `naudio-icon.png` | The NuGet package icon (and docs-site favicon): the icon rendered at 256 × 256 on white, so it stays visible in dark themes. NuGet doesn't accept SVG icons. |
| `naudio-logo.png` | The original 2015 raster artwork, kept as the reference the wordmark was fitted to. |

Colours: grey `#3A3A3C` (`#EDEDED` on dark backgrounds), red `#E51F25`.
All SVGs have a transparent background.

The 2015 logo was commissioned shortly after NAudio moved to GitHub in
February 2015; `naudio-logo.png` was added to this repository in 2017.

## Rebuilding or tweaking the logo

`build_logo.py` needs only the Python standard library, plus `cairosvg`
(`pip install cairosvg`) for PNG output. Run it from the repository root:

```
python branding/build_logo.py -o naudio-logo.svg                                        # 2026 logo
python branding/build_logo.py --ink "#EDEDED" -o naudio-logo-dark.svg                   # 2026, dark
python branding/build_logo.py --icon-only --ink "#EDEDED" -o naudio-icon-dark.svg       # icon, dark
python branding/build_logo.py --preset 2015 -o naudio-logo-2015.svg                     # 2015 logo
python branding/build_logo.py --icon-only --size 256 --background white -o naudio-icon.png  # NuGet icon
```

| Option | Meaning |
|---|---|
| `--preset` | `2026` (default: weight 500, tracking −0.010) or `2015` (weight 300, tracking 0). |
| `--weight` | Overrides the preset's stroke weight. 300 is the original artwork; each +100 thickens every stroke by 1.2 px (0.6 px per side). |
| `--tracking` | Overrides the preset's extra letter spacing, in em (the em is 136 logo units, twice the x-height). |
| `--ink`, `--accent` | Colours of the grey and red parts, e.g. `--ink "#EDEDED"` for a dark-background variant. |
| `--icon-only` | Write just the icon, recoloured with `--ink` / `--accent`. |
| `--size`, `--background` | PNG output only: the icon's side (or the logo's width) in px, and a background colour (default transparent). |

The icon part of the logo is read from `naudio-icon.svg`, so edits to the icon
carry through to every generated logo.

## Where the logo is used

- **README.md** shows `naudio-logo.svg`, or `naudio-logo-dark.svg` when GitHub is
  in dark mode (a `<picture>` element with a `prefers-color-scheme` source).
- **The docs site** (DocFX) uses `naudio-icon.svg` in the navbar;
  `docfx/templates/naudio/public/main.js` swaps in `naudio-icon-dark.svg` when the
  site's theme is dark. The favicon is `naudio-icon.png`.
- **NuGet packages** use `naudio-icon.png` as the package icon.

## How the wordmark is built

The letters are geometric constructions rather than a font. Each letter is a
single path made of:

- **rectangles** for stems (some stems taper slightly toward the baseline below
  a bowl, and the left stem of the **n** has a small chamfer where the arch joins);
- **quarter rings**: an outer and an inner cubic Bézier quarter. `Rx`/`Ry` are
  the outer radii, `tv` the side thickness, `th` the top/bottom thickness (round
  strokes are thinner at the top and bottom than at the sides), and `ko`/`ki`
  the outer/inner curve tension (0.5523 is a true ellipse; higher is squarer).
  Where a curve joins a stem, `dyi` moves the inner curve's centre so the counter
  meets the stem tangentially at its own height;
- **straight bands** joining quarters where a curve has a flat section.

Joins are built so the pieces meet exactly: a bowl's side shares the stem's
edges, and quarters meet at a shared point. All sub-paths wind clockwise, so the
default nonzero fill renders their union.

`wordmark.json` holds the parameters for each letter in the pixel coordinates of
the original `naudio-logo.png` (619 × 143). They were fitted to that artwork by
measuring edge positions from its anti-aliasing and optimising the parameters
against its pixels; at weight 300 the generated logo differs from the PNG by
less than one colour level per pixel on average.

The icon (`naudio-icon.svg`) is hand-editable: each brace is one outline of
straight lines and quarter arcs with a flat vertical tip, the right brace is the
same construction mirrored, and the meter bars are rectangles.
