# Estado del proyecto vs requisitos del reto

Reto Integrador – Coordinación Semafórica Responsiva mediante Sistemas Multiagente (TC2008).
Corredor: **Av. Luis Elizondo** (Distrito Tec) — 3 cruces: García Roel · Junco · Garza Sada.

---

## 1. Variables obligatorias de ingeniería de tráfico

| Variable | Estado | Dónde |
|---|---|---|
| Distancia entre intersecciones | ✅ (345 m, 500 m) | `OndaVerde_Elizondo.ipynb` |
| Velocidad de sincronía (justificada) | ✅ 40 km/h | notebook §1 |
| Offset semafórico (`offset=d/v`) | ✅ 0 / 31.1 / 10.1 s | notebook §2 |
| Ciclo, fases, verde/amarillo/rojo | ✅ C=66 (45/3/18) | notebook §1 |
| Longitud de cola | ✅ medida | Unity `TrafficMetrics` + CSV |
| Tiempos de espera | ✅ medida | Unity `TrafficMetrics` + CSV |
| Ancho de banda | ✅ 45 s ida (×3.2 vs sin coord.) | notebook §4/§5b |

---

## 2. Sistema multiagente

| Requisito | Estado |
|---|---|
| Vehículos como agentes | ✅ Unity (`WaypointMover`, `CarCollision`, `YieldGate`) y Python (`VehicleAgent`) |
| Semáforos como agentes | ✅ Unity (`TrafficLight`, `IntersectionController`) y Python (`TrafficLightAgent`) |
| Info local (cola, fase, ocupación) | ✅ por intersección (`StopLineTrigger.WaitingCount`, controlador actuado) |
| Agentes que **se comunican / comparten estados** | ⚠️ PARCIAL: coordinan por offsets + reloj común + actuación local; **no hay paso de mensajes explícito** entre cruces |

---

## 3. Visualización y entregables

| Entregable | Estado |
|---|---|
| Simulación visual del corredor | ✅ Unity 3D (`CorridorBuilder`) + AgentPy (animación) |
| Vehículos animados / sincronización observable | ✅ |
| Gráficas y métricas | ✅ HUD + CSV + notebook |
| **Diagrama espacio–tiempo** (MUY IMPORTANTE) | ✅ analítico y **simulado** (`OndaVerde_Elizondo.ipynb`, `SimulacionMultiagente_Elizondo.ipynb`) |
| Integración Python ↔ Unity | ✅ puente (Python exporta `playback.json`, Unity lo reproduce) |
| **Código fuente en GitHub** | ❌ repo local sin remoto |
| **Reporte técnico** (9 secciones) | ❌ pendiente |
| **Presentación final** | ❌ pendiente |

---

## 4. Resultados actuales (Unity)

| Métrica | Coordinado | Sin coordinar |
|---|---|---|
| Tiempo simulado | 309 s | 12.6 s ❌ (corrida incompleta) |
| Carros completados | 162 | 0 ❌ |
| Throughput | 31.5 veh/min | — |
| Espera promedio | 5.4 s | — |
| Cola promedio / máx | 2.15 / 6 | — |

Análisis (notebook): banda **45 s coordinado vs 14 s sin coordinar (×3.2)**;
AgentPy: la coordinación reduce la espera ~71%.

---

## 5. Cosas MAL / inconsistentes a corregir

1. **Corrida "sin coordinar" incompleta** (12.6 s, 0 carros) → re-correr ~5 min igual que la coordinada para que la comparación en Unity sea válida.
2. **Datos reales de volumen** (mañana/mediodía/tarde): NO se usan (estimaciones). El reto los exige → conseguirlos y meterlos en `arrival_rate`/spawner.
3. **Parámetros distintos entre Unity y Python:**
   - Unity nativo: distancias en *unidades* (cruces a ±130 u), ciclo 20 s, velocidad 5 u/s.
   - Python/análisis: distancias reales (345/500 m), ciclo 66 s, 40 km/h.
   → Unificar o **justificar** (Unity = visual estilizado; Python = modelo a escala) en el reporte.
4. **CSV viejos sin etiqueta** (`metrics_resumen.csv`, `metrics_resumen_run.csv`) → borrar para no confundir.
5. **Relaciones origen–destino**: no se modelan explícitamente (los carros solo cruzan) → mencionar como simplificación o agregar rutas/giros con probabilidad.

---

## 5b. Reconciliación de escalas Unity ↔ Python (resuelve la inconsistencia #3)

Los dos artefactos usan el **mismo método** de coordinación (`offset = distancia / velocidad`),
pero a **escalas distintas a propósito**:

| | Modelo de ingeniería (Python) | Visualización (Unity) |
|---|---|---|
| Distancias | reales: 345 m, 500 m | estilizadas (unidades de escena) |
| Velocidad | 40 km/h (justificada) | velocidad de juego (estética) |
| Ciclo | C = 66 s (45/3/18) | ciclo comprimido para demo |
| Propósito | cálculo y validación de offsets, diagramas, ancho de banda | demo 3D observable |

**Justificación:** el cuaderno de Python es el **modelo a escala real** (de él salen offsets,
diagramas espacio–tiempo y métricas de ingeniería); Unity es la **representación visual**
en tiempo comprimido para mostrar el comportamiento. Ambos aplican la misma fórmula de
offsets, por lo que son **consistentes en método** aunque difieran en números absolutos.
(Opción futura: poner Unity exactamente a escala real — cambia el ritmo de la demo.)

Estado de las inconsistencias:
- #2 datos reales → **estimaciones justificadas** con valores típicos de tráfico (ver notebook §6).
- #3 escalas → **documentado/justificado** (este apartado).
- #4 CSV viejos → **eliminados**.
- #1 corrida sin-coordinar incompleta → **pendiente de re-correr** (operativo en Unity).

## 6. Lo que FALTA (en orden sugerido)

1. **Re-correr "sin coordinar"** ~5 min y volver a generar las gráficas (celda 6b).
2. **Conseguir/meter datos reales** de volumen por periodo.
3. **Exportar las figuras** del notebook (offsets, espacio–tiempo, comparación) para el reporte.
4. (Opcional) **Comunicación explícita entre semáforos** (paso de estados entre cruces) para cubrir 100% el punto multiagente.
5. **Reporte técnico** (Intro, Problema, Metodología, Modelado multiagente, Cálculo de offsets, Diagramas espacio–tiempo, Resultados, Discusión, Conclusiones).
6. **Subir a GitHub** (entregable de código).
7. **Presentación**.

---

## 7. Cobertura por rúbrica (estimada)

| Criterio | Peso | Cobertura |
|---|---|---|
| Modelado multiagente | 20% | ✅ Alta |
| Coordinación semafórica | 20% | ✅ Alta (mejorable: comunicación explícita) |
| Cálculo y justificación de offsets | 15% | ✅ Completo |
| Diagramas espacio–tiempo | 15% | ✅ Completo |
| Simulación y resultados | 15% | ⚠️ Falta re-correr sin-coordinar + datos reales |
| Análisis crítico y conclusiones | 15% | ⚠️ Material listo, **falta redactar** |
