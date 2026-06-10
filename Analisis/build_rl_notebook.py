"""
Genera Aprendizaje_Coordinacion_Elizondo.ipynb — coordinación semafórica del
corredor Av. Luis Elizondo como MDP, resuelta con tres métodos que minimizan la
misma función de evaluación heurística J: tiempo fijo, heurística (la de la
pizarra) y Q-learning. Usa el módulo validado Analisis/rl_coordinacion.py.

Ejecutar:  python3 build_rl_notebook.py
"""
import json
import os


def md(*lines):
    return {"cell_type": "markdown", "metadata": {}, "source": "\n".join(lines)}


def code(src):
    return {"cell_type": "code", "metadata": {}, "execution_count": None,
            "outputs": [], "source": src.strip("\n")}


cells = []

cells.append(md(
    "# Coordinación semafórica responsiva como MDP — Q-learning",
    "## Corredor Av. Luis Elizondo (Distrito Tec) · TC2008",
    "",
    "Este cuaderno plantea la operación de los semáforos del corredor como un",
    "**problema de decisión secuencial (MDP)** y compara **tres métodos** que",
    "minimizan la **misma función de evaluación heurística**:",
    "",
    "$$ J = w_1\\,(\\text{colas}) + w_2\\,(\\text{paradas}) + w_3\\,(\\text{error de coordinación}) $$",
    "",
    "1. **Tiempo fijo** — reparto fijo del verde (plan de onda verde, sin adaptarse).",
    "2. **Heurística** — *respeta la onda verde, pero si detecta una cola alta extiende",
    "   el verde unos segundos* (y si la transversal se satura, le da más reparto).",
    "3. **Q-learning** — *aprende* el reparto que minimiza $J$ usando $-J$ como recompensa.",
    "",
    "> El código del entorno y de los métodos vive en `rl_coordinacion.py` (mismo",
    "> directorio); aquí se ejecuta y se grafican los resultados.",
))

cells.append(md(
    "## 1. Caso de estudio y supuestos",
    "",
    "- **3 intersecciones** sobre Av. Luis Elizondo: García Roel (0 m), Junco de la",
    "  Vega (345 m) y Garza Sada (845 m).",
    "- **Velocidad de sincronía** 40 km/h (11.11 m/s) y **ciclo común** $C=66$ s.",
    "- La **onda verde** queda anclada por los *offsets* (0 / 31.1 / 10.1 s): el verde",
    "  mayor siempre **inicia en el instante coordinado**, así el pelotón llega en verde.",
    "- **Datos de volumen:** no fueron proporcionados → se usan **estimaciones",
    "  justificadas**. La demanda del corredor y la transversal se modelan en",
    "  **contrafase** (hora pico): cuando una sube, la otra baja. Por eso un reparto",
    "  fijo desperdicia verde y un control adaptativo puede mejorar.",
))

cells.append(md(
    "## 2. Formulación MDP",
    "",
    "El control coordinado-actuado mantiene el **ciclo fijo** (ancla la onda verde) y",
    "decide, **cada ciclo y en cada intersección**, el **reparto** del verde mayor.",
    "",
    "| Elemento | Definición |",
    "|---|---|",
    "| **Estado** $s$ | (cola del corredor, cola de la transversal) discretizadas |",
    "| **Acción** $a$ | reparto del verde mayor $\\in\\{30,36,42,48,54\\}$ s (el resto es transversal) |",
    "| **Transición** | dinámica de colas (llegadas Poisson, descarga a flujo de saturación, pelotón que viaja $d/v$ al cruce de aguas abajo) |",
    "| **Recompensa** $r$ | $-(\\text{costo del ciclo}) = -(w_1\\,\\text{colas}+w_2\\,\\text{paradas}+w_3\\,|\\text{reparto}-44|)$ |",
    "",
    "- **Heurística** = una política *diseñada a mano* que aproxima $\\min J$.",
    "- **Q-learning** = *aprende* la política $\\arg\\max_a Q(s,a)$, con",
    "  $Q(s,a)\\leftarrow Q(s,a)+\\alpha\\,[\\,r+\\gamma\\max_{a'}Q(s',a')-Q(s,a)\\,]$.",
    "- **Error de coordinación** = cuánto se aparta el reparto del plan ($|\\text{reparto}-44|$):",
    "  penaliza romper el diseño de onda verde; el tiempo fijo lo tiene en 0.",
))

cells.append(code(
    "import numpy as np\n"
    "import matplotlib.pyplot as plt\n"
    "from rl_coordinacion import (OFFSETS, TAU, C, SPLITS, W1, W2, W3,\n"
    "                             run_static, decide_fixed, decide_heuristic,\n"
    "                             train_qlearning, eval_qlearning, SPLITS as SP)\n"
    "\n"
    "print('Offsets onda verde (s):', [round(o, 1) for o in OFFSETS], ' | ciclo C =', C, 's')\n"
    "print('Retardos de pelotón TAU (s):', TAU)\n"
    "print('Repartos posibles del verde mayor (s):', SPLITS)\n"
    "print(f'Función de evaluación:  J = {W1}*colas + {W2}*paradas + {W3}*error_coordinación')"
))

cells.append(md(
    "## 3. Baselines: tiempo fijo y heurística",
    "",
    "Se evalúan en un escenario de hora pico (mismo seed para comparar de forma justa).",
))

cells.append(code(
    "SEED = 7777   # escenario de evaluación (fuera del rango de entrenamiento de QL)\n"
    "m_fix = run_static(decide_fixed, seed=SEED)\n"
    "m_heu = run_static(decide_heuristic, seed=SEED)\n"
    "print('Tiempo fijo:', {k: round(v, 3) for k, v in m_fix.items()})\n"
    "print('Heurística :', {k: round(v, 3) for k, v in m_heu.items()})"
))

cells.append(md(
    "## 4. Q-learning: entrenamiento",
    "",
    "Se entrena el agente (tabla $Q$ compartida entre las 3 intersecciones) sobre",
    "muchos episodios de hora pico; la exploración $\\varepsilon$ decae con el tiempo.",
    "Tarda ~10–15 s.",
))

cells.append(code(
    "agent, curve = train_qlearning()\n"
    "m_ql = eval_qlearning(agent, seed=SEED)\n"
    "print('Q-learning :', {k: round(v, 3) for k, v in m_ql.items()})\n"
    "print('Estados aprendidos en la Q-table:', len(agent.Q))"
))

cells.append(md(
    "## 5. Resultados: comparación de los tres métodos",
))

cells.append(code(
    "rows = [('Tiempo fijo', m_fix), ('Heurística', m_heu), ('Q-learning', m_ql)]\n"
    "print(f\"{'Método':<14}{'J':>9}{'colas':>9}{'paradas':>10}{'coord(s)':>10}\")\n"
    "print('-' * 52)\n"
    "for name, m in rows:\n"
    "    print(f\"{name:<14}{m['J']:>9.2f}{m['colas']:>9.2f}{m['paradas']:>10.3f}{m['error_coord']:>10.2f}\")\n"
    "mej_h = 100 * (m_fix['J'] - m_heu['J']) / m_fix['J']\n"
    "mej_q = 100 * (m_fix['J'] - m_ql['J']) / m_fix['J']\n"
    "print(f\"\\nMejora de J vs tiempo fijo:  heurística {mej_h:+.1f}%   Q-learning {mej_q:+.1f}%\")\n"
    "\n"
    "labels = [r[0] for r in rows]; x = np.arange(3); cols = ['#999', '#c98a00', '#2a7a2a']\n"
    "fig, ax = plt.subplots(1, 2, figsize=(12, 4))\n"
    "ax[0].bar(x, [r[1]['J'] for r in rows], color=cols)\n"
    "ax[0].set_xticks(x); ax[0].set_xticklabels(labels); ax[0].set_ylabel('J')\n"
    "ax[0].set_title('Función de evaluación J (menor es mejor)')\n"
    "for comp_i, c in enumerate(['colas', 'paradas', 'error_coord']):\n"
    "    ax[1].bar(x + (comp_i - 1) * 0.25, [r[1][c] for r in rows], 0.25, label=c)\n"
    "ax[1].set_xticks(x); ax[1].set_xticklabels(labels); ax[1].legend()\n"
    "ax[1].set_title('Componentes de J')\n"
    "plt.tight_layout(); plt.show()"
))

cells.append(md(
    "## 6. Curva de aprendizaje de Q-learning",
    "",
    "$J$ por episodio durante el entrenamiento; debe **bajar** y estabilizarse por",
    "debajo del tiempo fijo y de la heurística.",
))

cells.append(code(
    "plt.figure(figsize=(8, 4))\n"
    "plt.plot(curve, lw=1.6, color='#2a7a2a', label='Q-learning (entrenamiento)')\n"
    "plt.axhline(m_fix['J'], ls='--', c='#999', label='Tiempo fijo')\n"
    "plt.axhline(m_heu['J'], ls='--', c='#c98a00', label='Heurística')\n"
    "plt.xlabel('Episodio'); plt.ylabel('J'); plt.legend()\n"
    "plt.title('Q-learning aprende a minimizar J'); plt.grid(True, ls='--', alpha=0.4)\n"
    "plt.tight_layout(); plt.show()"
))

cells.append(md(
    "## 7. Política aprendida (interpretación)",
    "",
    "Para cada estado (cola del corredor, cola de la transversal), el reparto de verde",
    "mayor que el agente eligió. Se espera: **más verde al corredor** cuando su cola es",
    "alta y la transversal baja; **menos** (más a la transversal) en el caso contrario.",
))

cells.append(code(
    "print('(cola_corredor, cola_transversal) -> verde mayor (s)')\n"
    "for s in sorted(agent.Q):\n"
    "    print('   ', s, '->', SP[int(np.argmax(agent.Q[s]))])"
))

cells.append(md(
    "## 8. Discusión",
    "",
    "- El **tiempo fijo** no se adapta: cuando la transversal entra en hora pico, su",
    "  reparto fijo se satura y las colas crecen.",
    "- La **heurística** (la de la pizarra) ya mejora bastante $J$ extendiendo/recortando",
    "  el verde según la cola, **respetando** la onda verde (ciclo y offset anclados).",
    "- **Q-learning** descubre, sin que se le digan las reglas, un reparto **mejor** que",
    "  la heurística: misma desviación de coordinación pero **menos colas**.",
    "- Los tres métodos comparten la **misma** $J$, por eso la comparación es justa; los",
    "  pesos $w_1,w_2,w_3$ son una decisión de diseño (priorizar colas, paradas o no",
    "  romper la coordinación).",
))

cells.append(md(
    "## 9. Conclusiones y trabajo futuro",
    "",
    "- La coordinación **responsiva** (heurística o aprendida) supera al plan fijo en el",
    "  corredor Av. Luis Elizondo bajo demanda variable de hora pico.",
    "- **Q-learning** es viable incluso con una tabla pequeña y mejora a la heurística,",
    "  mostrando el valor del control **adaptativo**.",
    "- **Trabajo futuro:** calibrar con **volúmenes reales**, estado más rico (medir flujo",
    "  reciente, no solo cola), aproximación de $Q$ con función (DQN) y coordinación",
    "  **multiagente** entre cruces por paso de mensajes.",
))

nb = {
    "cells": cells,
    "metadata": {
        "kernelspec": {"display_name": "Python 3", "language": "python", "name": "python3"},
        "language_info": {"name": "python", "version": "3"},
    },
    "nbformat": 4, "nbformat_minor": 5,
}

out = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "Aprendizaje_Coordinacion_Elizondo.ipynb")
with open(out, "w", encoding="utf-8") as f:
    json.dump(nb, f, ensure_ascii=False, indent=1)
print("Notebook generado:", out, "con", len(cells), "celdas")
