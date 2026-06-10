# Análisis de coordinación semafórica — Av. Luis Elizondo

Parte **analítica/cuantitativa** del Reto Integrador (TC2008 – Sistemas Multiagente).
Complementa la simulación multiagente en Unity (`Assets/`).

## Contenido

- **`OndaVerde_Elizondo.ipynb`** — Notebook (Colab/Jupyter) que cubre los entregables
  de ingeniería de tráfico del reto:
  - Distancias entre los 3 cruces (García Roel, Junco, Garza Sada).
  - Velocidad de sincronía justificada (40 km/h).
  - Ciclo común y splits (C = 66 s; verde 45 / amarillo 3 / rojo 18).
  - **Cálculo de offsets** `offset_i = (offset_{i-1} + d/v) mod C` (ambos sentidos).
  - **Diagrama espacio–tiempo** con bandas verdes y trayectorias (entregable clave).
  - **Ancho de banda** y **evaluación de progresión** (¿llega en verde?).
  - Escenarios de demanda mañana/mediodía/tarde (estimados, sustituir por datos reales).
  - Conclusiones que responden las preguntas clave del reto.
- **`build_notebook.py`** — Script que regenera el `.ipynb` (reproducible).

## Cómo usar

Opción A — Colab/Jupyter: subir `OndaVerde_Elizondo.ipynb` y ejecutar todas las celdas.

Opción B — regenerar el notebook:
```bash
cd Analisis
python3 build_notebook.py
```

## Resultados (con los parámetros actuales)

| Métrica | Valor |
|---|---|
| Offsets (García Roel→Garza Sada) | 0 / 31.1 / 10.1 s |
| Ancho de banda sentido principal | 45 s (68% del ciclo) |
| Ancho de banda sentido inverso | 21 s (32% del ciclo) |
| Progresión en verde (ideal) | 100% ambos sentidos |

## Pendiente

- Sustituir volúmenes estimados por los **datos reales** (mañana/mediodía/tarde).
- Conectar con métricas de la simulación Unity (colas, tiempo de espera) — siguiente paso.
