# M4_Cruce — Coordinación Semafórica Responsiva (Onda Verde) · Av. Luis Elizondo

Reto Integrador **TC2008 – Sistemas Multiagente**. Simulación y evaluación de una
**onda verde** en un corredor de 3 cruces del Distrito Tec:

- **S1 – Fernando García Roel / Cantú Leal**
- **S2 – Junco de la Vega** (cruce en T)
- **S3 – Eugenio Garza Sada**

El proyecto tiene **tres componentes** que modelan el mismo corredor:
1. **Simulación 3D en Unity** (C#) — visual, interactiva.
2. **Simulación multiagente en Python (AgentPy)** — vehículos y semáforos como agentes.
3. **Análisis de ingeniería de tráfico (Python/matplotlib)** — offsets, diagramas
   espacio–tiempo, ancho de banda y métricas.

---

## Estructura

```
Assets/Scripts/            # Simulación Unity (C#)
  WaypointMover.cs           - vehículo: movimiento, física, car-following, ceder paso
  TrafficLight.cs            - semáforo (fase por reloj+offset / control externo)
  IntersectionController.cs  - control coordinado-actuado por intersección
  StopLineTrigger.cs         - detección/parada en línea de alto (+ cola)
  CarCollision.cs, YieldGate.cs, CarSpawner.cs
  TrafficMetrics.cs          - métricas (HUD + export CSV)
  PythonPlayback.cs          - puente: reproduce la simulación de Python en 3D
  Editor/CorridorBuilder.cs  - arma el corredor (menús M4Cruce)
  Editor/PlaybackBuilder.cs  - arma la escena del puente Python→Unity

Analisis/                  # Python
  OndaVerde_Elizondo.ipynb            - offsets + diagramas espacio-tiempo + ancho de banda + resultados
  SimulacionMultiagente_Elizondo.ipynb- simulación AgentPy (agentes, comunicación, origen-destino)
  export_playback.py                  - genera playback.json para el puente Unity
  build_notebook.py / build_sim_notebook.py - regeneran los notebooks
  ESTADO_PROYECTO.md                  - auditoría vs rúbrica
  figs/                               - figuras exportadas (se generan al correr)
```

---

## Cómo correr

### Unity (menú **M4Cruce**)
- **Construir Corredor (Limpio)** → Play: corredor coordinado (onda verde actuada) + HUD de métricas.
- **Construir Corredor (SIN coordinar)** → Play: baseline de ciclo fijo (para comparar).
- **Reproducir Simulación Python (Bridge)** → Play: Unity dibuja la simulación de Python.

Las métricas se exportan a `Analisis/metrics_*_{coordinado|sin_coordinar}.csv` (cada 30 s y al salir).

### Python (Colab/Jupyter)
- Abrir `Analisis/OndaVerde_Elizondo.ipynb` y `Analisis/SimulacionMultiagente_Elizondo.ipynb` y ejecutar.
- Puente: `python3 Analisis/export_playback.py` genera `playback.json`.

---

## Parámetros del corredor (modelo de ingeniería)

| Parámetro | Valor | Justificación |
|---|---|---|
| Distancias | 345 m, 500 m | medición en Google Maps |
| Velocidad de sincronía | 40 km/h | avenida urbana del Distrito Tec |
| Ciclo / verde / amarillo / rojo | 66 / 45 / 3 / 18 s | corredor principal con prioridad |
| Offsets (García Roel→Garza Sada) | 0 / 31.1 / 10.1 s | `offset = d / v` |

> **Datos de volumen:** no fueron proporcionados → se usan **estimaciones justificadas**
> con valores típicos de tráfico (capacidad de arteria urbana, hora pico). La rúbrica
> acepta una aproximación coherente y justificada.

---

## Resultados clave

- **Ancho de banda:** 45 s coordinado vs 14 s sin coordinar → **×3.2**.
- **Progresión:** 100 % en verde (sentido principal, ideal).
- **Unity (coordinado):** espera ~5.4 s/carro, throughput ~31 veh/min, cola prom 2.15.
- **AgentPy:** la coordinación **reduce la espera ~75 %** vs sin coordinar.
- **Coordinación distribuida:** la onda verde **emerge de la comunicación** entre semáforos.
- **Origen–destino:** ~61 % cruza el corredor, ~39 % gira en una intersección.

---

## Estado vs rúbrica

| Criterio | Peso | Estado |
|---|---|---|
| Modelado multiagente | 20% | ✅ |
| Coordinación semafórica | 20% | ✅ |
| Cálculo y justificación de offsets | 15% | ✅ |
| Diagramas espacio–tiempo | 15% | ✅ |
| Simulación y resultados | 15% | ⚠️ falta re-correr "sin coordinar" ~5 min |
| Análisis crítico y conclusiones | 15% | ⚠️ material y figuras listos, falta redactar |

---

## Pendientes (no técnicos)

1. **Re-correr "SIN coordinar"** ~5 min en Unity para la comparación de métricas.

