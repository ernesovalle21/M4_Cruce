# Reporte Técnico — Coordinación Semafórica Responsiva mediante Sistemas Multiagente
### Onda verde en el corredor Av. Luis Elizondo (Distrito Tec)

**Curso:** TC2008 – Modelación de Sistemas Multiagentes con Gráficas Computacionales
**Equipo:** [nombres y matrículas]
**Repositorio:** https://github.com/ernesovalle21/M4_Cruce
**Fecha:** [fecha]

---

## 1. Introducción

La movilidad urbana depende de la coordinación de los semáforos a lo largo de un
corredor para mantener la *progresión vehicular* (que un grupo de autos avance sin
detenerse cruce tras cruce), concepto conocido como **onda verde**. Este proyecto
diseña y evalúa una estrategia de coordinación semafórica **responsiva** para un
corredor real del Distrito Tec, **Av. Luis Elizondo**, modelando vehículos y
semáforos como **agentes** y midiendo el desempeño del corredor mediante simulación.

## 2. Problema planteado

Coordinar **tres intersecciones semaforizadas** sobre Av. Luis Elizondo:

1. **S1 – Fernando García Roel / Cantú Leal**
2. **S2 – Junco de la Vega** (cruce en T)
3. **S3 – Eugenio Garza Sada**

de manera que se **maximice la progresión** en el sentido principal y se **minimice
la congestión**, evaluando si la onda verde es viable, qué tan estable es y cómo se
comporta ante cambios de demanda.

## 3. Metodología

Se trabajó con **tres componentes complementarios** que modelan el mismo corredor:

1. **Análisis de ingeniería de tráfico (Python/matplotlib)** — cálculo de offsets,
   diagramas espacio–tiempo y ancho de banda (`Analisis/OndaVerde_Elizondo.ipynb`).
2. **Simulación multiagente (Python/AgentPy)** — vehículos y semáforos como agentes,
   con coordinación por comunicación y origen–destino
   (`Analisis/SimulacionMultiagente_Elizondo.ipynb`).
3. **Simulación 3D (Unity/C#)** — visualización interactiva del corredor con métricas
   en tiempo real y exportación a CSV (`Assets/`), más un **puente Python→Unity**.

**Parámetros del corredor:**

| Parámetro | Valor | Justificación |
|---|---|---|
| Distancias | 345 m (S1–S2), 500 m (S2–S3) | medición en Google Maps |
| Velocidad de sincronía | 40 km/h | avenida urbana con peatones/campus |
| Ciclo común (C) | 66 s | corredor principal |
| Verde / amarillo / rojo | 45 / 3 / 18 s | prioridad al corredor (split 68%) |

> **Datos de volumen:** no fueron proporcionados, por lo que se usan **estimaciones
> justificadas** con valores típicos de ingeniería de tráfico (capacidad de arteria
> urbana ~1,100–1,580 veh/h por carril; flujo óptimo < ~500; relación día↔hora pico).

## 4. Modelado multiagente

- **Agente Vehículo:** avanza por el corredor con aceleración/frenado graduales,
  mantiene distancia con el de adelante (*car-following*), respeta el semáforo y cede
  el paso al incorporarse/girar. Tiene un **destino** (origen–destino).
- **Agente Semáforo:** mantiene su fase (verde/amarillo/rojo). En el modelo coordinado
  toma su turno según el reloj común y su *offset*; en el modelo distribuido **comparte
  su estado** y **envía un mensaje** al semáforo aguas abajo avisando cuándo llegará el
  pelotón, de modo que **el offset emerge de la comunicación** entre agentes.
- **Información local por intersección:** estado de fase, longitud de cola y ocupación.
- **Coordinación:** por offsets (onda verde) y, en el modelo distribuido, por paso de
  mensajes entre cruces vecinos.

## 5. Cálculo de offsets

El offset es el desfase de arranque del verde entre cruces para que el pelotón viaje
sin detenerse:

```
t_i = (d_i - d_{i-1}) / v        Offset_i = (Offset_{i-1} + t_i) mod C
```

Con v = 40 km/h = 11.11 m/s y C = 66 s:

| Intersección | Distancia | Offset (sentido principal) |
|---|---|---|
| García Roel | 0 m | 0.0 s |
| Junco | 345 m | 31.1 s |
| Garza Sada | 845 m | 10.1 s |

## 6. Diagramas espacio–tiempo

El diagrama espacio–tiempo (figuras en `Analisis/figs/`) muestra las **bandas verdes**
de cada cruce y las **trayectorias** de los vehículos. Cuando la trayectoria cruza las
bandas verdes en los tres cruces, la onda verde está coordinada.

- `Diagrama_espacio_tiempo_*_Garcia_Roel_a_Garza_Sada.png` — sentido principal.
- `Diagrama_espacio_tiempo_*_Garza_Sada_a_Garcia_Roel.png` — sentido inverso.
- `Espacio_tiempo_SIN_coordinar.png` — baseline sin coordinación (contraste).

## 7. Resultados

**Análisis (ancho de banda y progresión):**

| Métrica | Coordinado | Sin coordinar |
|---|---|---|
| Ancho de banda (sentido principal) | 45 s (68% del ciclo) | 14 s |
| Mejora | **×3.2** | — |
| Progresión en verde (ideal) | 100% | — |

**Simulación Unity (modo coordinado, ~309 s):** 162 carros completados,
throughput **31.5 veh/min**, espera promedio **5.4 s/carro**, cola promedio 2.15
(máx 6). *(Comparación con el modo "sin coordinar": pendiente de capturar una corrida
equivalente — ver `Analisis/metrics_*_sin_coordinar.csv`.)*

**Simulación AgentPy:** la coordinación **reduce la espera promedio ~75 %** frente al
modo sin coordinar. En el modelo distribuido se intercambiaron **mensajes entre
semáforos** y la onda verde emergió de esa comunicación. En el escenario origen–destino,
~61 % de los vehículos cruzó el corredor completo y ~39 % giró en una intersección.

## 8. Discusión (respuestas a las preguntas clave)

- **¿Es posible coordinar el corredor?** Sí. Con v=40 km/h y C=66 s, los offsets
  (0/31.1/10.1 s) producen una banda verde de 45 s (68% del ciclo) en el sentido principal.
- **¿Qué tan estable es la progresión?** Muy estable en el sentido coordinado (100% en
  verde); en el sentido inverso la banda baja a ~21 s, típico de corredores asimétricos.
- **¿Qué ocurre al aumentar la demanda?** Las colas crecen; al acercarse a la saturación
  (grado de saturación > ~0.85) la onda verde empieza a romperse por colas residuales.
- **¿Qué intersección genera más congestión?** Junco (T) y los cruces con vueltas, por
  la incorporación; se observa en `metrics_resumen_*.csv` (cola por intersección).
- **¿Sensibilidad a la velocidad?** Cambiar v desplaza los offsets; el ancho de banda es
  máximo cuando la velocidad real coincide con la de sincronía.
- **¿La onda verde se mantiene o se rompe?** Se mantiene con demanda baja/media; se rompe
  con demanda alta o si la velocidad real difiere de la de diseño.
- **Limitaciones urbanas:** el cruce en T de Junco, las vueltas/incorporaciones y los
  peatones reducen la capacidad efectiva y el ancho de banda real.

## 9. Conclusiones

- La onda verde es **viable** en Av. Luis Elizondo con los parámetros propuestos y
  **mejora el flujo ~3× (ancho de banda) y ~75% (espera)** frente a no coordinar.
- El enfoque **multiagente** (vehículos y semáforos) reproduce el fenómeno de forma
  consistente en los tres componentes (análisis, AgentPy y Unity).
- La **coordinación distribuida por comunicación** logra la onda verde sin precalcular
  offsets, mostrando una vía hacia sistemas adaptativos.
- Como trabajo futuro: integrar **datos reales de volumen**, control **totalmente
  adaptativo** (p. ej. aprendizaje por refuerzo) e integración en vivo Python↔Unity.

---

### Anexos
- Código: repositorio GitHub (Unity en `Assets/`, Python en `Analisis/`).
- Figuras: `Analisis/figs/`.
- Métricas: `Analisis/metrics_*.csv`.
- Auditoría/estado: `Analisis/ESTADO_PROYECTO.md`.
