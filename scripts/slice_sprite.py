#!/usr/bin/env python3
"""Corta una hoja de sprites en cuadros limpios y ligeros (chroma key automático).

- Detecta el color de fondo en las esquinas (verde, magenta, gris... cualquiera) y lo
  quita → transparente. La criatura no debe usar ese color exacto de fondo.
- Corta la rejilla cols x rows, recorta cada cuadro a su contenido y lo reduce de tamaño.

Uso:
  python scripts/slice_sprite.py <hoja.png> <salida_dir> [cols] [rows] [max_px] [tol]
Ej (fondo verde croma):
  python scripts/slice_sprite.py art_source/creatures/mordak.png game/assets/creatures/mordak 4 2
"""
import sys, os
from PIL import Image


def detect_bg(cell):
    w, h = cell.size
    pts = [(1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2), (w // 2, 1), (w // 2, h - 2)]
    rs = gs = bs = 0
    for p in pts:
        r, g, b, _ = cell.getpixel(p)
        rs += r; gs += g; bs += b
    n = len(pts)
    return (rs // n, gs // n, bs // n)


def key_bg(cell, tol):
    bg = detect_bg(cell)
    tol2 = tol * tol
    px = list(cell.getdata())
    out = []
    keyed = 0
    for (r, g, b, a) in px:
        d2 = (r - bg[0]) ** 2 + (g - bg[1]) ** 2 + (b - bg[2]) ** 2
        if d2 < tol2:
            out.append((r, g, b, 0))
            keyed += 1
        else:
            out.append((r, g, b, a))
    cell.putdata(out)
    return keyed, bg


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    src, out = sys.argv[1], sys.argv[2]
    cols = int(sys.argv[3]) if len(sys.argv) > 3 else 4
    rows = int(sys.argv[4]) if len(sys.argv) > 4 else 2
    max_px = int(sys.argv[5]) if len(sys.argv) > 5 else 256
    tol = int(sys.argv[6]) if len(sys.argv) > 6 else 95

    os.makedirs(out, exist_ok=True)
    sheet = Image.open(src).convert("RGBA")
    W, H = sheet.size
    cw, ch = W // cols, H // rows
    idx = 0
    for r in range(rows):
        for c in range(cols):
            cell = sheet.crop((c * cw, r * ch, c * cw + cw, r * ch + ch)).copy()
            keyed, bg = key_bg(cell, tol)
            bbox = cell.getbbox()
            if bbox:
                cell = cell.crop(bbox)
            w, h = cell.size
            scale = min(1.0, max_px / max(w, h))
            if scale < 1.0:
                cell = cell.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
            cell.save(os.path.join(out, f"{idx}.png"))
            print(f"  frame {idx}: {cell.size}  bg={bg} keyed={keyed}px")
            idx += 1
    print(f"OK: {idx} cuadros en {out}")


if __name__ == "__main__":
    main()
