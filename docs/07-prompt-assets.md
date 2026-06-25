# 07 — Prompt para generar assets (Gemini / IA de imagen)

> Prompts para producir el arte **pixel-art 2D** del juego con tema **animal + tecnología**
> (criaturas biomecánicas, vibe Digimon). Pega el "estilo base" + el sujeto concreto.
> Genera **un asset por imagen**. Pide siempre **fondo transparente y sin texto**.

## Formato preciso y perspectiva (LÉELO PRIMERO)

- **Perspectiva: vista lateral (side view), la criatura MIRA A LA DERECHA.** Aunque el
  campo se vea "desde arriba", usamos sprites laterales tipo *billboard* (como el dino de
  referencia) y los **volteamos en el motor** hacia su objetivo. Estándar en 2D y aprovecha
  el detalle. **No** pidas vista cenital.
- **Fondo TRANSPARENTE**, sin texto, sin marca de agua, sin escena, sin sombra de suelo.
- **Un asset por imagen** (o una hoja de frames, ver abajo). Lienzo **cuadrado**, criatura
  **centrada** con margen transparente. Nosotros recortamos/escalamos.
- **Frames para recortar nosotros:** pide una **HOJA HORIZONTAL** de N cuadros del **mismo
  tamaño**, **alineados a una rejilla, SIN separación ni fondo entre cuadros** (así cortamos
  dividiendo el ancho entre N). Recomendado: **4 frames** → idle-1, idle-2, ataque, daño.
  *(Lo más simple: **1 solo frame idle** y animamos el rebote por código.)*
- **Consistencia:** misma paleta por color de especie, **contorno oscuro de 1px**, luz
  arriba-izquierda, mismo nivel de detalle y grosor de línea en todos.

## Set mínimo (v1) — genera SOLO esto primero

1. **5 criaturas** (idle, lateral, mirando a la derecha) — tabla de abajo.
2. **3 secuaces** (uno por bioma).
3. **3 jefes** (uno por bioma).

> Las **9 partes**, los **marcos de rareza** y la **GUI** se siguen dibujando por código por
> ahora; déjalos para una v2.

## Estilo base (preámbulo — pégalo antes de cada sujeto)

> **Línea de diseño: BESTIAS-GUERRERAS ANTROPOMORFAS (furros) BIOMECÁNICAS.** Animales
> humanoides bípedos (estilo furro/kemono) con armadura y tecnología, vibe Digimon
> humanoide. **Fondo de color sólido (chroma key)** — Gemini no hace transparencia, así
> que usamos un fondo plano que luego quitamos por código.

```
Pixel art 2D, sprite de videojuego retro 16-bit, estilo guerrero coleccionable tipo
Digimon. Personaje ANTROPOMORFO (furro): ANIMAL HUMANOIDE BÍPEDO, de pie en dos piernas,
con pelaje/escamas y armadura + partes biomecánicas (placas metálicas, circuitos, tubos,
LEDs de neón, articulaciones de pistón). Cuerpo completo, VISTA LATERAL PURA mirando a la
DERECHA, postura heroica. Silueta clara y legible. Sombreado por celdas con 3-4 tonos por
color y CONTORNO oscuro de 1px. Paleta limitada, sin antialias.

FONDO DE COLOR PLANO Y UNIFORME tipo chroma key: VERDE BRILLANTE puro (#00FF00),
exactamente igual en toda la imagen, SIN degradados ni sombras ni patrones. El personaje
NO debe usar ese verde de fondo. Sin texto, sin marca de agua. Personaje centrado.
```

> Para la criatura **verde (Toxia)** usa fondo **MAGENTA (#FF00FF)** en vez de verde, para
> que no se borre su cuerpo. El slicer detecta el color de fondo automáticamente.

**Salida:** PNG con fondo verde/magenta plano. Para frames añade al final →
`Hoja horizontal de 4 cuadros del mismo tamaño en una rejilla, mismo fondo verde plano entre cuadros: idle, idle, ataque, daño.`

## Criaturas del jugador (5 especies)

Pega `[estilo base]` + uno de estos:

| Especie | Sujeto (BESTIA ANTROPOMORFA bípeda + color + rol/habilidad) |
|---|---|
| **Mordak (Guardian)** | **Tortuga antropomorfa** guerrera bípeda, color **azul**. Armadura pesada de placas, caparazón en la espalda, **gran escudo de energía** en un brazo, casco con visor. Robusto. Rol: TANQUE (Provocar). Fondo verde. |
| **Rendkar (Bruiser)** | **Rinoceronte/toro antropomorfo** bípedo y musculoso, color **naranja**. **Brazo-cañón** biomecánico, hombreras macizas, cuerno. Rol: PEGADOR A DISTANCIA (Ráfaga). Fondo verde. |
| **Voltfang (Charger)** | **Lobo antropomorfo** ninja bípedo, color **cian**, esbelto. Pelaje, líneas de energía eléctrica, hojas/garras, estela de rayo. Rol: VELOZ/EVASIVO (Esquiva). Fondo verde. |
| **Skarn (Leaper)** | **Rana/conejo antropomorfo** acróbata bípedo, color **morado**. Piernas con **pistones/resortes** hidráulicos, ágil. Rol: SALTADOR (Salto). Fondo verde. |
| **Toxia (Venomous)** | **Escorpión/lagarto antropomorfo** bípedo, color **verde**. Tanques de **veneno luminoso** en la espalda, guanteletes con jeringas/aguijón, capucha. Rol: VENENO (Estallido tóxico). **Fondo MAGENTA** (no verde). |

> Opcional (animación): pide **una fila de 2-4 frames** del mismo tamaño (idle con leve rebote, ataque) como sprite sheet horizontal, fondo transparente, celdas uniformes.

## Prompts en INGLÉS (listos para pegar — uno por criatura)

> Fondo verde plano (chroma) salvo Toxia (magenta). El slicer detecta y quita el fondo.

**Mordak (Guardian — tank, blue):**
```
Pixel art 2D, 16-bit retro game sprite, collectible monster style (Digimon-like). An ANTHROPOMORPHIC (furry/kemono) BIPEDAL ANIMAL WARRIOR standing on two legs, with fur/scales, armor and biomechanical tech parts (metal plates, circuits, tubes, glowing neon LEDs, piston joints). Full body, PURE SIDE VIEW facing RIGHT, heroic stance. Clear readable silhouette. Cel-shaded, 3-4 tones per color, dark 1px outline. Limited cohesive palette, no anti-aliasing. FLAT SOLID UNIFORM CHROMA-KEY BACKGROUND: pure bright GREEN #00FF00, identical across the whole image, no gradient, no shadow, no pattern; the character must NOT use that green. No text, no watermark, no scene. Character centered.
Subject: "Mordak", an anthropomorphic TURTLE warrior, bipedal, BLUE. Heavy plated armor, a turtle shell on the back, a large glowing ENERGY SHIELD on one arm, visored helmet, sturdy heavy build. Role: defensive tank.
Horizontal sheet of 4 equal-size frames in a single row, same flat green background between frames, same character size and position in each frame: idle, idle, attack, hurt.
```

**Rendkar (Bruiser — heavy ranged, orange):**
```
[same style preamble as above]
Subject: "Rendkar", an anthropomorphic RHINO/BULL warrior, bipedal, muscular, ORANGE. A biomechanical ARM-CANNON on one arm, massive shoulder pads, a horn, aiming stance. Role: heavy ranged attacker.
Horizontal sheet of 4 equal-size frames in a single row, same flat green background between frames, same character size and position: idle, idle, attack, hurt.
```

**Voltfang (Charger — fast evasive, cyan):**
```
[same style preamble]
Subject: "Voltfang", an anthropomorphic WOLF ninja, bipedal, slim and agile, CYAN. Fur, electric energy lines, blades/claws, lightning accents. Role: fast evasive striker.
Horizontal sheet of 4 equal-size frames in a single row, same flat green background between frames, same character size and position: idle, idle, attack, hurt.
```

**Skarn (Leaper — jumper, purple):**
```
[same style preamble]
Subject: "Skarn", an anthropomorphic FROG/RABBIT acrobat, bipedal, PURPLE. Oversized spring/piston hydraulic legs, lithe flexible body. Role: jumping acrobat.
Horizontal sheet of 4 equal-size frames in a single row, same flat green background between frames, same character size and position: idle, idle, attack, hurt.
```

**Toxia (Venomous — poison AoE, green) — MAGENTA background:**
```
[same style preamble BUT replace the background line with:]
FLAT SOLID UNIFORM CHROMA-KEY BACKGROUND: pure bright MAGENTA #FF00FF, identical across the whole image, no gradient/shadow/pattern; the character must NOT use that magenta.
Subject: "Toxia", an anthropomorphic SCORPION/LIZARD warrior, bipedal, GREEN. Glowing venom tanks and tubes on the back, gauntlets with syringes/stinger, a hood. Role: poison area attacker.
Horizontal sheet of 4 equal-size frames in a single row, same flat magenta background between frames, same character size and position: idle, idle, attack, hurt.
```

## Enemigos por mapa (3 biomas)

Criaturas-secuaz biomecánicas, más simples y "monstruosas" (blob con pinchos), 1 por bioma + su jefe (más grande, imponente, con cuernos):

| Bioma | Enemigos | Jefe |
|---|---|---|
| **Bosque Abisal** | Insecto/planta biomecánico **verde** | **Devorador Abisal** (enorme, fauces, raíces-cables) |
| **Cavernas de Magma** | Criatura roca/lava biomecánica **naranja-roja** | **Coloso de Magma** (cuerpo de roca con grietas de lava y metal) |
| **Tundra Espectral** | Espectro/criatura de hielo biomecánica **azul claro** | **Heraldo Glacial** (cristales de hielo y placas heladas) |

## Prompts en INGLÉS — enemigos y jefes (bestias monstruosas, NO antropomorfas)

> Bestias feroces biomecánicas (a 4 patas / mole), para contrastar con los héroes
> humanoides. Fondo croma **VERDE** salvo las criaturas verdes → **MAGENTA**.

**Style preamble (enemies):**
```
Pixel art 2D, 16-bit retro game sprite, monster style (Digimon-like). A FERAL BIOMECHANICAL MONSTER, a wild beast (NOT humanoid, NOT bipedal warrior): four-legged or hulking creature mixing organic flesh with machine parts (metal plates, exposed cables, glowing eyes, tech growths, neon). Full body, PURE SIDE VIEW facing RIGHT, menacing. Clear readable silhouette. Cel-shaded, 3-4 tones per color, dark 1px outline, limited palette, no anti-aliasing. FLAT SOLID UNIFORM CHROMA-KEY BACKGROUND: pure bright GREEN #00FF00 (or MAGENTA #FF00FF if the creature itself is green), identical across the whole image, no gradient/shadow/pattern; the creature must NOT use the background color. No text, no watermark, no scene. Centered.
Horizontal sheet of 4 equal-size frames in a single row, same flat background between frames, same size and position: idle, idle, attack, hurt.
```

**Minions (one per biome) — add to the preamble:**
- **Bosque Abisal** (GREEN → use **MAGENTA** bg): `Subject: a small feral biomech plant-insect beast, GREEN, with vine-like cables, mandibles and a glowing core, snarling, low to the ground.`
- **Cavernas de Magma** (green bg): `Subject: a small feral biomech magma hound, ORANGE-RED, cracked rock body with lava glow and metal plating, aggressive.`
- **Tundra Espectral** (green bg): `Subject: a small feral biomech ice beast, pale ICY BLUE, ice crystals and frosted metal, ghostly glow, fanged.`

**Bosses (huge and imposing) — add to the preamble (bigger canvas):**
- **Devorador Abisal** (GREEN → **MAGENTA** bg): `Subject: HUGE BOSS "Abyssal Devourer", a massive feral biomech beast with a giant fanged maw, root-like cables and a glowing core, GREEN, terrifying and imposing.`
- **Coloso de Magma** (green bg): `Subject: HUGE BOSS "Magma Colossus", a giant biomech golem-beast of molten rock and metal plates, ORANGE-RED lava cracks, towering and heavy.`
- **Heraldo Glacial** (green bg): `Subject: HUGE BOSS "Glacial Herald", a giant biomech ice entity with crystal shards and frozen armor plates, ICY BLUE, regal and cold.`

## Iconos de partes (9 ranuras de anatomía)

Cada parte como un **objeto biomecánico aislado**, icono cuadrado, fondo transparente, ~32×32:

- **Ofensivas:** Garras metálicas · Colmillos cibernéticos · Aguijón con tubo de veneno.
- **Defensivas:** Caparazón de placas · Pelaje con fibras tech · Escamas metálicas.
- **Utilidad:** Alas mecánicas · Cola con pistón · Glándulas/tanques luminosos.

## Fondos de arena (uno por bioma) — INGLÉS

> Suelo en **vista cenital**, **oscuro y de bajo contraste** (los personajes brillantes
> deben resaltar), **seamless/tileable** (sin costuras, para rellenar cualquier tamaño).
> Sin chroma key (es un fondo opaco). Guardar en `game/assets/maps/<bioma>.png`.

**Style preamble (backgrounds):**
```
Pixel art 2D, 16-bit retro game background, TOP-DOWN ground/floor texture for a battle arena seen from directly above. SEAMLESS TILEABLE texture: the pattern wraps perfectly on all four edges with no visible seams. DARK and LOW-CONTRAST overall so bright characters placed on top clearly stand out. Subtle even detail across the whole image, no single focal point, no strong highlights, no light source. NO characters, NO creatures, NO objects, NO text, NO UI, NO border, NO vignette. Cohesive limited dark palette. Square image, flat top-down perspective.
```

**Subjects (añade al preámbulo):**
- **Bosque Abisal** (`bosque.png`): `Subject: dark abyssal forest floor — damp mossy ground, very dark teal-green, scattered roots and cables, faint bioluminescent spores, wet dark stone.`
- **Cavernas de Magma** (`magma.png`): `Subject: dark volcanic cavern floor — charred black rock and ash, thin DIM cracks of glowing lava, a few embers. Very dark with faint orange glow.`
- **Tundra Espectral** (`tundra.png`): `Subject: dark frozen tundra floor — cracked ice and frost over dark stone, pale blue tints, faint spectral glow. Very dark cold blue palette.`

> Estos NO se cortan ni se les quita fondo: van directos a `game/assets/maps/` y los uso
> como textura tileada de la arena.

## Marcos de rareza (8 tiers)

Para los marcos/auras de los objetos, usa estos colores (Fresh→BioMerge):

```
Fresh gris · In-Training verde · Rookie azul · Champion morado ·
Ultimate dorado · Mega rojo · Burst Mode cian · BioMerge blanco radiante
```

Pide un **marco de inventario pixel-art** (estilo placa tecnológica con esquinas remachadas) en cada color, fondo transparente, cuadrado.

## GUI (interfaz híbrida moderna + pixel)

- **Panel/marco 9-slice:** borde pixel-art tipo placa metálica oscura con esquinas y remaches, interior translúcido oscuro. Pídelo como textura **9-patch** (bordes y esquinas claros para recortar).
- **Botón** (normal / presionado): cápsula con bisel pixel, tono oscuro + acento de neón.
- **Iconos UI:** engranaje (opciones), candado (desbloquear), espada (batalla), escudo (formación), hélice/ADN (gestión).

## Consejos de consistencia

- Mantén la **misma paleta y grosor de contorno** en todos los assets.
- Misma **fuente de luz** (arriba-izquierda) para que combinen.
- Entrega cada sprite **centrado** y con **margen de 1-2 px** transparente.
- Nombres de archivo sugeridos: `creature_guardian.png`, `enemy_magma.png`, `boss_glacial.png`, `part_claws.png`, `frame_mega.png`, `ui_panel.png`, etc.
