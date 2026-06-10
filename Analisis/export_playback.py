"""
Corre el modelo multiagente (AgentPy) y exporta playback.json para que Unity lo
reproduzca (puente Python -> Unity).

Ejecutar:  python3 export_playback.py
Salida:    playback.json  (en esta carpeta Analisis/)
"""
import agentpy as ap
import numpy as np
import json, os

class TrafficLightAgent(ap.Agent):
    def setup(self):
        self.pos_x = 0.0; self.offset = 0.0; self.state = 'red'
    def update(self, t):
        C, g, y = self.p.C, self.p.green, self.p.yellow
        ph = (t - self.offset) % C
        self.state = 'green' if ph < g else ('yellow' if ph < g + y else 'red')

class VehicleAgent(ap.Agent):
    def setup(self):
        self.x = self.p.x_entry; self.t0 = self.model.t

class CorridorModel(ap.Model):
    def setup(self):
        self.lights = ap.AgentList(self, len(self.p.xs), TrafficLightAgent)
        for L, x, o in zip(self.lights, self.p.xs, self.p.offsets):
            L.pos_x = float(x); L.offset = float(o)
        self.vehicles = []
        self.frames = []
    def step(self):
        for L in self.lights: L.update(self.t)
        if np.random.rand() < self.p.arrival_rate:
            self.vehicles.append(ap.AgentList(self, 1, VehicleAgent)[0])
        prev_x = None
        for veh in sorted(self.vehicles, key=lambda a: a.x, reverse=True):
            target = veh.x + self.p.speed
            if prev_x is not None:
                target = min(target, prev_x - self.p.min_gap)
            for L in self.lights:
                if L.pos_x > veh.x:
                    if L.state != 'green':
                        target = min(target, L.pos_x - self.p.stop_margin)
                    break
            veh.x = max(target, veh.x); prev_x = veh.x
        self.vehicles = [v for v in self.vehicles if v.x < self.p.x_exit]
        # snapshot del frame
        state_map = {'red': 0, 'yellow': 1, 'green': 2}
        self.frames.append({
            "t": int(self.t),
            "lights": [state_map[L.state] for L in self.lights],
            "cars": [round(float(v.x), 2) for v in self.vehicles],
        })

v = 40 * 1000 / 3600
xs = [0, 345, 845]
C = 66
def offsets_forward(xs, v, C):
    o = [0.0]
    for i in range(1, len(xs)): o.append((o[-1] + (xs[i]-xs[i-1])/v) % C)
    return o

p = dict(C=C, green=45, yellow=3, speed=v, x_entry=-60, x_exit=905,
         min_gap=7, stop_margin=5, arrival_rate=0.45, steps=220, seed=7,
         xs=xs, offsets=offsets_forward(xs, v, C))

m = CorridorModel(p); m.run(display=False)

out = {
    "xs": [float(x) for x in xs],
    "xEntry": float(p["x_entry"]),
    "xExit": float(p["x_exit"]),
    "dt": 1.0,
    "frames": m.frames,
}
path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "playback.json")
with open(path, "w", encoding="utf-8") as f:
    json.dump(out, f)
print(f"playback.json generado: {path}  ({len(m.frames)} frames)")
