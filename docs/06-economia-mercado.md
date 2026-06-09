# 06 — Economía, Drops y Mercado (decisiones de diseño)

> Decisiones fundacionales de la economía. Implementación en **Fase 5 (servidor)** y
> **Fase 6 (Steam Market)**, pero se diseñan ahora para no construir en contra de ellas.

## 1. Decisión clave: mercado de poder + ranked normalizado

**Las partes biológicas dan poder y SON intercambiables** en el Steam Market (la
perseverancia y las partes raras tienen valor real, como pide el GDD). Para que esto NO
sea pay-to-win, **el ranked normaliza las stats**:

- **PvE / idle / arena / colección:** usan tus partes reales (su poder cuenta).
- **Ranked (auto-battler asíncrono):** las stats de cada parte se **normalizan al valor
  canónico de su tier/familia**. Es decir, en ranked una parte "perfecta" Mega rinde
  igual que una Mega "mínima" de la misma familia/ranura. Lo que decide es **el build
  (qué familias/sets/posiciones eliges) + skill**, no la magnitud comprada.

**Resultado:** comprar partes te da colección, poder en PvE, conveniencia (saltarte
grindeo) y lucirte — pero **no compra posición en el ladder**. Economía viva, ranked justo.

> Implicación de diseño: el "techo de poder" de una parte en ranked depende de su
> **tier + familia + ranura**, no de su roll exacto. Los rolls perfectos importan para
> PvE/colección/mercado, no para la integridad competitiva.

## 2. Drops: "Semilla Génesis de Temporada" (escasez verificable)

Dos piezas:

### a) Drops deterministas (anti-fabricación)
Cada drop se deriva de `(semillaTemporada, cuentaId, contadorDrop)`. Ya tenemos
determinismo local (`FarmingRngState` + `DeterministicRng`). Para una economía real, el
**servidor calcula/valida** los drops → nadie puede inventar ítems (anti-duplicación).
Es la base de que un ítem tenga valor de mercado.

### b) Suministro limitado (escasez real)
Cap **global de acuñación por tier y temporada**. Ej.: solo existen N partes **BioMerge**
de cada familia por temporada; al alcanzar el cap, los drops de ese tipo **bajan al tier
inferior**. El servidor lleva la cuenta. Esto da escasez verificable (tipo edición
limitada) → valor de mercado estable, ligado a la perseverancia.

> Los tiers altos (Mega / Burst Mode / BioMerge) son los acuñados con cap. Los bajos
> (Fresh…Champion) son abundantes (no se comercian o valen poco).

## 3. Steam Market "sano"

Principios (modelo probado, evita gambling y pay-to-win):

- **Todo se gana jugando.** Cero loot boxes con llaves, cero gambling.
- **El dev NO vende poder.** A lo sumo cosméticos. El mercado es jugador↔jugador.
- **Steam Inventory Service** para los ítems; Steam retiene ~15% por transacción, que
  actúa de **sumidero anti-inflación** (saca ítems/valor del sistema).
- **Fondos en Steam Wallet** (no cash-out directo): mantiene la economía dentro del
  ecosistema y reduce riesgos legales de "dinero real".
- **Cooldowns de intercambio + Steam Guard** (los gestiona Steam).
- Solo se acuñan/comercian los **tiers altos con cap**; lo común no satura el mercado.

## 4. Dónde vive cada pieza

| Pieza | Fase | Estado |
|-------|------|--------|
| Rarezas/tiers (8) + RNG determinista | core | ✅ existe |
| Normalización de stats en ranked | core + servidor | diseño |
| Semilla de temporada + caps de acuñación | servidor | Fase 5 |
| Validación de drops (anti-fabricación) | servidor | Fase 5 |
| Steam Inventory + Market | cliente + backend | Fase 6 |

## 5. Riesgos / pendientes

- Definir la **fórmula exacta de normalización** en ranked (techo por tier+familia).
- Definir los **caps de acuñación** por tier/temporada (números de balance).
- Backend de firma de transacciones para el Steam Inventory Service.
- Cumplir políticas de Steam sobre ítems (earned, no gambling).
