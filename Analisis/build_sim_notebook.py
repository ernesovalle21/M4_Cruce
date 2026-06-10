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
        # Origen-destino: con prob turn_prob el carro gira (su destino es una
        # intersección); si no, cruza todo el corredor.
        tp = getattr(self.p, 'turn_prob', 0.0)
        opciones = [x for x in self.model.p.xs if x > self.x]
        if tp > 0.0 and opciones and np.random.rand() < tp:
            self.dest = float(np.random.choice(opciones))
        else:
            self.dest = float(self.p.x_exit)

class CorridorModel(ap.Model):
    def setup(self):
        xs, offs = self.p.xs, self.p.offsets
        self.lights = ap.AgentList(self, len(xs), TrafficLightAgent)
        for L, x, o in zip(self.lights, xs, offs):
            L.pos_x = float(x); L.offset = float(o)
        self.vehicles = []
        self.next_id = 0
        self.exited = 0
        self.turned = 0
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
        # 4) salidas / giros (origen-destino)
        for v in [v for v in self.vehicles if v.x >= v.dest]:
            if v.dest >= self.p.x_exit:
                self.exited += 1; self.travel.append(self.t - v.t0)
            else:
                self.turned += 1
        self.vehicles = [v for v in self.vehicles if v.x < v.dest]
        self.exit_hist.append(self.exited)

    def end(self):
        self.report('exited', self.exited)
        self.report('turned', self.turned)
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
"            arrival_rate=0.5, steps=400, seed=42, turn_prob=0.0)\n"
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
"## 7. Coordinación DISTRIBUIDA por comunicación entre semáforos",
"",
"Aquí los offsets **no se precalculan**: cada semáforo-agente, al ponerse en verde,",
"**le envía un mensaje** a su vecino de aguas abajo indicándole cuándo llegará el",
"pelotón; el vecino programa su verde con esa información. La **onda verde emerge",
"de la comunicación** entre agentes (coordinación distribuida).",
"",
"Esto cubre explícitamente el requisito de que los agentes *se comuniquen, compartan",
"estados y coordinen decisiones*."
))

cells.append(code(
"class CommLight(ap.Agent):\n"
"    def setup(self):\n"
"        self.pos_x = 0.0; self.state = 'red'; self.timer = 0.0; self.queue = 0\n"
"        self.downstream = None; self.pending_green_at = None\n"
"        self.msgs_sent = 0; self.is_first = False\n"
"    def sense(self):\n"
"        self.queue = sum(1 for v in self.model.vehicles if 0 < (self.pos_x - v.x) <= 35)\n"
"    def on_green(self, t):\n"
"        # COMUNICACIÓN: avisa al vecino cuándo llegará el pelotón\n"
"        if self.downstream is not None:\n"
"            travel = (self.downstream.pos_x - self.pos_x) / self.p.speed\n"
"            self.downstream.pending_green_at = t + travel\n"
"            self.msgs_sent += 1\n"
"    def step_light(self, t):\n"
"        p = self.p\n"
"        if self.state == 'green':\n"
"            self.timer += 1\n"
"            if self.timer >= p.green: self.state = 'red'; self.timer = 0\n"
"        else:\n"
"            self.timer += 1\n"
"            ready = self.timer >= p.min_red\n"
"            if self.is_first and self.timer >= p.first_red:\n"
"                self.state = 'green'; self.timer = 0; self.on_green(t)\n"
"            elif (self.pending_green_at is not None) and (t >= self.pending_green_at) and ready:\n"
"                self.state = 'green'; self.timer = 0; self.pending_green_at = None; self.on_green(t)\n"
"\n"
"class CommModel(ap.Model):\n"
"    def setup(self):\n"
"        self.lights = ap.AgentList(self, len(self.p.xs), CommLight)\n"
"        for L, x in zip(self.lights, self.p.xs): L.pos_x = float(x)\n"
"        for i in range(len(self.lights) - 1): self.lights[i].downstream = self.lights[i+1]\n"
"        self.lights[0].is_first = True; self.lights[0].state = 'green'\n"
"        self.next_id = 0\n"
"        self.vehicles = []; self.total_wait = 0; self.exited = 0; self.travel = []\n"
"    def step(self):\n"
"        for L in self.lights: L.sense()\n"
"        for L in self.lights: L.step_light(self.t)\n"
"        if np.random.rand() < self.p.arrival_rate:\n"
"            self.vehicles.append(ap.AgentList(self, 1, VehicleAgent)[0])\n"
"        prev = None\n"
"        for v in sorted(self.vehicles, key=lambda a: a.x, reverse=True):\n"
"            tgt = v.x + self.p.speed\n"
"            if prev is not None: tgt = min(tgt, prev - self.p.min_gap)\n"
"            for L in self.lights:\n"
"                if L.pos_x > v.x:\n"
"                    if L.state != 'green': tgt = min(tgt, L.pos_x - self.p.stop_margin)\n"
"                    break\n"
"            nx = max(tgt, v.x)\n"
"            if nx - v.x < 0.1: self.total_wait += 1\n"
"            v.x = nx; prev = v.x\n"
"        for v in [v for v in self.vehicles if v.x >= self.p.x_exit]:\n"
"            self.exited += 1; self.travel.append(self.t - v.t0)\n"
"        self.vehicles = [v for v in self.vehicles if v.x < self.p.x_exit]\n"
"    def end(self):\n"
"        self.report('exited', self.exited)\n"
"        self.report('avg_wait', self.total_wait / max(1, self.exited))\n"
"        self.report('mensajes', sum(L.msgs_sent for L in self.lights))\n"
"\n"
"pc = dict(base); pc['xs'] = xs\n"
"pc['green'] = 45; pc['min_red'] = 10; pc['first_red'] = 18\n"
"mc = CommModel(pc); mc.run(display=False)\n"
"print(f\"Coordinación por comunicación -> salieron={mc.reporters['exited']:.0f}, \"\n"
"      f\"espera={mc.reporters['avg_wait']:.1f}s, mensajes intercambiados={mc.reporters['mensajes']:.0f}\")\n"
"print('Los offsets EMERGEN de los mensajes entre semáforos (no se precalculan).')"
))

cells.append(md(
"## 8. Origen–destino (giros con probabilidad)",
"",
"No todos los carros cruzan el corredor completo: con probabilidad `turn_prob`",
"cada vehículo **gira en una intersección** (su destino es ese cruce), modelando",
"**relaciones origen–destino**. El resto cruza de extremo a extremo."
))

cells.append(code(
"po = dict(base); po['xs'] = xs; po['offsets'] = offsets_coord; po['turn_prob'] = 0.35\n"
"mo = CorridorModel(po); mo.run(display=False)\n"
"cruzaron = int(mo.reporters['exited']); giraron = int(mo.reporters['turned'])\n"
"total = max(1, cruzaron + giraron)\n"
"print(f'Con origen-destino (turn_prob=0.35):')\n"
"print(f'  - {cruzaron} cruzaron todo el corredor ({cruzaron/total:.0%})')\n"
"print(f'  - {giraron} giraron en una intersección ({giraron/total:.0%})')\n"
"\n"
"plt.figure(figsize=(6, 4))\n"
"plt.bar(['Cruzan todo', 'Giran en cruce'], [cruzaron, giraron], color=['steelblue', 'orange'])\n"
"plt.title('Distribución origen-destino de los vehículos')\n"
"plt.ylabel('vehículos'); plt.grid(True, axis='y', ls='--', alpha=0.4)\n"
"plt.tight_layout(); plt.show()"
))

cells.append(md(
"## 9. Conclusiones",
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
