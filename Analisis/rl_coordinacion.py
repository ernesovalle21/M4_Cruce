"""
rl_coordinacion.py
==================
Coordinación semafórica del corredor Av. Luis Elizondo planteada como un
problema de DECISIÓN SECUENCIAL (MDP) y resuelta con tres métodos comparables
que minimizan la MISMA función de evaluación heurística:

        J = w1 * (colas) + w2 * (paradas) + w3 * (error de coordinación)

  - TIEMPO FIJO : reparto fijo verde mayor/menor (44/22). Onda verde perfecta,
                  pero no se adapta a la demanda real ............... baseline
  - HEURÍSTICA  : respeta la onda verde (ciclo e inicio del verde mayor anclados),
                  pero si la cola del corredor es alta EXTIENDE su verde y si la
                  transversal se satura le da más reparto ...... lo de la pizarra
  - Q-LEARNING  : APRENDE el reparto que minimiza J usando -J como recompensa.

Idea clave (control coordinado-actuado real): el CICLO se mantiene FIJO (C=66 s)
y el verde mayor SIEMPRE inicia en el instante coordinado (offset) -> la onda
verde queda anclada y el pelotón llega en verde. Lo único que se decide cada
ciclo es el REPARTO: cuántos de los 66 s son verde mayor (entre 30 y 54) y el
resto verde transversal. Apartarse del reparto del plan es el "error de
coordinación" que J penaliza; a cambio, adaptarse a la demanda baja colas/paradas.

MDP (por intersección; política/Q-table compartida entre las 3):
  Estado  s = (cola_corredor, cola_transversal)            [discretizado]
  Acción  a = reparto de verde mayor in {30,36,42,48,54} s
  Recomp. r = -(costo del ciclo) = -(w1*colas + w2*paradas + w3*|reparto-44|)

Corre headless:  python3 rl_coordinacion.py   (genera figuras en Analisis/figs/)
"""
import os
import numpy as np

# --------------------------------------------------------------------------
# Parámetros del corredor (mismos del reporte)
# --------------------------------------------------------------------------
V_SYNC = 11.11               # m/s (40 km/h)
DIST = [0.0, 345.0, 845.0]   # García Roel(0) -> Junco(345) -> Garza Sada(845)
C = 66                       # ciclo común FIJO (s) -> ancla la onda verde
GMAJ_NOM = 44                # reparto nominal del verde mayor (plan)
LOST = 2                     # arranque perdido al inicio de cada verde
SPLITS = [30, 36, 42, 48, 54]   # repartos posibles del verde mayor (acciones)

S_MAJOR = 1.5                # flujo de saturación mayor (veh/s, ~3 carriles)
S_MINOR = 0.7                # flujo de saturación menor (veh/s)

# Demanda (estimaciones justificadas; no se proporcionaron datos reales).
# Mayor y menor en CONTRAFASE: cuando el corredor sube, la transversal baja y
# viceversa -> el reparto fijo desperdicia verde; el adaptativo gana.
LAM_MAJOR0 = 0.50            # llegadas externas al cruce 0 (veh/s, ~1800 veh/h pico)
LAM_MINOR = 0.18             # llegadas transversales por cruce (su pico rebasa el fijo)
RUSH_AMP = 0.6
RUSH_PERIOD = 1500           # periodo valle->pico->valle (s); lento -> las colas reflejan la demanda

# Pesos de la función de evaluación J
W1, W2, W3 = 1.0, 1.0, 0.3

N = len(DIST)
TAU = [0] + [int(round((DIST[i] - DIST[i - 1]) / V_SYNC)) for i in range(1, N)]
OFFSETS = [0.0]
for i in range(1, N):
    OFFSETS.append((OFFSETS[i - 1] + (DIST[i] - DIST[i - 1]) / V_SYNC) % C)


# --------------------------------------------------------------------------
# Entorno del corredor (tiempo discreto, paso = 1 s; decisión = 1 por ciclo)
# --------------------------------------------------------------------------
class Corridor:
    def __init__(self, seed=0):
        self.rng = np.random.default_rng(seed)
        self.reset()

    def reset(self):
        self.t = 0
        self.q_major = np.zeros(N)
        self.q_minor = np.zeros(N)
        self.cyc = np.array([int(round(OFFSETS[i])) % C for i in range(N)])  # offset
        self.split = np.full(N, GMAJ_NOM)            # reparto vigente por cruce
        self.cycle_cost = np.zeros(N)                # costo local acumulado del ciclo
        self.arr_buf = [dict() for _ in range(N)]    # pelotón aguas abajo
        self.sum_queue = self.sum_stops = self.sum_coord = 0.0
        self.n_cycles = self.n_steps = 0
        self.total_veh = 0.0
        return self

    def _rush_major(self):
        return 1.0 + RUSH_AMP * np.sin(2 * np.pi * self.t / RUSH_PERIOD)

    def _rush_minor(self):
        return 1.0 - RUSH_AMP * np.sin(2 * np.pi * self.t / RUSH_PERIOD)  # contrafase

    def at_cycle_start(self, i):
        return self.cyc[i] == 0

    def state(self, i):
        def b(q):
            return 0 if q < 0.5 else 1 if q < 3 else 2 if q < 8 else 3
        return (b(self.q_major[i]), b(self.q_minor[i]))

    def set_split(self, i, s):
        self.split[i] = int(np.clip(s, SPLITS[0], SPLITS[-1]))

    def step(self):
        """Avanza 1 s. Devuelve (costo_global, costo_local_del_paso)."""
        stops_step = 0.0
        local_step = np.zeros(N)
        minor_arr = self.rng.poisson(max(0.0, LAM_MINOR * self._rush_minor()), size=N)

        for i in range(N):
            inflow_major = (self.rng.poisson(max(0.0, LAM_MAJOR0 * self._rush_major()))
                            if i == 0 else self.arr_buf[i].pop(self.t, 0))
            inflow_minor = minor_arr[i]
            self.total_veh += inflow_major + inflow_minor

            in_major = self.cyc[i] < self.split[i]
            major_green = in_major and self.cyc[i] >= LOST
            minor_green = (not in_major) and (self.cyc[i] - self.split[i]) >= LOST

            cap = S_MAJOR if major_green else 0.0
            stopped = max(0.0, inflow_major - max(0.0, cap - self.q_major[i]))
            depart = min(self.q_major[i] + inflow_major, cap)
            self.q_major[i] += inflow_major - depart
            stops_step += stopped
            local_step[i] += W2 * stopped
            if i + 1 < N and depart > 0:                 # propagar pelotón
                ta = self.t + TAU[i + 1]
                self.arr_buf[i + 1][ta] = self.arr_buf[i + 1].get(ta, 0) + depart

            capm = S_MINOR if minor_green else 0.0
            stoppedm = max(0.0, inflow_minor - max(0.0, capm - self.q_minor[i]))
            departm = min(self.q_minor[i] + inflow_minor, capm)
            self.q_minor[i] += inflow_minor - departm
            stops_step += stoppedm
            local_step[i] += W2 * stoppedm

            local_step[i] += W1 * (self.q_major[i] + self.q_minor[i])

        total_q = float(self.q_major.sum() + self.q_minor.sum())
        coord_step = 0.0
        for i in range(N):
            self.cyc[i] += 1
            if self.cyc[i] >= C:                          # fin de ciclo
                dev = abs(self.split[i] - GMAJ_NOM)
                coord_step += dev
                local_step[i] += W3 * dev                 # costo de coordinación al cerrar
                self.sum_coord += dev
                self.n_cycles += 1
                self.cyc[i] = 0

        cost = W1 * total_q + W2 * stops_step + W3 * coord_step
        self.sum_queue += total_q
        self.sum_stops += stops_step
        self.n_steps += 1
        self.t += 1
        self.cycle_cost += local_step
        return cost, local_step

    def metrics(self):
        n = max(1, self.n_steps)
        colas = self.sum_queue / n
        paradas = self.sum_stops / max(1.0, self.total_veh)
        coord = self.sum_coord / max(1, self.n_cycles)    # desfase medio del reparto (s)
        return dict(colas=colas, paradas=paradas, error_coord=coord,
                    J=W1 * colas + W2 * paradas + W3 * coord)


# --------------------------------------------------------------------------
# Políticas (deciden el reparto al inicio de cada ciclo)
# --------------------------------------------------------------------------
def decide_fixed(env, i):
    return GMAJ_NOM                                        # reparto del plan


def decide_heuristic(env, i):
    """Pizarra: extiende el verde mayor si la cola del corredor es alta; lo
    recorta si la transversal está más saturada."""
    qd = env.q_major[i] - env.q_minor[i]
    if qd > 3:
        return 52                                         # corredor saturado -> extiende
    if qd < -3:
        return 34                                         # transversal saturada -> recorta
    return GMAJ_NOM


class QLearner:
    def __init__(self, alpha=0.3, gamma=0.85):
        self.Q, self.alpha, self.gamma = {}, alpha, gamma
        self.eps = 0.3
        self.rng = np.random.default_rng(123)

    def _q(self, s):
        return self.Q.setdefault(s, np.zeros(len(SPLITS)))

    def act(self, s, explore=True):
        if explore and self.rng.random() < self.eps:
            return int(self.rng.integers(len(SPLITS)))
        return int(np.argmax(self._q(s)))

    def update(self, s, a, r, s2):
        q = self._q(s)
        q[a] += self.alpha * (r + self.gamma * np.max(self._q(s2)) - q[a])


# --------------------------------------------------------------------------
# Corridas
# --------------------------------------------------------------------------
def run_static(decide_fn, steps=8000, seed=7):
    env = Corridor(seed=seed)
    for _ in range(steps):
        for i in range(N):
            if env.at_cycle_start(i):
                env.set_split(i, decide_fn(env, i))
        env.step()
    return env.metrics()


def train_qlearning(episodes=300, steps=2500, seed=0):
    agent = QLearner()
    curve = []
    for ep in range(episodes):
        env = Corridor(seed=seed + ep)
        agent.eps = max(0.02, 0.30 * (1 - ep / episodes))
        pend = {}   # i -> (s, a)  decisión abierta del ciclo en curso
        for _ in range(steps):
            for i in range(N):
                if env.at_cycle_start(i):
                    s = env.state(i)
                    if i in pend:                         # cerrar el ciclo anterior
                        s0, a0 = pend[i]
                        agent.update(s0, a0, -env.cycle_cost[i], s)
                    a = agent.act(s, explore=True)
                    env.set_split(i, SPLITS[a])
                    env.cycle_cost[i] = 0.0
                    pend[i] = (s, a)
            env.step()
        curve.append(env.metrics()["J"])
    return agent, curve


def eval_qlearning(agent, steps=8000, seed=7):
    env = Corridor(seed=seed)
    for _ in range(steps):
        for i in range(N):
            if env.at_cycle_start(i):
                env.set_split(i, SPLITS[agent.act(env.state(i), explore=False)])
        env.step()
    return env.metrics()


# --------------------------------------------------------------------------
# Main
# --------------------------------------------------------------------------
def main():
    print("=" * 66)
    print("Coordinación semafórica como MDP — Av. Luis Elizondo")
    print("=" * 66)
    print(f"Offsets onda verde (s): {[round(o,1) for o in OFFSETS]}  (ciclo C={C}s)")
    print(f"Retardos de pelotón TAU (s): {TAU}")
    print(f"Repartos posibles (verde mayor, s): {SPLITS}")
    print(f"Pesos J: w1={W1} (colas), w2={W2} (paradas), w3={W3} (coordinación)\n")

    EVAL = 7777   # seed de evaluación FUERA del rango de entrenamiento (0..299)
    m_fix = run_static(decide_fixed, seed=EVAL)
    m_heu = run_static(decide_heuristic, seed=EVAL)
    agent, curve = train_qlearning()
    m_ql = eval_qlearning(agent, seed=EVAL)

    rows = [("Tiempo fijo", m_fix), ("Heurística", m_heu), ("Q-learning", m_ql)]
    print(f"{'Método':<14}{'J':>9}{'colas':>9}{'paradas':>10}{'coord(s)':>10}")
    print("-" * 52)
    for name, m in rows:
        print(f"{name:<14}{m['J']:>9.3f}{m['colas']:>9.3f}"
              f"{m['paradas']:>10.3f}{m['error_coord']:>10.3f}")

    mej_heu = 100 * (m_fix["J"] - m_heu["J"]) / m_fix["J"]
    mej_ql = 100 * (m_fix["J"] - m_ql["J"]) / m_fix["J"]
    print(f"\nMejora de J vs tiempo fijo:  heurística {mej_heu:+.1f}%   "
          f"Q-learning {mej_ql:+.1f}%")
    print(f"Estados aprendidos en la Q-table: {len(agent.Q)}")

    _figures(rows, curve)
    return rows, curve


def _figures(rows, curve):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    figs = os.path.join(os.path.dirname(os.path.abspath(__file__)), "figs")
    os.makedirs(figs, exist_ok=True)

    plt.figure(figsize=(7, 4))
    plt.plot(curve, lw=1.6, color="#2a7a2a", label="Q-learning")
    plt.axhline(rows[0][1]["J"], ls="--", c="#999", label="Tiempo fijo")
    plt.axhline(rows[1][1]["J"], ls="--", c="#c98a00", label="Heurística")
    plt.xlabel("Episodio de entrenamiento")
    plt.ylabel("J (costo; menor es mejor)")
    plt.title("Q-learning: curva de aprendizaje (minimiza J)")
    plt.legend(); plt.tight_layout()
    plt.savefig(os.path.join(figs, "rl_curva_aprendizaje.png"), dpi=130); plt.close()

    labels = [r[0] for r in rows]
    x = np.arange(len(labels)); cols = ["#999", "#c98a00", "#2a7a2a"]
    fig, ax = plt.subplots(1, 2, figsize=(11, 4))
    ax[0].bar(x, [r[1]["J"] for r in rows], color=cols)
    ax[0].set_xticks(x); ax[0].set_xticklabels(labels)
    ax[0].set_title("Función de evaluación J (total)"); ax[0].set_ylabel("J")
    comp = ["colas", "paradas", "error_coord"]; w = 0.25
    for k, c in enumerate(comp):
        ax[1].bar(x + (k - 1) * w, [r[1][c] for r in rows], w, label=c)
    ax[1].set_xticks(x); ax[1].set_xticklabels(labels)
    ax[1].set_title("Componentes de J"); ax[1].legend()
    plt.tight_layout()
    plt.savefig(os.path.join(figs, "rl_comparacion_J.png"), dpi=130); plt.close()
    print(f"Figuras: {figs}/rl_curva_aprendizaje.png , rl_comparacion_J.png")


if __name__ == "__main__":
    main()
