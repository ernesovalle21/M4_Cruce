# Presentación — Coordinación Semafórica Responsiva (Onda Verde) · Av. Luis Elizondo
### TC2008 · Guion diapositiva por diapositiva + inventario de archivos

> Cómo usar este documento: cada bloque **Diapositiva N** trae el **título**, los
> **puntos** que van en la slide, el **[Visual]** (figura exacta a pegar) y el
> **Guion** (lo que dices al presentar). Al final está el **inventario de archivos**
> y la **hoja de números clave**.

---

## 0. Inventario de archivos (lo que existe y dónde)

**Reporte y docs**
- `REPORTE.md` — reporte técnico completo (estructura de la entrega).
- `PRESENTACION.md` — este documento.
- `README.md`, `Analisis/ESTADO_PROYECTO.md` — índice y estado del proyecto.

**Python — análisis y RL** (carpeta `Analisis/`)
- `OndaVerde_Elizondo.ipynb` — **notebook consolidado**: offsets, diagramas
  espacio–tiempo, ancho de banda, escenarios de demanda, resultados de Unity y
  **MDP + heurística (J) + Q-learning** (§7, modelo real).
- `SimulacionMultiagente_Elizondo.ipynb` — modelo multiagente AgentPy (vehículos +
  semáforos, comunicación, origen–destino).
- `rl_coordinacion.py` — entorno + métodos de control (fuente del RL, embebido en §7).
- `export_playback.py` — puente Python→Unity (genera `playback.json`).

**Figuras** (carpeta `Analisis/figs/`)
- `Diagrama_espacio_tiempo___sentido_Garcia_Roel_a_Ga.png` — onda verde, sentido principal.
- `Diagrama_espacio_tiempo___sentido_Garza_Sada_a_Gar.png` — sentido inverso.
- `Espacio_tiempo_SIN_coordinar.png` — baseline sin coordinar.
- `rl_comparacion_J.png` — **J: fijo vs heurística vs Q-learning** (barras).
- `rl_curva_aprendizaje.png` — **curva de aprendizaje de Q-learning**.
- `resultados_cola.png`, `resultados_completados.png`, `resultados_resumen.png` — métricas.

**Unity — simulación 3D** (carpeta `Assets/Scripts/`)
- `CorridorBuilder.cs` (Editor) — construye el corredor (menú *M4Cruce*).
- `IntersectionController.cs` — control coordinado-actuado + **extensión de verde (heurística J)**.
- `TrafficLight.cs`, `StopLineTrigger.cs`, `WaypointMover.cs`, `CarSpawner.cs`,
  `YieldGate.cs`, `TrafficMetrics.cs`, `PythonPlayback.cs`.

**Métricas exportadas** (carpeta `Analisis/`)
- `metrics_resumen_*.csv`, `metrics_serie_*.csv` (Unity).

---

## 1. Estructura de la presentación (16 slides, ~12 min)

| # | Slide | Tiempo |
|---|---|---|
| 1 | Portada | 0:30 |
| 2 | Problema | 1:00 |
| 3 | Objetivos | 0:45 |
| 4 | Caso de estudio (3 cruces) | 1:00 |
| 5 | Enfoque multiagente | 1:00 |
| 6 | Onda verde: offsets | 1:00 |
| 7 | Diagrama espacio–tiempo | 1:00 |
| 8 | Función de evaluación J | 1:00 |
| 9 | Formulación MDP | 0:45 |
| 10 | Tres métodos de control | 0:45 |
| 11 | Resultados: comparación J | 1:15 |
| 12 | Q-learning aprende | 1:00 |
| 13 | Coordinación vs no coordinación | 1:00 |
| 14 | Simulación Unity (demo) | 1:00 |
| 15 | Discusión | 0:45 |
| 16 | Conclusiones y trabajo futuro | 0:45 |

---

## Diapositiva 1 — Portada
- **Título:** Coordinación Semafórica Responsiva mediante Sistemas Multiagente
- **Subtítulo:** Onda verde en el corredor Av. Luis Elizondo (Distrito Tec)
- Equipo · matrículas · TC2008 · fecha
- Repo: github.com/ernesovalle21/M4_Cruce

**[Visual]** captura de la escena 3D de Unity (corredor con los 3 cruces).

**Guion:** "Presentamos una estrategia de coordinación de semáforos —la onda verde—
para un corredor real del Distrito Tec, modelada con sistemas multiagente y evaluada
por simulación en Python y Unity."

---

## Diapositiva 2 — Problema
- Sin coordinación, cada semáforo opera aislado → los pelotones **se detienen en cada
  cruce** → más colas, paradas y tiempo de viaje.
- **Onda verde:** desfasar los verdes para que un grupo de autos avance **sin parar**.
- Reto: coordinar **3 intersecciones** y que el control **responda** a la demanda
  (hora pico) **sin romper** la onda verde.

**[Visual]** `Espacio_tiempo_SIN_coordinar.png` (autos chocando con las bandas rojas).

**Guion:** "El problema es coordinar tres semáforos de Av. Luis Elizondo para maximizar
la progresión y minimizar la congestión, y decidir cómo debe reaccionar el control
cuando cambia la demanda."

---

## Diapositiva 3 — Objetivos
- **General:** diseñar y evaluar una coordinación semafórica **responsiva** con enfoque
  multiagente, y compararla contra la operación sin coordinar.
- **Específicos:**
  1. Calcular **offsets** y verificarlos con diagramas espacio–tiempo.
  2. Modelar vehículos y semáforos como **agentes** (Python y Unity).
  3. Definir una **función de evaluación J** (colas, paradas, coordinación).
  4. Comparar **3 métodos**: tiempo fijo, heurística y **Q-learning**.
  5. **Cuantificar la mejora** vs el plan fijo y vs no coordinar.

**Guion:** breve, leer los 5 objetivos como hoja de ruta de la presentación.

---

## Diapositiva 4 — Caso de estudio: 3 cruces
- Av. Luis Elizondo (Distrito Tec, Monterrey):

| ID | Calle transversal | Distancia | Tipo |
|----|------------------|----------:|------|
| S1 | Fernando García Roel | 0 m | cruz |
| S2 | Junco de la Vega | 345 m | **T** con vuelta |
| S3 | Eugenio Garza Sada | 845 m | cruz |

- v de sincronía = **40 km/h** · ciclo **C = 66 s**.

**[Visual]** captura aérea de la escena Unity o mapa con los 3 cruces marcados.

**Guion:** "Tres cruces reales separados 345 y 500 m. Junco es una T con vuelta, lo que
lo hace el más conflictivo."

---

## Diapositiva 5 — Enfoque multiagente
- **Agente Vehículo:** car-following, respeta semáforo, cede al girar, tiene destino (O–D).
- **Agente Semáforo:** mantiene su fase; coordina por **offset** y, en el modo
  distribuido, **avisa por mensaje** al cruce siguiente la llegada del pelotón.
- **3 componentes** del mismo corredor:
  1. Análisis de tráfico (offsets, espacio–tiempo) — Python.
  2. Simulación multiagente — **AgentPy**.
  3. Simulación 3D interactiva — **Unity** (+ puente Python→Unity).

**Guion:** "Modelamos dos tipos de agentes y validamos el mismo fenómeno con tres
herramientas, para robustez."

---

## Diapositiva 6 — Onda verde: cálculo de offsets
- Fórmula: `Offsetᵢ = (Offsetᵢ₋₁ + dᵢ/v) mod C`

| Cruce | Distancia | Offset |
|---|---|---|
| García Roel | 0 m | **0.0 s** |
| Junco | 345 m | **31.1 s** |
| Garza Sada | 845 m | **10.1 s** |

**Guion:** "Con la velocidad de sincronía y el ciclo, el desfase de cada semáforo sale
de la distancia. Estos offsets son los que sincronizan el corredor."

---

## Diapositiva 7 — Diagrama espacio–tiempo
- Las **bandas verdes** alineadas dejan pasar al pelotón sin parar.
- Coordinado: la trayectoria cruza las 3 bandas en verde. Sin coordinar: se detiene.

**[Visual]** lado a lado: `Diagrama_espacio_tiempo___sentido_Garcia_Roel_a_Ga.png`
(coordinado) **vs** `Espacio_tiempo_SIN_coordinar.png`.

**Guion:** "A la izquierda, coordinado: la línea del auto viaja dentro de la banda
verde. A la derecha, sin coordinar: choca con los rojos."

---

## Diapositiva 8 — Función de evaluación J (la heurística)
- Medimos el desempeño del control con:

  **J = w₁·(colas) + w₂·(paradas) + w₃·(error de coordinación)**

- Idea de operación: *el semáforo respeta la onda verde, pero si detecta una cola
  alta puede **extender el verde** unos segundos.*
- Minimizar J = **menos colas, menos paradas, menor ruptura de la onda verde**.
- `w₁,w₂,w₃` = decisión de diseño (qué priorizar).

**[Visual]** la ecuación grande + foto/redibujo de la pizarra.

**Guion:** "Esta es la función que guía todo: queremos minimizar colas y paradas, pero
penalizando apartarse del plan de onda verde. Es exactamente la idea de la pizarra."

---

## Diapositiva 9 — Formulación MDP
- Cada intersección decide, **cada ciclo**, el **reparto** del verde mayor.

| Elemento | Definición |
|---|---|
| **Estado** | (cola del corredor, cola de la transversal) |
| **Acción** | reparto verde mayor ∈ {30,36,42,48,54} s |
| **Recompensa** | **−J** (−(w₁·colas + w₂·paradas + w₃·\|reparto−44\|)) |

- El **ciclo fijo** ancla la onda verde (el verde mayor siempre inicia en el offset).

**Guion:** "Planteamos la decisión como un MDP: el estado son las colas, la acción es
cuánto verde dar al corredor, y la recompensa es menos J."

---

## Diapositiva 10 — Tres métodos de control
1. **Tiempo fijo** — reparto fijo 44/22. Onda verde perfecta pero **no se adapta**.
2. **Heurística** — la de la pizarra: extiende el verde si la cola del corredor es
   alta, lo recorta si la transversal se satura. (También en Unity: `IntersectionController.cs`.)
3. **Q-learning** — **aprende** el reparto que minimiza J con
   `Q(s,a) ← Q(s,a) + α[r + γ·max Q(s',a') − Q(s,a)]`.

**Guion:** "Comparamos tres formas de resolver el mismo MDP: una fija, una heurística
diseñada a mano, y una que aprende sola."

---

## Diapositiva 11 — Resultados: comparación de J  ⭐
- Escenario de hora pico (seed de evaluación **fuera** del entrenamiento):

| Método | J | colas | paradas | coord (s) | Mejora vs fijo |
|---|---:|---:|---:|---:|---:|
| Tiempo fijo | 60.2 | 59.5 | 0.70 | 0.0 | — |
| **Heurística** | 36.6 | 34.1 | 0.64 | 6.3 | **+39%** |
| **Q-learning** | 29.1 | 26.5 | 0.69 | 6.4 | **+52%** |

**[Visual]** `rl_comparacion_J.png` (barras de J + componentes).

**Guion:** "El tiempo fijo es el peor. La heurística mejora J un 39%. Y Q-learning,
aprendiendo solo, llega a 52% mejor que el fijo, superando incluso a la heurística."

---

## Diapositiva 12 — Q-learning aprende
- La **curva de J por episodio baja** y se estabiliza bajo ambos baselines.
- **Robusto** en múltiples seeds no vistos.
- **Política aprendida (interpretable):** más verde al corredor cuando su cola es alta;
  más a la transversal cuando ésta se satura.

**[Visual]** `rl_curva_aprendizaje.png`.

**Guion:** "Sin decirle las reglas, el agente descubre una política sensata: dar verde
a quien tiene más cola, sin romper la coordinación."

---

## Diapositiva 13 — Coordinación vs NO coordinación
- **Ancho de banda** (sentido principal): 45 s vs 14 s → **×3.2**.
- **AgentPy:** la coordinación **reduce la espera ~75%**.
- **Unity** (corrida de ejemplo, modo coordinado ~309 s): ~162 autos, ≈31.5 veh/min,
  espera ≈5.4 s/auto, cola promedio ~2.

**[Visual]** `resultados_resumen.png` o `resultados_cola.png`.

**Guion:** "Coordinar triplica el ancho de banda y baja la espera ~75% frente a no
coordinar. Lo vemos consistente en el análisis, en AgentPy y en Unity."

---

## Diapositiva 14 — Simulación Unity (demo)
- Corredor 3D real: 3 cruces, banquetas, **cruces peatonales**, campus, teatro, parque.
- Unity ejecuta **EN VIVO la heurística** (control coordinado-actuado + **extensión de
  verde por cola alta**, la función J).
- El **MDP y Q-learning** se implementan y **entrenan en Python** (offline); existe un
  **puente** que lleva la simulación a Unity en 3D.
- Métricas en vivo (HUD) y exportación a CSV.

**[Visual]** captura de la escena (o **demo en vivo**: *M4Cruce → Construir Corredor
(Limpio) → Play*).

**Guion (exacto, sin exagerar):** "Unity corre en vivo la heurística coordinada-actuada
—la de la función J, con extensión de verde por cola alta—. El MDP y Q-learning los
implementamos y entrenamos en Python, y un puente lleva la simulación a Unity en 3D.
Conectar el control aprendido en vivo es nuestro trabajo a futuro." *(Dar Play unos seg.)*

---

## Diapositiva 15 — Discusión
- **Sí es viable** coordinar: banda verde de 45 s (68% del ciclo).
- El control **responsivo** gana bajo demanda variable (+39% heurística, +52% QL).
- **Heurística vs aprendizaje:** la heurística es simple y buena; QL la supera con
  más finura (a costa de entrenamiento).
- **Limitaciones:** Junco (T) y vueltas reducen capacidad; volúmenes **estimados** (no
  reales); el estado usa solo colas.

**Guion:** mencionar honestamente las limitaciones; da credibilidad.

---

## Diapositiva 16 — Conclusiones y trabajo futuro
- La **onda verde** es viable y mejora el flujo ~3× (banda) y ~75% (espera) vs no coordinar.
- El control que minimiza **J** mejora aún más sobre el plan fijo: **+39%** heurística,
  **+52%** Q-learning.
- El **enfoque multiagente** reproduce el fenómeno en los 3 componentes.
- **Futuro:** volúmenes reales, estado más rico (flujo reciente), **DQN**, coordinación
  distribuida por mensajes, integración en vivo Python↔Unity.

**Guion:** cerrar con la frase fuerte: "Coordinar el corredor es viable y un control que
aprende puede mejorarlo aún más." → **¿Preguntas?**

---

## Hoja de números clave (para memorizar)

| Dato | Valor |
|---|---|
| Cruces | García Roel (0 m) · Junco T (345 m) · Garza Sada (845 m) |
| Velocidad sincronía / ciclo | 40 km/h · C = 66 s |
| Offsets | 0 / 31.1 / 10.1 s |
| Ancho de banda coord. vs no | 45 s vs 14 s → **×3.2** |
| AgentPy espera | **−75%** con coordinación |
| **J** | fijo 60.2 · heurística 36.6 (**+39%**) · Q-learning 29.1 (**+52%**) |
| Q-learning | tabla de 16 estados, robusto en seeds no vistos |
| Unity (coordinado, ejemplo) | ~162 autos, 31.5 veh/min, espera 5.4 s |

## Función J (para citarla exacta)
**J = w₁·colas + w₂·paradas + w₃·error_coordinación** — minimizar J = menos colas,
menos paradas, menor ruptura de la onda verde.

## Arquitectura: qué corre dónde (para preguntas)
| Componente | Python (`Analisis/`) | Unity (`Assets/`) |
|---|:---:|:---:|
| Heurística (función J, extensión de verde) | ✅ | ✅ **en vivo** (`IntersectionController.cs`) |
| Onda verde / offsets | ✅ | ✅ (`TrafficLight.cs`) |
| MDP (formulación) | ✅ (`rl_coordinacion.py`) | ❌ |
| Q-learning (entrenamiento) | ✅ (`rl_coordinacion.py`, notebook) | ❌ (offline) |
| Puente Python→Unity (playback) | ✅ (`export_playback.py`) | ✅ (`PythonPlayback.cs`) |

> **Mensaje:** cada método tiene su "casa". Python = laboratorio de control (formular
> MDP, entrenar Q-learning). Unity = visualización 3D que corre la heurística en vivo.
> Conectar el control aprendido a Unity en vivo = **trabajo a futuro** (no afirmar que
> ya está). El RL **se entrena fuera de línea**: es lo normal, no un hueco.

## Reparto sugerido (3 personas)
- **Persona A** (problema y contexto): slides 1–5.
- **Persona B** (onda verde y métodos): slides 6–10.
- **Persona C** (resultados y cierre): slides 11–16 (+ demo Unity).

## Checklist antes de presentar
- [ ] Exportar las 4 figuras clave a la plantilla: espacio–tiempo (coord y sin coord),
      `rl_comparacion_J.png`, `rl_curva_aprendizaje.png`.
- [ ] Tener Unity abierto en la escena para la **demo en vivo** (o un video corto).
- [ ] Llenar nombres/matrículas/fecha en la portada.
- [ ] (Pendiente del reporte) re-correr Unity ~5 min en cada modo para los números
      finales de la sección "coordinación vs no coordinación".
