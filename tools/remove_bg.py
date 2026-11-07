#!/usr/bin/env python3
"""Quita el fondo de las imágenes de ArtDrop/ (Fase 10).

Gemini no entrega PNG con alfa real: las piezas se piden sobre croma #00FF00 (o #FF00FF si el objeto es verde/turquesa).
1) Si está instalado rembg (MIT, `pip install rembg`), se usa el modelo local.
2) Si no, se aplica una llave de croma con supresión de derrame (Pillow).
Solo procesa los ids del manifiesto con "alpha": true. Escribe encima del archivo en ArtDrop/ (guarda copia .orig.png).

Uso: python3 tools/remove_bg.py [--chroma-only]
"""
import json
import os
import shutil
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DROP = os.path.join(ROOT, "ArtDrop")


def chroma_key(img, key):
    img = img.convert("RGBA")
    px = img.load()
    kr, kg, kb = key
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            # Distancia al croma en el canal dominante: verde (o magenta) muy por encima de los otros canales.
            if key == (0, 255, 0):
                dominance = g - max(r, b)
            else:
                dominance = min(r, b) - g
            if dominance > 90:
                px[x, y] = (r, g, b, 0)
            elif dominance > 30:
                alpha = int(255 * (1 - (dominance - 30) / 60))
                if key == (0, 255, 0):
                    g = max(r, b)  # quita el derrame verde del borde
                else:
                    r = b = g
                px[x, y] = (r, g, b, min(a, alpha))
    return img


def main():
    try:
        from PIL import Image
    except ImportError:
        sys.exit("Falta Pillow: pip install pillow")
    use_rembg = "--chroma-only" not in sys.argv
    remove = None
    if use_rembg:
        try:
            from rembg import remove as _remove
            remove = _remove
        except ImportError:
            print("rembg no está instalado: se usa llave de croma.")
    manifest = json.load(open(os.path.join(ROOT, "docs", "ASSET_MANIFEST.json"), encoding="utf-8"))
    done = 0
    for a in manifest["assets"]:
        if not a["alpha"]:
            continue
        path = os.path.join(DROP, a["id"] + ".png")
        if not os.path.exists(path):
            continue
        shutil.copyfile(path, path.replace(".png", ".orig.png"))
        img = Image.open(path)
        if remove is not None:
            img = remove(img)
        else:
            green = any(k in a["id"] for k in ("oliva", "turquesa"))
            img = chroma_key(img, (255, 0, 255) if green else (0, 255, 0))
        img.save(path)
        done += 1
        print("✔", a["id"])
    print(done, "imágenes procesadas")


if __name__ == "__main__":
    main()
