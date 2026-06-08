# 01 — Documento de Diseño (GDD)

> El **qué**. Este documento describe la visión de juego. Para el **cómo** técnico, ver [02-arquitectura-tecnica.md](02-arquitectura-tecnica.md).

## 1. Pitch

Gestionas un equipo de criaturas en segundo plano mientras usas tu PC. En lugar de espadas y armaduras, recolectas **mutaciones genéticas y partes biológicas** para equipar a tus bestias. El objetivo es armar la composición perfecta (un *meta-team*) para escalar en las ligas competitivas globales de batallas automatizadas.

- **Género:** Idle / Auto-Battler Táctico de Colección.
- **Plataforma:** PC (Steam), como widget de barra de tareas de bajo consumo.
- **Fantasía del jugador:** "Crío y optimizo bestias perfectas mientras trabajo; mi paciencia se convierte en dominio competitivo."

## 2. Bucle de Juego

### Fase Inactiva — Farming en la barra de tareas
Las criaturas recorren biomas de forma autónoma, enfrentando enemigos y recolectando experiencia.

- **Botín (Loot):** recolectan equipo biológico de distintas rarezas (Común → Legendario) que va a un inventario.
- **Bajo consumo:** esta fase **no** simula combate en tiempo real. Acumula tiempo transcurrido y resuelve recompensas por tablas de probabilidad (ver [arquitectura §4](02-arquitectura-tecnica.md#4-fase-idle-progreso-por-tiempo)).

### Fase Activa — Management y Táctica
El jugador abre la interfaz principal para:

- Fusionar partes repetidas.
- Gestionar el equipo y el inventario.
- Criar nuevas generaciones (herencia genética).
- Organizar la formación de combate (posicionamiento).

## 3. Equipamiento Biológico

Reemplaza el equipo clásico por **ranuras de anatomía**:

| Categoría | Ranuras (ejemplos) | Determinan |
|-----------|--------------------|-----------|
| **Ofensiva** | Garras, colmillos, aguijones | Daño, críticos, tipo de ataque |
| **Defensiva** | Caparazones, pelajes densos, escamas | Salud, resistencias |
| **Utilidad / Movilidad** | Alas, colas, glándulas venenosas | Velocidad, evasión, pasivas de estado |

### Sinergias de Set
Equipar varias partes de una misma **familia** (ej. 3 de "Bestia Abisal") otorga bonificaciones pasivas escalonadas (2/3/4 piezas), obligando a pensar en sinergias y no solo en números base.

### Fusión
Las partes repetidas se fusionan para mejorar rareza/estadísticas, dando salida al loot duplicado.

## 4. Profundidad Táctica y Crianza

### Posicionamiento
La preparación del equipo lo es todo. El jugador decide:
- **Línea frontal:** absorbe daño (tanques).
- **Retaguardia:** aplica daño o estados alterados.

### Herencia Genética
Las criaturas tienen **límite de nivel**. Al "retirar" a un campeón veterano, este transmite **uno de sus rasgos o equipamientos** como estadística base permanente para la siguiente generación. Permite criar bestias perfectas a lo largo del tiempo.

## 5. Ecosistema Competitivo (Steam / Multijugador)

### Ranked Asíncrono (Auto-battler)
Las criaturas **no** pelean en tiempo real. El jugador bloquea su *Setup* y el **servidor simula** las batallas contra formaciones de otros jugadores para definir ascensos de liga (Bronce, Plata, Oro…).

> **Requisito clave:** la simulación debe ser **determinista**. El mismo par de setups + semilla produce siempre el mismo resultado, de modo que la previsualización del cliente coincida con el veredicto del servidor.

### Temporadas (Seasons)
Clasificaciones mensuales o *Time-Attacks* con reglas variables (ej. "esta temporada las partes Venenosas son menos efectivas") para mantener el meta fresco.

### Economía de Mercado
Las partes genéticas raras/perfectas pueden venderse o intercambiarse en el **Steam Community Market**, dando valor real a la perseverancia.

## 6. Pilares de diseño (criterios para decir "sí" o "no" a una feature)

1. **Respeta el tiempo del jugador.** El progreso ocurre aunque no mires. La fase activa es opcional, nunca obligatoria minuto a minuto.
2. **Profundidad por sinergia, no por números.** Las decisiones interesantes vienen de sets + posicionamiento + estados, no solo de stats más altos.
3. **Justicia competitiva.** Determinismo y simulación del lado del servidor. Cero pay-to-win directo (el mercado mueve partes, no poder garantizado).
4. **Bajo consumo siempre.** Si una feature dispara el uso de CPU/RAM en idle, se rediseña o se descarta.
