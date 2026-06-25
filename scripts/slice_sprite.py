#!/usr/bin/env python3
"""Corta una hoja de sprites en cuadros limpios y ligeros.

- Quita el fondo gris NEUTRO (R≈G≈B en tono medio) → transparente. La criatura es
  de color, así que no se ve afectada.
- Corta la rejilla cols x rows, recorta cada cuadro a su contenido y lo reduce a un
  tamaño manejable (más liviano).

Uso:
  python scripts/slice_sprite.py <hoja.png> <salida_dir> [cols] [rows] [max_px]
Ej:
  python scripts/slice_sprite.py game/assets/creatures/mordak.png game/assets/creatures/mordak 4 2
"""
import sys, os
from PIL import Image


def key_neutral_gray(cell):
    """Pone transparente el fondo gris neutro de tono medio."""
    px = list(cell.getdata())
    out = []
    keyed = 0
    for (r, g, b, a) in px:
        mx, mn = max(r, g, b), min(r, g, b)
        avg = (r + g + b) // 3
        if (mx - mn) < 22 and 95 <= avg <= 180:
            out.append((r, g, b, 0))
            keyed += 1
        else:
            out.append((r, g, b, a))
    cell.putdata(out)
    return keyed


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    src, out = sys.argv[1], sys.argv[2]
    cols = int(sys.argv[3]) if len(sys.argv) > 3 else 4
    rows = int(sys.argv[4]) if len(sys.argv) > 4 else 2
    max_px = int(sys.argv[5]) if len(sys.argv) > 5 else 256

    os.makedirs(out, exist_ok=True)
    sheet = Image.open(src).convert("RGBA")
    W, H = sheet.size
    cw, ch = W // cols, H // rows
    idx = 0
    for r in range(rows):
        for c in range(cols):
            cell = sheet.crop((c * cw, r * ch, c * cw + cw, r * ch + ch)).copy()
            keyed = key_neutral_gray(cell)
            bbox = cell.getbbox()
            if bbox:
                cell = cell.crop(bbox)
            # Reduce a max_px (lado mayor) preservando proporción.
            w, h = cell.size
            scale = min(1.0, max_px / max(w, h))
            if scale < 1.0:
                cell = cell.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
            cell.save(os.path.join(out, f"{idx}.png"))
            print(f"  frame {idx}: {cell.size}  (keyed {keyed}px)")
            idx += 1
    print(f"OK: {idx} cuadros en {out}")


if __name__ == "__main__":
    main()
