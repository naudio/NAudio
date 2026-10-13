# NAudio logo

The logo SVGs live in the repository root. This folder holds the generator and
the parameters that define the wordmark, so the logo can be rebuilt or tweaked.

| File | What it is |
|---|---|
| `naudio-icon.svg` | The icon: curly braces around a peak meter. |
| `naudio-logo.svg` | Icon + wordmark, **weight 500, tracking −0.010em**. The current logo. |
| `naudio-logo-original.svg` | Icon + wordmark exactly as in the original artwork (weight 300, tracking 0). |
| `naudio-icon.png`, `naudio-logo.png` | The original raster artwork. The icon PNG is the NuGet package icon. |

Colours: grey `#3A3A3C`, red `#E51F25`. All SVGs have a transparent background.

## Rebuilding or tweaking the logo

`build_logo.py` needs only the Python standard library. Run it from the
repository root:

```
python branding/build_logo.py -o naudio-logo.svg                                   # current logo
python branding/build_logo.py --weight 300 --tracking 0 -o naudio-logo-original.svg  # original
```

| Option | Meaning |
|---|---|
| `--weight` | Stroke weight. 300 is the original artwork; each +100 thickens every stroke by 1.2 px (0.6 px per side). Default 500. |
| `--tracking` | Extra letter spacing in em (the em is 136 logo units, twice the x-height). Default −0.010. |
| `--ink`, `--accent` | Colours of the grey and red parts, e.g. `--ink "#F2F2F2"` for a dark-background variant. |
| `--icon-only` | Write just the icon, recoloured with `--ink` / `--accent`. |

The icon part of the logo is read from `naudio-icon.svg`, so edits to the icon
carry through to every generated logo.

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
