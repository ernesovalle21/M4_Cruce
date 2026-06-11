# Reporte Técnico — Coordinación Semafórica Responsiva mediante Sistemas Multiagente
### Onda verde en el corredor Av. Luis Elizondo (Distrito Tec)

**Curso:** TC2008 – Modelación de Sistemas Multiagentes con Gráficas Computacionales
**Equipo:** [nombres y matrículas]
**Repositorio:** https://github.com/ernesovalle21/M4_Cruce
**Fecha:** [fecha]

---

## 1. Introducción

### 1.1 Planteamiento del problema

La movilidad urbana en un corredor depende de la **coordinación de los semáforos**
para mantener la *progresión vehicular* (que un grupo de autos avance sin detenerse
cruce tras cruce): la **onda verde**. Sin coordinación, cada intersección opera
aislada, los pelotones se detienen en cada semáforo y crecen las colas, las paradas
y el tiempo de viaje.

El problema es **coordinar tres intersecciones semaforizadas** sobre Av. Luis
Elizondo de modo que se **maximice la progresión** en el sentido principal y se
**minimice la congestión**, y además decidir cómo debe **responder** el control
cuando la demanda cambia (hora pico) sin romper la onda verde. Se modela el sistema
con **agentes** (vehículos y semáforos) y se evalúa por **simulación**.

### 1.2 Objetivos

**Objetivo general:** diseñar y evaluar una estrategia de coordinación semafórica
**responsiva** para el corredor Av. Luis Elizondo, modelada con sistemas multiagente,
y comparar su desempeño contra la operación sin coordinar.

**Objetivos específicos:**
1. Calcular los **offsets** de la onda verde a partir de distancias reales y velocidad
   de sincronía, y verificarlos con **diagramas espacio–tiempo**.
2. Modelar vehículos y semáforos como **agentes** (en Python/AgentPy y en Unity).
3. Definir una **función de evaluación** `J` del desempeño (colas, paradas, error de
   coordinación) y usarla para comparar métodos de control.
4. Implementar y comparar **tres métodos** de control que minimizan `J`: tiempo fijo,
   **heurística** coordinada-actuada y **Q-learning**.
5. **Cuantificar la mejora** de la coordinación responsiva frente al plan fijo y
   frente a la operación sin coordinar (colas, paradas, espera, throughput).

### 1.3 Cruces seleccionados (caso de estudio)

Tres intersecciones reales sobre Av. Luis Elizondo (Distrito Tec, Monterrey):

| ID | Calle transversal | Distancia acumulada | Tipo |
|----|------------------|--------------------:|------|
| S1 | Fernando García Roel | 0 m (inicio) | cruz, doble sentido |
| S2 | Junco de la Vega | 345 m | **T**, doble sentido con vuelta |
| S3 | Eugenio Garza Sada | 845 m | cruz |

La escena 3D (Unity) reproduce el corredor con sus banquetas, cruces peatonales,
campus, teatro y parque para dar contexto urbano realista.

---

## 2. Métodos seleccionados

### 2.0 Componentes de modelado

Se trabajó con **tres componentes complementarios** del mismo corredor:

1. **Análisis de ingeniería de tráfico (Python/matplotlib)** — offsets, diagramas
   espacio–tiempo y ancho de banda (`Analisis/OndaVerde_Elizondo.ipynb`).
2. **Simulación multiagente (Python/AgentPy)** — vehículos y semáforos como agentes,
   con coordinación por comunicación y origen–destino
   (`Analisis/SimulacionMultiagente_Elizondo.ipynb`).
3. **Simulación 3D (Unity/C#)** — visualización interactiva con métricas en tiempo
   real y exportación a CSV (`Assets/`), más un **puente Python→Unity**.

**Parámetros del corredor**

| Parámetro | Valor | Justificación |
|---|---|---|
| Distancias | 345 m (S1–S2), 500 m (S2–S3) | medición en Google Maps |
| Velocidad de sincronía | 40 km/h (11.11 m/s) | avenida urbana con peatones/campus |
| Ciclo común (C) | 66 s | corredor principal |
| Verde/amarillo/rojo | 45 / 3 / 18 s | prioridad al corredor (split ≈ 68%) |

> **Datos de volumen:** no fueron proporcionados, por lo que se usan **estimaciones
> justificadas** con valores típicos de ingeniería de tráfico (capacidad de arteria
> urbana ~1,100–1,580 veh/h por carril; flujo de saturación ~1.5 veh/s en 3 carriles).

### 2.1 Función de evaluación heurística `J`

El desempeño del control se mide con una **función de evaluación** que pondera tres
costos (todos a minimizar):

$$ J = w_1\,(\text{colas}) + w_2\,(\text{paradas}) + w_3\,(\text{error de coordinación}) $$

- **colas:** longitud media de cola en las aproximaciones.
- **paradas:** fracción de vehículos que se detienen (rompe la progresión).
- **error de coordinación:** cuánto se aparta el control del **plan de onda verde**
  (en el modelo, la desviación del reparto de verde respecto al diseñado, `|reparto−44|`).

La idea operativa (coordinada-actuada) es: **el semáforo respeta la onda verde, pero
si detecta una cola alta puede extender el verde algunos segundos.** Minimizar `J`
busca *menos colas, menos paradas y menor ruptura de la onda verde*. Los pesos
`w₁, w₂, w₃` son una **decisión de diseño** (qué priorizar).

### 2.2 Cálculo de offsets (onda verde)

El offset es el desfase de arranque del verde entre cruces para que el pelotón viaje
sin detenerse:

```
t_i = (d_i − d_{i−1}) / v        Offset_i = (Offset_{i−1} + t_i) mod C
```

Con v = 11.11 m/s y C = 66 s:

| Intersección | Distancia | Offset |
|---|---|---|
| García Roel | 0 m | 0.0 s |
| Junco | 345 m | 31.1 s |
| Garza Sada | 845 m | 10.1 s |

### 2.3 Modelado multiagente

- **Agente Vehículo:** avanza con aceleración/frenado graduales, mantiene distancia
  con el de adelante (*car-following*), respeta el semáforo y cede el paso al
  girar/incorporarse. Tiene un **destino** (origen–destino).
- **Agente Semáforo:** mantiene su fase; en el modo coordinado toma su turno según el
  reloj común y su *offset*; en el modo distribuido **comparte su estado** y **envía
  un mensaje** al cruce de aguas abajo avisando la llegada del pelotón, de modo que
  el offset **emerge de la comunicación**.

### 2.4 Métodos de control comparados (MDP, heurística, Q-learning)

La operación de cada intersección se plantea como un **proceso de decisión de Markov
(MDP)** con ciclo fijo (que ancla la onda verde) donde se decide el **reparto** del
verde mayor cada ciclo:

| Elemento | Definición |
|---|---|
| **Estado** `s` | (cola del corredor, cola de la transversal), discretizadas |
| **Acción** `a` | reparto del verde mayor ∈ {30, 36, 42, 48, 54} s (resto: transversal) |
| **Transición** | dinámica de colas: llegadas Poisson, descarga a flujo de saturación, pelotón que viaja d/v al cruce siguiente |
| **Recompensa** `r` | `−(costo del ciclo) = −(w₁·colas + w₂·paradas + w₃·|reparto−44|) = −J` |

Sobre este MDP se comparan **tres métodos**:

1. **Tiempo fijo** — reparto fijo (44/22). Onda verde perfecta pero **no se adapta**.
2. **Heurística** — la regla de la pizarra: respeta la onda verde, **extiende** el
   verde mayor si la cola del corredor es alta y lo **recorta** si la transversal se
   satura. Es una política diseñada a mano que aproxima `min J`.
3. **Q-learning** — *aprende* la política `argmaxₐ Q(s,a)` con
   `Q(s,a) ← Q(s,a) + α[r + γ·maxₐ' Q(s',a') − Q(s,a)]`, usando `−J` como recompensa.

Implementación: `Analisis/rl_coordinacion.py` (modelo real, embebido en el notebook `Analisis/OndaVerde_Elizondo.ipynb`, sección 7).
En Unity, la heurística está integrada en `IntersectionController.cs` (extensión de
verde por cola alta) para la demo en vivo.

---

## 3. Resultados y análisis

### 3.1 Diagramas espacio–tiempo (onda verde)

Los diagramas (en `Analisis/figs/`) muestran las **bandas verdes** de cada cruce y
las **trayectorias** de los vehículos. Cuando la trayectoria cruza las bandas verdes
en los tres cruces, la onda verde está coordinada.

- `Diagrama_espacio_tiempo_*_Garcia_Roel_a_Garza_Sada.png` — sentido principal.
- `Diagrama_espacio_tiempo_*_Garza_Sada_a_Garcia_Roel.png` — sentido inverso.
- `Espacio_tiempo_SIN_coordinar.png` — baseline sin coordinación (contraste).

### 3.2 Comparativa: coordinación vs sin coordinación

**Análisis (ancho de banda y progresión):**

| Métrica | Coordinado | Sin coordinar |
|---|---|---|
| Ancho de banda (sentido principal) | 45 s (68% del ciclo) | 14 s |
| Mejora | **×3.2** | — |
| Progresión en verde (ideal) | 100% | — |

**Simulación AgentPy:** la coordinación **reduce la espera promedio ~75%** frente al
modo sin coordinar. En el modelo distribuido se intercambiaron **mensajes entre
semáforos** y la onda verde emergió de esa comunicación. En el escenario
origen–destino, ~61% de los vehículos cruzó el corredor completo y ~39% giró.

**Simulación Unity — corridas de IGUAL duración (~180 s), coordinado vs sin coordinar:**

| Métrica (Unity) | Coordinado | Sin coordinar | Mejora |
|---|---:|---:|---:|
| Autos completados | 69 | 60 | **+15 %** |
| Throughput (veh/min) | 22.99 | 19.97 | **+15 %** |
| Espera promedio (s/auto) | 5.40 | 6.89 | **−22 %** |
| Cola promedio | 1.71 | 2.13 | **−20 %** |
| Cola máxima | 7 | 7 | = |

La coordinación (onda verde actuada) mejora throughput, espera y cola frente al ciclo
fijo sin coordinar, de forma consistente con el análisis de ancho de banda y AgentPy.
Datos en `Analisis/metrics_*_coordinado.csv` y `metrics_*_sin_coordinar.csv`.

### 3.3 Comparativa de métodos de control: tiempo fijo vs heurística vs Q-learning

Evaluación sobre un escenario de hora pico (demanda corredor/transversal en
contrafase; seed de evaluación **fuera** del entrenamiento de Q-learning):

| Método | J | colas | paradas | error coord. (s) | Mejora de J vs fijo |
|---|---:|---:|---:|---:|---:|
| Tiempo fijo | 60.2 | 59.5 | 0.698 | 0.0 | — |
| **Heurística** (pizarra) | 36.6 | 34.1 | 0.644 | 6.3 | **+39%** |
| **Q-learning** | 29.1 | 26.5 | 0.688 | 6.4 | **+52%** |

(Figuras: `Analisis/figs/rl_comparacion_J.png` y `rl_curva_aprendizaje.png`.)

- El **tiempo fijo** acumula grandes colas cuando la transversal entra en hora pico.
- La **heurística** mejora `J` ~39% adaptando el reparto, respetando la onda verde.
- **Q-learning** descubre —sin que se le digan las reglas— un reparto **mejor que la
  heurística** (~52% vs fijo): misma desviación de coordinación pero **menos colas**.
- La **curva de aprendizaje** desciende y se estabiliza por debajo de ambos baselines;
  el resultado es **robusto** en múltiples seeds no vistos.

---

## 4. Discusión de resultados

- **¿Es posible coordinar el corredor?** Sí. Con v = 40 km/h y C = 66 s los offsets
  (0 / 31.1 / 10.1 s) producen una banda verde de 45 s (68% del ciclo) en el sentido
  principal.
- **¿Conviene un control responsivo?** Sí, bajo demanda variable: la heurística y
  Q-learning superan al plan fijo (≈39% y ≈52% en `J`). La clave es **adaptar el
  reparto sin romper la onda verde** (ciclo y offset anclados).
- **¿Heurística o aprendizaje?** La heurística es simple y efectiva; Q-learning la
  supera porque ajusta el reparto con más finura según el estado de las colas. El
  costo de Q-learning es el entrenamiento previo.
- **¿Qué ocurre al aumentar la demanda?** Las colas crecen; cerca de la saturación la
  onda verde empieza a romperse por colas residuales (se observa en las métricas).
- **¿Qué intersección genera más congestión?** Junco (T) y los cruces con vueltas, por
  la incorporación; visible en `metrics_resumen_*.csv` (cola por intersección).
- **Limitaciones:** el cruce en T, las vueltas/incorporaciones y los peatones reducen
  la capacidad efectiva; los volúmenes son estimados (no reales); el estado del MDP
  usa solo colas (no flujo reciente).

---

## 5. Conclusiones y trabajo a futuro

**Conclusiones**

- La **onda verde** es viable en Av. Luis Elizondo y mejora el flujo ~3× en ancho de
  banda y ~75% en espera frente a **no coordinar**.
- El control **responsivo** que minimiza `J` mejora aún más sobre el plan fijo:
  **+39%** la heurística y **+52%** Q-learning.
- El enfoque **multiagente** (vehículos y semáforos) reproduce el fenómeno de forma
  consistente en los tres componentes (análisis, AgentPy y Unity).
- Q-learning demuestra que el control **adaptativo aprendido** es viable incluso con
  una tabla pequeña, superando a la heurística diseñada a mano.

**Trabajo a futuro**

- Calibrar con **volúmenes reales** (mañana/mediodía/tarde).
- Estado más rico (medir **flujo reciente**, no solo cola) y **aproximación de Q con
  función** (DQN) para más intersecciones.
- **Coordinación multiagente distribuida** entre cruces por paso de mensajes.
- Integración en vivo **Python↔Unity** (control aprendido manejando la simulación 3D).

---

### Anexos
- Código: repositorio GitHub (Unity en `Assets/`, Python en `Analisis/`).
- Métodos de control / RL: `Analisis/rl_coordinacion.py` (embebido en `Analisis/OndaVerde_Elizondo.ipynb`, §7).
- Figuras: `Analisis/figs/` (espacio–tiempo, comparación J, curva de aprendizaje).
- Métricas Unity: `Analisis/metrics_*.csv`.
- Auditoría/estado: `Analisis/ESTADO_PROYECTO.md`.
