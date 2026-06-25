# Assets de arte (PNG)

Imágenes pixel-art del juego. Al estar dentro de `game/`, se cargan como `res://assets/...`.

## Flujo (importante)

1. La **hoja original** (sheet) generada por IA va en **`art_source/creatures/<nombre>.png`**
   (fuera de `game/`, para que Godot no importe el archivo pesado).
2. Se **corta** con el script → genera frames limpios y ligeros aquí:
   ```
   python scripts/slice_sprite.py art_source/creatures/mordak.png game/assets/creatures/mordak 4 2
   ```
   El script **quita el fondo gris** (lo deja transparente), recorta cada cuadro a su
   contenido y lo reduce de tamaño.
3. El juego carga `creatures/<nombre>/0.png, 1.png, ...` (frames 0 y 1 = ciclo idle).

## Convención

- **Vista lateral, la criatura MIRA A LA DERECHA** (el motor la voltea hacia su objetivo).
- El sheet puede traer el fondo "cuadriculado/gris" del generador: el script lo limpia.
- Layout recomendado del sheet: rejilla de cuadros (p. ej. 4×2). El orden ideal: los
  primeros frames = idle/caminar; luego poses (ataque, escudo, etc.).

## Criaturas esperadas (`creatures/<nombre>/`)

| Carpeta | Especie | Estado |
|---|---|---|
| `mordak/` | Guardian (tanque, azul) | ✅ |
| `rendkar/` | Bruiser (pegador, naranja) | pendiente |
| `voltfang/` | Charger (veloz, cian) | pendiente |
| `skarn/` | Leaper (saltador, morado) | pendiente |
| `toxia/` | Venomous (veneno, verde) | pendiente |

Las que falten usan el sprite generado por código como respaldo. Prompts en
[docs/07-prompt-assets.md](../../docs/07-prompt-assets.md).
