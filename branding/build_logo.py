#!/usr/bin/env python3
"""Generate the NAudio logo SVGs from the parametric wordmark in wordmark.json.

The "naudio" wordmark is a geometric reconstruction of the original logo
artwork. Each letter is built from rectangles, quarter-ring curves (an outer
and an inner cubic Bezier quarter) and straight bands, and is emitted as one
path whose sub-paths all wind clockwise, so the default nonzero fill renders
their union. The icon is read from naudio-icon.svg.

Coordinates are those of the original naudio-logo.png (619 x 143 px).

Examples (run from the repository root):

    python branding/build_logo.py -o naudio-logo.svg                       # 2026 logo
    python branding/build_logo.py --preset 2015 -o naudio-logo-2015.svg    # original 2015 logo
    python branding/build_logo.py --ink "#EDEDED" -o naudio-logo-dark.svg
    python branding/build_logo.py --icon-only --ink "#EDEDED" -o naudio-icon-dark.svg

Only the Python standard library is needed.
"""
import argparse
import json
import math
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
INK, ACCENT = "#3A3A3C", "#E51F25"
DARK_INK = "#EDEDED"   # grey used in place of INK on dark backgrounds

# Named weight / tracking settings. 2015 is the original artwork; 2026 is the
# heavier, slightly tighter update.
PRESETS = {"2015": (300, 0.0), "2026": (500, -0.010)}

# Weight 300 is the original artwork. Each 100 above it thickens every stroke
# by 1.2 px (0.6 px on each side); this matches the weight slider used when the
# 500 / -0.010em look was chosen.
ORIGINAL_WEIGHT = 300
GROW_PER_100 = 0.6
EM = 136.0            # the wordmark's em in logo units (twice its x-height of 68)
K0 = 0.5523           # Bezier tension of a true ellipse quarter

D = 0.0               # outward growth applied by every primitive (set by build())

# x-coordinate parameters of each letter, shifted when tracking moves the letter
XKEYS = {"n": ["cxL", "lx", "chx"], "a": ["sx", "tcx", "xcut", "bxl", "bcx3"],
         "u": ["xl", "sx"], "d": ["L", "sx"], "i": ["sx", "dx"], "o": ["cx"]}


def n(v):
    s = f"{v:.2f}".rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s


def P(x, y):
    return f"{n(x)} {n(y)}"


# ---------------------------------------------------------------- primitives

def rect(x0, y0, x1, y1, grow_x=True, grow_y=True):
    """Clockwise rectangle. Bands that only join two curves grow along one axis."""
    gx, gy = (D if grow_x else 0), (D if grow_y else 0)
    x0, y0, x1, y1 = x0 - gx, y0 - gy, x1 + gx, y1 + gy
    if x1 <= x0 or y1 <= y0:
        return ""
    return f"M{P(x0, y0)}H{n(x1)}V{n(y1)}H{n(x0)}Z"


def poly(*pts):
    """Clockwise convex polygon, grown outward by D with mitred corners."""
    if D:
        cx = sum(p[0] for p in pts) / len(pts)
        cy = sum(p[1] for p in pts) / len(pts)
        lines = []
        for a, b in zip(pts, pts[1:] + pts[:1]):
            dx, dy = b[0] - a[0], b[1] - a[1]
            ln = math.hypot(dx, dy)
            nx, ny = dy / ln, -dx / ln
            if nx * ((a[0] + b[0]) / 2 - cx) + ny * ((a[1] + b[1]) / 2 - cy) < 0:
                nx, ny = -nx, -ny
            lines.append(((a[0] + nx * D, a[1] + ny * D), (dx, dy)))
        out = []
        for (p1, d1), (p2, d2) in zip(lines[-1:] + lines[:-1], lines):
            den = d1[0] * d2[1] - d1[1] * d2[0]
            t = ((p2[0] - p1[0]) * d2[1] - (p2[1] - p1[1]) * d2[0]) / den
            out.append((p1[0] + d1[0] * t, p1[1] + d1[1] * t))
        pts = tuple(out)
    return "M" + "L".join(P(x, y) for x, y in pts) + "Z"


def stem(x0, x1, top, bot, tap_y=None, tap_dx=0.0):
    """Vertical stem; its left edge may taper in by tap_dx from tap_y to the baseline."""
    if tap_y is None or tap_dx == 0:
        return rect(x0, top, x1, bot)
    return poly((x0, top), (x1, top), (x1, bot), (x0 + tap_dx, bot), (x0, tap_y))


def circle(cx, cy, r):
    r += D
    return (f"M{P(cx - r, cy)}A{n(r)} {n(r)} 0 0 1 {P(cx + r, cy)}"
            f"A{n(r)} {n(r)} 0 0 1 {P(cx - r, cy)}Z")


def _quarter(cx, cy, Rx, Ry, ta, k):
    """Cubic Bezier for the quarter from angle ta to ta + 90 (y down, clockwise)."""
    def pt(a):
        a = math.radians(a)
        return cx + Rx * round(math.cos(a)), cy + Ry * round(math.sin(a))
    S, E = pt(ta), pt(ta + 90)
    C = (S[0], E[1]) if abs(S[1] - cy) < 1e-9 else (E[0], S[1])
    return (S, (S[0] + k * (C[0] - S[0]), S[1] + k * (C[1] - S[1])),
            (E[0] + k * (C[0] - E[0]), E[1] + k * (C[1] - E[1])), E)


def qring(cx, cy, Rx, Ry, tv, th, q, ko=K0, ki=K0, dyi=0.0):
    """Quarter of a ring (q = tl, tr, br or bl).

    Outer quarter: radii Rx, Ry about (cx, cy). Inner quarter: side thickness
    tv, crown/bottom thickness th, and its centre may sit dyi lower where the
    side joins a stem (so the counter meets the stem at its own height).
    ko / ki are the outer / inner Bezier tensions (0.5523 = true ellipse).
    """
    Rx, Ry, tv, th = Rx + D, Ry + D, tv + 2 * D, th + 2 * D
    top = q[0] == "t"
    rx, ry = Rx - tv, Ry - th + (dyi if top else -dyi)
    if rx <= 0.05 or ry <= 0.05:
        return ""
    ta = {"tl": 180, "tr": 270, "br": 0, "bl": 90}[q]
    S, a1, a2, E = _quarter(cx, cy, Rx, Ry, ta, ko)
    s, b1, b2, e = _quarter(cx, cy + dyi, rx, ry, ta, ki)
    return f"M{P(*S)}C{P(*a1)} {P(*a2)} {P(*E)}L{P(*e)}C{P(*b2)} {P(*b1)} {P(*s)}Z"


def _split_from(S, c1, c2, E, t):
    lerp = lambda p, q: (p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t)
    p01, p12, p23 = lerp(S, c1), lerp(c1, c2), lerp(c2, E)
    p012, p123 = lerp(p01, p12), lerp(p12, p23)
    return lerp(p012, p123), p123, p23, E


def _t_at_x(S, c1, c2, E, x):
    f = lambda t: (1 - t) ** 3 * S[0] + 3 * (1 - t) ** 2 * t * c1[0] + 3 * (1 - t) * t * t * c2[0] + t ** 3 * E[0] - x
    lo, hi = 0.0, 1.0
    if f(lo) * f(hi) > 0:
        return None
    for _ in range(50):
        mid = (lo + hi) / 2
        lo, hi = (lo, mid) if f(lo) * f(mid) <= 0 else (mid, hi)
    return (lo + hi) / 2


def qring_clipped_tl(cx, cy, Rx, Ry, tv, th, xcut, ko=K0, ki=K0):
    """Top-left quarter ring ending in a vertical cut at x = xcut (the a's terminal)."""
    Rx, Ry, tv, th, xcut = Rx + D, Ry + D, tv + 2 * D, th + 2 * D, xcut - D
    rx, ry = Rx - tv, Ry - th
    o, i = _quarter(cx, cy, Rx, Ry, 180, ko), _quarter(cx, cy, rx, ry, 180, ki)
    to, ti = _t_at_x(*o, xcut), _t_at_x(*i, xcut)
    if to is None or ti is None:
        return ""
    S, a1, a2, E = _split_from(*o, to)
    s, b1, b2, e = _split_from(*i, ti)
    return f"M{P(*S)}C{P(*a1)} {P(*a2)} {P(*E)}L{P(*e)}C{P(*b2)} {P(*b1)} {P(*s)}Z"


# ------------------------------------------------------------------- letters

def letter_n(p):
    cxL = p["cxL"]
    cxR = cxL + abs(p.get("gap", 0.0))                  # the two crown quarters meet
    xr = cxR + p["RxR"]                                  # outer edge of the right stem
    tvL = p["lx"] + p["lw"] - (cxL - p["RxL"])           # counter meets the left stem tangentially
    d = qring(cxL, p["cy"], p["RxL"], p["Ry"], tvL, p["th"], "tl", p["ko_tl"], p["ki_tl"], p["dyi_tl"])
    d += qring(cxR, p["cy"], p["RxR"], p["Ry"], p["tv"], p["th"], "tr", p["ko_tr"], p["ki_tr"])
    if cxR > cxL:
        d += rect(cxL - 0.01, p["cy"] - p["Ry"], cxR + 0.01, p["cy"] - p["Ry"] + p["th"], grow_x=False)
    d += rect(xr - p["tv"], p["cy"] - 0.01, xr, p["bot"])
    x0, x1 = p["lx"], p["lx"] + p["lw"]                  # left stem with chamfered top-right corner
    d += poly((x0, p["ltop"]), (p["chx"], p["ltop"]), (x1, p["chy"]), (x1, p["bot"]), (x0, p["bot"]))
    return d


def letter_a(p):
    sx0, sx1 = p["sx"], p["sx"] + p["sw"]
    # top arc: its right quarter IS the stem; its left quarter ends in a vertical cut
    d = qring(p["tcx"], p["tcy"], sx1 - p["tcx"], p["tRy"], p["sw"], p["thT"], "tr",
              p["ko_ttr"], p["ki_ttr"], abs(p["dyi_ttr"]))
    d += qring_clipped_tl(p["tcx"], p["tcy"], p["tRxL"], p["tRy"], p["tvT"], p["thT"], p["xcut"],
                          p["ko_ttl"], p["ki_ttl"])
    d += stem(sx0, sx1, p["tcy"], p["sb"], p["tap_y"], p["tap_dx"])
    # bowl: flat top joining the stem, rounded left side, bottom-right quarter rising into the stem
    xl, bcy = p["bxl"], p["bcy"]
    c1, c2, c3 = xl + p["bRx1"], xl + p["bRx2"], p["bcx3"]
    d += qring(c1, bcy, p["bRx1"], p["bRy1"], p["btv"], p["bth1"], "tl", p["ko_btl"], p["ki_btl"])
    d += qring(c2, bcy, p["bRx2"], p["bRy2"], p["btv"], p["bth2"], "bl", p["ko_bbl"], p["ki_bbl"])
    d += qring(c3, bcy + p["bRy2"] - p["bRy3"], sx1 - c3, p["bRy3"], p["sw"], p["bth2"], "br",
               p["ko_bbr"], p["ki_bbr"], p["dyi_bbr"])
    if c3 > c2:
        d += rect(c2 - 0.01, bcy + p["bRy2"] - p["bth2"], c3 + 0.01, bcy + p["bRy2"], grow_x=False)
    d += rect(c1 - 0.01, bcy - p["bRy1"], sx0 + 0.5, bcy - p["bRy1"] + p["bth1"], grow_x=False)
    return d


def letter_u(p):
    xl, B = p["xl"], p["B"]
    cx = xl + p["RxL"]                                   # both bottom quarters meet here
    cyL, cyR = B - p["RyL"], B - p["RyR"]
    d = rect(xl, p["topL"], xl + p["tv"], cyL + 0.01)
    d += qring(cx, cyL, p["RxL"], p["RyL"], p["tv"], p["th"], "bl", p["ko_bl"], p["ki_bl"])
    d += qring(cx, cyR, p["sx"] + p["sw"] - cx, p["RyR"], p["sw"], p["th"], "br",   # right side IS the stem
               p["ko_br"], p["ki_br"], p["dyi_br"])
    d += stem(p["sx"], p["sx"] + p["sw"], p["topR"], p["bot"], p["tap_y"], p["tap_dx"])
    return d


def bowl(p):
    """d's bowl: four independent corner quarters; the right side IS the stem."""
    L, T, B = p["L"], p["T"], p["B"]
    cx = L + p["RxL"]                                    # top and bottom quarters meet here
    RxR, tvR = p["sx"] + p["sw"] - cx, p["sw"]
    cy = {"tl": T + p["RyTL"], "bl": B - p["RyBL"], "tr": T + p["RyTR"], "br": B - p["RyBR"]}
    d = ""
    for q, Rx, tv in (("tl", p["RxL"], p["tv"]), ("bl", p["RxL"], p["tv"]), ("tr", RxR, tvR), ("br", RxR, tvR)):
        th = p["thT"] if q[0] == "t" else p["thB"]
        d += qring(cx, cy[q], Rx, p["Ry" + q.upper()], tv, th, q, p["ko_" + q], p["ki_" + q], p.get("dyi_" + q, 0.0))
    d += rect(L, cy["tl"] - 0.01, L + p["tv"], cy["bl"] + 0.01, grow_y=False)
    d += rect(cx + RxR - tvR, cy["tr"] - 0.01, cx + RxR, cy["br"] + 0.01, grow_y=False)
    return d


def letter_d(p):
    return bowl(p) + stem(p["sx"], p["sx"] + p["sw"], p["st"], p["sb"], p["tap_y"], p["tap_dx"])


def letter_i(p):
    return rect(p["sx"], p["st"], p["sx"] + p["sw"], p["sb"]) + circle(p["dx"], p["dy"], p["dr"])


def letter_o(p):
    return "".join(qring(p["cx"], p["cy"], p["Rx"], p["Ry"], p["tv"], p["th"], q, p["ko_" + q], p["ki_" + q])
                   for q in ("tl", "tr", "br", "bl"))


LETTERS = {"n": letter_n, "a": letter_a, "u": letter_u, "d": letter_d, "i": letter_i, "o": letter_o}


# --------------------------------------------------------------------- build

def icon_markup(ink, accent, indent="    "):
    svg = (ROOT / "naudio-icon.svg").read_text()
    inner = svg[svg.index("<g fill"):svg.rindex("</svg>")]
    inner = re.sub(r"<!--.*?-->", "", inner, flags=re.S)
    inner = inner.replace(f'fill="{INK}"', f'fill="{ink}"').replace(f'fill="{ACCENT}"', f'fill="{accent}"')
    lines = [l.strip() for l in inner.splitlines() if l.strip()]
    return "\n".join(indent + ("" if l.startswith(("<g", "</g")) else "  ") + l for l in lines)


def build(weight=500, tracking=-0.010, ink=INK, accent=ACCENT, icon_only=False):
    global D
    if icon_only:
        return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 157 142">\n'
                f'  <!-- NAudio icon (generated by branding/build_logo.py) -->\n'
                f'{icon_markup(ink, accent, "  ")}\n</svg>\n')
    params = json.loads((HERE / "wordmark.json").read_text())
    D = max(0.0, (weight - ORIGINAL_WEIGHT) / 100 * GROW_PER_100)
    paths = {}
    for i, (k, f) in enumerate(LETTERS.items()):
        p = dict(params[k])
        for key in XKEYS[k]:
            p[key] += i * tracking * EM
        paths[k] = f(p)
    D = 0.0
    # keep the original right-hand margin (619 - 608.94) after the o
    o = params["o"]
    right = o["cx"] + 5 * tracking * EM + o["Rx"] + GROW_PER_100 * max(0, weight - ORIGINAL_WEIGHT) / 100
    width = right + (619 - (o["cx"] + o["Rx"]))
    dark = "\n".join(f'    <path d="{paths[k]}"/>' for k in "audio")
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {n(width)} 143">
  <!-- NAudio logo, weight {weight:g}, tracking {tracking:+.3f}em
       (generated by branding/build_logo.py; see branding/README.md) -->
  <g transform="translate(8 0)">
{icon_markup(ink, accent)}
  </g>
  <path fill="{accent}" d="{paths['n']}"/>
  <g fill="{ink}">
{dark}
  </g>
</svg>
'''


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--preset", choices=sorted(PRESETS), default="2026",
                    help="named weight/tracking: 2015 = original artwork, 2026 = current logo (default)")
    ap.add_argument("--weight", type=float, help="override the preset's weight (300 = original artwork)")
    ap.add_argument("--tracking", type=float, help="override the preset's extra letter spacing, in em")
    ap.add_argument("--ink", default=INK, help=f"colour of the grey parts (default {INK})")
    ap.add_argument("--accent", default=ACCENT, help=f"colour of the red parts (default {ACCENT})")
    ap.add_argument("--icon-only", action="store_true", help="write just the icon (for recoloured variants)")
    ap.add_argument("-o", "--output", required=True, help="SVG file to write")
    a = ap.parse_args()
    weight, tracking = PRESETS[a.preset]
    weight = weight if a.weight is None else a.weight
    tracking = tracking if a.tracking is None else a.tracking
    Path(a.output).write_text(build(weight, tracking, a.ink, a.accent, a.icon_only))
    print("wrote", a.output)


if __name__ == "__main__":
    main()
