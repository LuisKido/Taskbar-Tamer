# Assets de arte (PNG)

Imágenes pixel-art del juego. Al estar dentro de `game/`, se cargan como `res://assets/...`.

## Convención

- **Vista lateral, la criatura MIRA A LA DERECHA** (el motor la voltea hacia su objetivo).
- **PNG con fondo transparente**, sujeto centrado.
- **Animación:** hoja **horizontal** de 4 cuadros del mismo tamaño, sin separación entre
  cuadros (idle, idle, ataque, daño). *(Alternativa simple: 1 solo cuadro idle.)*
- Filtro de textura **nearest** (lo aplicamos al integrar, para que el pixel-art quede nítido).

## Archivos esperados

### `creatures/` — las 5 especies del jugador
| Archivo | Especie |
|---|---|
| `mordak.png` | Guardian (tanque, azul) |
| `rendkar.png` | Bruiser (pegador, naranja) |
| `voltfang.png` | Charger (veloz, cian) |
| `skarn.png` | Leaper (saltador, morado) |
| `toxia.png` | Venomous (veneno, verde) |

### `enemies/` — secuaces y jefes
| Archivo | Qué es |
|---|---|
| `minion_bosque.png` | Secuaz Bosque Abisal (verde) |
| `minion_magma.png` | Secuaz Cavernas de Magma (naranja-rojo) |
| `minion_tundra.png` | Secuaz Tundra Espectral (azul) |
| `boss_abisal.png` | Jefe Devorador Abisal |
| `boss_magma.png` | Jefe Coloso de Magma |
| `boss_glacial.png` | Jefe Heraldo Glacial |

> Los prompts para generarlos están en [docs/07-prompt-assets.md](../../docs/07-prompt-assets.md).
