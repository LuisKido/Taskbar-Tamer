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

> **Línea de diseño: GUERREROS BIOMECÁNICOS HUMANOIDES** (cyborg/mecha-guerrero, mezcla de
> humanoide + tecnología + un toque animal). Estilo de las formas humanoides de Digimon.

```
Pixel art 2D, sprite de videojuego retro 16-bit, estilo guerrero coleccionable (tipo
Digimon humanoide). Personaje HUMANOIDE BIOMECÁNICO: cuerpo bípedo con armadura y partes
cibernéticas (placas metálicas, circuitos, tubos, LEDs/luces de neón, articulaciones de
pistón) y rasgos de bestia. Cuerpo completo de pie, VISTA LATERAL PURA mirando a la
DERECHA. Silueta clara y legible. Sombreado por celdas con 3-4 tonos por color y CONTORNO
oscuro de 1px. Paleta limitada y cohesiva, sin antialias.

FONDO 100% TRANSPARENTE (canal alfa real). NO pintes un patrón de cuadros ni un fondo
gris: deja los píxeles del fondo totalmente vacíos/transparentes. Sin texto, sin marca de
agua, sin escena, sin sombra en el suelo. Sujeto centrado con margen transparente.
```

**Salida:** PNG con **transparencia real**. Para frames añade al final →
`Hoja horizontal de 4 cuadros del mismo tamaño en una rejilla, sin separación ni fondo entre cuadros: idle, idle, ataque, daño.`

## Criaturas del jugador (5 especies)

Pega `[estilo base]` + uno de estos:

| Especie | Sujeto (HUMANOIDE biomecánico + color + rol/habilidad) |
|---|---|
| **Mordak (Guardian)** | Guerrero **humanoide** acorazado tipo juggernaut/caballero, color **azul**. Armadura pesada de placas, **gran escudo de energía** en un brazo, casco con visor, complexión robusta. Toque de tortuga (caparazón en la espalda). Rol: TANQUE (habilidad Provocar). |
| **Rendkar (Bruiser)** | **Humanoide** artillero pesado, color **naranja**. Brazo-cañón biomecánico, hombreras macizas, postura de disparo. Rol: PEGADOR A DISTANCIA (habilidad Ráfaga: proyectil pesado). |
| **Voltfang (Charger)** | **Humanoide** ágil tipo ninja/velocista, color **cian**. Delgado, líneas de energía eléctrica, piernas con amortiguadores, estela de rayo. Rol: VELOZ/EVASIVO (habilidad Esquiva: dash). |
| **Skarn (Leaper)** | **Humanoide** acróbata, color **morado**. Piernas con **pistones/resortes** hidráulicos sobredimensionados, cuerpo compacto y flexible. Rol: SALTADOR (habilidad Salto). |
| **Toxia (Venomous)** | **Humanoide** alquimista/asesino tóxico, color **verde**. Tanques y tubos de **veneno luminoso** en la espalda, guanteletes con jeringas/aguijones, capucha. Rol: VENENO EN ÁREA (habilidad Estallido tóxico). |

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
