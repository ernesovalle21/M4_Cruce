"""
Genera SimulacionMultiagente_Elizondo.ipynb — simulación multiagente (AgentPy)
del corredor Av. Luis Elizondo: vehículos y semáforos como agentes, onda verde,
métricas, diagrama espacio-tiempo simulado y comparación coordinado vs sin coordinar.

Ejecutar:  python3 build_sim_notebook.py
"""
import json, os

def md(*lines):
    return {"cell_type": "markdown", "metadata": {}, "source": "\n".join(lines)}

def code(src):
    return {"cell_type": "code", "metadata": {}, "execution_count": None,
            "outputs": [], "source": src.strip("\n")}

# Modelo AgentPy (validado) — se reutiliza como string en una celda
MODELO = r'''
import agentpy as ap
import numpy as np

class TrafficLightAgent(ap.Agent):
    """Semáforo como agente: su fase sale del reloj común + offset (onda verde)."""
    def setup(self):
        self.pos_x = 0.0
        self.offset = 0.0
        self.state = 'red'
    def update(self, t):
        C, g, y = self.p.C, self.p.green, self.p.yellow
        ph = (t - self.offset) % C
        self.state = 'green' if ph < g else ('yellow' if ph < g + y else 'red')

class VehicleAgent(ap.Agent):
    """Vehículo como agente: avanza por el corredor, respeta semáforos y al de adelante."""
    def setup(self):
        self.x = self.p.x_entry
        self.t0 = self.model.t
        self.vid = self.model.next_id
        self.model.next_id += 1
        self.wait = 0

class CorridorModel(ap.Model):
    def setup(self):
        xs, offs = self.p.xs, self.p.offsets
        self.lights = ap.AgentList(self, len(xs), TrafficLightAgent)
        for L, x, o in zip(self.lights, xs, offs):
            L.pos_x = float(x); L.offset = float(o)
        self.vehicles = []
        self.next_id = 0
        self.exited = 0
        self.total_wait = 0
        self.travel = []
        self.queue_hist = []   # carros detenidos por paso
        self.exit_hist = []    # acumulado de salidas
        self.traj = []         # (t, x, vid) para el diagrama espacio-tiempo

    def step(self):
        # 1) actualizar semáforos
        for L in self.lights:
            L.update(self.t)
        # 2) llegada de vehículos (Bernoulli/Poisson aprox)
        if np.random.rand() < self.p.arrival_rate:
            self.vehicles.append(ap.AgentList(self, 1, VehicleAgent)[0])
        # 3) mover vehículos del frente hacia atrás (car-following + semáforo)
        stopped = 0
        prev_x = None
        for veh in sorted(self.vehicles, key=lambda a: a.x, reverse=True):
            target = veh.x + self.p.speed
            if prev_x is not None:
                target = min(target, prev_x - self.p.min_gap)   # no chocar al de adelante
            for L in self.lights:                                # próximo semáforo adelante
                if L.pos_x > veh.x:
                    if L.state != 'green':
                        target = min(target, L.pos_x - self.p.stop_margin)
                    break
            target = max(target, veh.x)
            if target - veh.x < 0.1:
                veh.wait += 1; self.total_wait += 1; stopped += 1
            veh.x = target
            prev_x = veh.x
            self.traj.append((self.t, veh.x, veh.vid))
        self.queue_hist.append(stopped)
        # 4) salidas
        for v in [v for v in self.vehicles if v.x >= self.p.x_exit]:
            self.exited += 1; self.travel.append(self.t - v.t0)
        self.vehicles = [v for v in self.vehicles if v.x < self.p.x_exit]
        self.exit_hist.append(self.exited)

    def end(self):
        self.report('exited', self.exited)
        self.report('avg_wait', self.total_wait / max(1, self.exited))
        self.report('avg_travel', float(np.mean(self.travel)) if self.travel else 0.0)
        self.report('avg_queue', float(np.mean(self.queue_hist)) if self.queue_hist else 0.0)
'''

cells = []

cells.append(md(
"# Simulación multiagente (AgentPy) — Corredor Av. Luis Elizondo",
"",
"**Reto Integrador – TC2008.** Simulación de los 3 cruces de Av. Luis Elizondo",
"(García Roel · Junco · Garza Sada) con **agentes**:",
"",
"- **Semáforos** (`TrafficLightAgent`): su fase sale de un **ciclo común + offset**",
"  (coordinación de onda verde).",
"- **Vehículos** (`VehicleAgent`): avanzan por el corredor, respetan el semáforo y",
"  mantienen distancia con el de adelante (car-following).",
"",
"Se mide cola, espera y throughput, se grafica el **diagrama espacio–tiempo simulado**",
"y se compara **coordinado vs sin coordinar**.",
"",
"_Complementa la simulación 3D en Unity y el cuaderno de offsets/onda verde._"
))

cells.append(code("!pip install -q agentpy"))

cells.append(code(
"import agentpy as ap\n"
"import numpy as np\n"
"import pandas as pd\n"
"import matplotlib.pyplot as plt\n"
"import IPython"
))

cells.append(md("## 1. Definición de los agentes y el modelo"))
cells.append(code(MODELO.strip("\n")))

cells.append(md(
"## 2. Parámetros del corredor y offsets de onda verde",
"",
"Distancias reales (m), velocidad de sincronía 40 km/h y ciclo C=66 s",
"(verde 45 / amarillo 3). Los offsets se calculan con `offset = d/v`."
))
cells.append(code(
"v = 40 * 1000 / 3600          # m/s (40 km/h)\n"
"xs = [0, 345, 845]            # posición de los 3 cruces (m)\n"
"C, green, yellow = 66, 45, 3\n"
"\n"
"def offsets_forward(xs, v, C):\n"
"    o = [0.0]\n"
"    for i in range(1, len(xs)):\n"
"        o.append((o[-1] + (xs[i] - xs[i-1]) / v) % C)\n"
"    return o\n"
"\n"
"offsets_coord = offsets_forward(xs, v, C)\n"
"offsets_sinc  = [0.0, 0.0, 0.0]   # baseline: todos iguales (sin coordinar)\n"
"\n"
"base = dict(C=C, green=green, yellow=yellow, speed=v,\n"
"            x_entry=-60, x_exit=905, min_gap=7, stop_margin=5,\n"
"            arrival_rate=0.5, steps=400, seed=42)\n"
"print('Offsets coordinados:', [round(o,1) for o in offsets_coord])"
))

cells.append(md("## 3. Correr la simulación: coordinado vs sin coordinar"))
cells.append(code(
"def correr(offsets):\n"
"    p = dict(base); p['xs'] = xs; p['offsets'] = offsets\n"
"    m = CorridorModel(p)\n"
"    m.run(display=False)\n"
"    return m\n"
"\n"
"m_coord = correr(offsets_coord)\n"
"m_sinc  = correr(offsets_sinc)\n"
"\n"
"def fila(m, nombre):\n"
"    r = m.reporters   # dict con los report() del modelo\n"
"    return {'estrategia': nombre, 'salieron': int(r['exited']),\n"
"            'espera_prom_s': round(r['avg_wait'],1),\n"
"            'viaje_prom_s': round(r['avg_travel'],1),\n"
"            'cola_prom': round(r['avg_queue'],2)}\n"
"\n"
"resultados = pd.DataFrame([fila(m_coord,'Coordinado'), fila(m_sinc,'Sin coordinar')])\n"
"resultados"
))

cells.append(md(
"## 4. Diagrama espacio–tiempo simulado",
"",
"Trayectorias **reales** de los vehículos (de la simulación) sobre las bandas verdes.",
"Si las líneas avanzan sin frenar dentro de las bandas, la onda verde funciona."
))
cells.append(code(
"def espacio_tiempo_sim(m, offsets, titulo):\n"
"    fig, ax = plt.subplots(figsize=(13, 6))\n"
"    tmax = m.p.steps\n"
"    # bandas verdes por cruce\n"
"    for x, off in zip(xs, offsets):\n"
"        k = 0\n"
"        while off + k * C < tmax:\n"
"            ax.hlines(x, off + k*C, off + k*C + green, color='green', lw=8, alpha=0.30)\n"
"            k += 1\n"
"    # trayectorias por vehículo\n"
"    traj = pd.DataFrame(m.traj, columns=['t', 'x', 'vid'])\n"
"    for vid, g in traj.groupby('vid'):\n"
"        ax.plot(g['t'], g['x'], color='blue', lw=0.8, alpha=0.6)\n"
"    for x in xs:\n"
"        ax.axhline(x, color='gray', ls=':', alpha=0.4)\n"
"    ax.set_title(titulo)\n"
"    ax.set_xlabel('Tiempo (pasos)'); ax.set_ylabel('Posición en el corredor (m)')\n"
"    ax.set_xlim(0, tmax); ax.set_ylim(-60, 905)\n"
"    ax.grid(True, ls='--', alpha=0.3); plt.tight_layout(); plt.show()\n"
"\n"
"espacio_tiempo_sim(m_coord, offsets_coord, 'Espacio-tiempo SIMULADO — Coordinado (onda verde)')\n"
"espacio_tiempo_sim(m_sinc,  offsets_sinc,  'Espacio-tiempo SIMULADO — Sin coordinar')"
))

cells.append(md(
"## 5. Comparación de métricas"
))
cells.append(code(
"mets = ['espera_prom_s', 'viaje_prom_s', 'cola_prom']\n"
"etiquetas = ['Espera prom (s)', 'Viaje prom (s)', 'Cola prom']\n"
"x = np.arange(len(mets)); w = 0.35\n"
"plt.figure(figsize=(9, 4))\n"
"plt.bar(x - w/2, resultados.iloc[0][mets].values, w, color='green', label='Coordinado')\n"
"plt.bar(x + w/2, resultados.iloc[1][mets].values, w, color='red', alpha=0.8, label='Sin coordinar')\n"
"plt.xticks(x, etiquetas); plt.title('Coordinado vs sin coordinar (AgentPy)')\n"
"plt.legend(); plt.grid(True, axis='y', ls='--', alpha=0.4); plt.tight_layout(); plt.show()\n"
"\n"
"red_espera = 1 - resultados.iloc[0].espera_prom_s / max(resultados.iloc[1].espera_prom_s, 0.1)\n"
"print(f'La coordinación reduce la espera promedio en {red_espera:.0%}.')"
))

cells.append(md(
"## 6. Animación de la simulación",
"",
"Vista del corredor: cuadros = semáforos (verde/rojo), triángulos = vehículos."
))
cells.append(code(
"def animation_plot_single(m, ax):\n"
"    ax.clear()\n"
"    ax.set_title(f'Corredor Av. Luis Elizondo — t={m.t}')\n"
"    ax.hlines(0, m.p.x_entry, m.p.x_exit, color='gray', lw=10, alpha=0.4)\n"
"    for L in m.lights:\n"
"        c = 'green' if L.state == 'green' else ('orange' if L.state == 'yellow' else 'red')\n"
"        ax.scatter(L.pos_x, 6, c=c, s=160, marker='s', edgecolors='black')\n"
"        ax.vlines(L.pos_x, -4, 6, color='lightgray', lw=1)\n"
"    xs_v = [vh.x for vh in m.vehicles]\n"
"    ax.scatter(xs_v, [0]*len(xs_v), c='blue', s=60, marker='>')\n"
"    ax.set_xlim(m.p.x_entry, m.p.x_exit); ax.set_ylim(-12, 14)\n"
"    ax.set_yticks([]); ax.set_xlabel('Posición (m)')\n"
"\n"
"def animation_plot(parameters):\n"
"    fig, ax = plt.subplots(figsize=(12, 3))\n"
"    p = dict(parameters); p['xs'] = xs; p['offsets'] = offsets_coord; p['steps'] = 160\n"
"    model = CorridorModel(p)\n"
"    anim = ap.animate(model, fig, ax, animation_plot_single)\n"
"    return IPython.display.HTML(anim.to_jshtml(fps=10))\n"
"\n"
"animation_plot(base)"
))

cells.append(md(
"## 7. Conclusiones",
"",
"- Con la **coordinación (onda verde)** los vehículos cruzan los 3 semáforos con",
"  mucha menos espera y cola que **sin coordinar**, como se ve en el diagrama",
"  espacio–tiempo y en las métricas.",
"- El modelo multiagente (vehículos + semáforos) reproduce el efecto que también se",
"  observa en la simulación 3D de Unity y en el análisis de offsets.",
"- Al **aumentar `arrival_rate`** (más demanda) la onda verde empieza a saturarse:",
"  probar valores altos para ver cuándo se rompe la progresión.",
"- Pendiente: usar los **volúmenes reales** (mañana/mediodía/tarde) en `arrival_rate`."
))

nb = {
    "cells": cells,
    "metadata": {
        "kernelspec": {"display_name": "Python 3", "language": "python", "name": "python3"},
        "language_info": {"name": "python", "version": "3"},
    },
    "nbformat": 4, "nbformat_minor": 5,
}

out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "SimulacionMultiagente_Elizondo.ipynb")
with open(out, "w", encoding="utf-8") as f:
    json.dump(nb, f, ensure_ascii=False, indent=1)
print("Notebook generado:", out, "con", len(cells), "celdas")
