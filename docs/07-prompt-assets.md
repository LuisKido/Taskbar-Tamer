# 07 — Prompt para generar assets (Gemini / IA de imagen)

> Prompts para producir el arte **pixel-art 2D** del juego con tema **animal + tecnología**
> (criaturas biomecánicas, vibe Digimon). Pega el "estilo base" + el sujeto concreto.
> Genera **un asset por imagen**. Pide siempre **fondo transparente y sin texto**.

## Estilo base (preámbulo — pégalo antes de cada sujeto)

```
Pixel art 2D, sprite de videojuego retro 16-bit, estilo monstruo coleccionable (tipo
Digimon). Criatura BIOMECÁNICA que combina un animal con tecnología: partes orgánicas
con detalles cibernéticos (placas metálicas, circuitos, tubos, LEDs/luces de neón,
articulaciones de pistón). Vista lateral ligeramente 3/4. Silueta clara y legible a
tamaño pequeño. Sombreado por celdas con 3-4 tonos por color y CONTORNO oscuro de 1px.
Paleta limitada y cohesiva. Resolución nativa baja con píxeles nítidos (nearest-neighbor,
sin difuminado ni antialias). FONDO TRANSPARENTE, sujeto centrado, sin texto, sin marca de
agua, sin escena de fondo, sin sombra de suelo.
```

**Salida pedida:** PNG con transparencia, ~**48×48** px nativo (o 64×64), un sujeto por imagen.

## Criaturas del jugador (5 especies)

Pega `[estilo base]` + uno de estos:

| Especie | Sujeto (animal + tech + color + rol) |
|---|---|
| **Mordak (Guardian)** | Tortuga/escarabajo acorazado biomecánico, color **azul**. Caparazón de placas metálicas con escudo de energía, cuerpo robusto y compacto, mirada tranquila. Rol: tanque defensivo. |
| **Rendkar (Bruiser)** | Felino/rinoceronte biomecánico, color **naranja**. Cuernos y garras de metal afiladas, músculos con refuerzos mecánicos, postura agresiva. Rol: pegador. |
| **Voltfang (Charger)** | Lobo/guepardo cibernético esbelto, color **cian**. Líneas de energía eléctrica, patas veloces con amortiguadores, detalles de rayo. Rol: veloz/evasivo. |
| **Skarn (Leaper)** | Rana/saltamontes biomecánico, color **morado**. Patas traseras tipo resorte/pistón hidráulico, cuerpo ágil y compacto. Rol: saltador. |
| **Toxia (Venomous)** | Escorpión/avispa cibernético, color **verde**. Aguijón y glándulas con tubos de veneno luminoso, antenas. Rol: veneno/área. |

> Opcional (animación): pide **una fila de 2-4 frames** del mismo tamaño (idle con leve rebote, ataque) como sprite sheet horizontal, fondo transparente, celdas uniformes.

## Enemigos por mapa (3 biomas)

Criaturas-secuaz biomecánicas, más simples y "monstruosas" (blob con pinchos), 1 por bioma + su jefe (más grande, imponente, con cuernos):

| Bioma | Enemigos | Jefe |
|---|---|---|
| **Bosque Abisal** | Insecto/planta biomecánico **verde** | **Devorador Abisal** (enorme, fauces, raíces-cables) |
| **Cavernas de Magma** | Criatura roca/lava biomecánica **naranja-roja** | **Coloso de Magma** (cuerpo de roca con grietas de lava y metal) |
| **Tundra Espectral** | Espectro/criatura de hielo biomecánica **azul claro** | **Heraldo Glacial** (cristales de hielo y placas heladas) |

## Iconos de partes (9 ranuras de anatomía)

Cada parte como un **objeto biomecánico aislado**, icono cuadrado, fondo transparente, ~32×32:

- **Ofensivas:** Garras metálicas · Colmillos cibernéticos · Aguijón con tubo de veneno.
- **Defensivas:** Caparazón de placas · Pelaje con fibras tech · Escamas metálicas.
- **Utilidad:** Alas mecánicas · Cola con pistón · Glándulas/tanques luminosos.

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
