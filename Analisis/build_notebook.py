"""
Genera el notebook OndaVerde_Elizondo.ipynb (análisis de coordinación semafórica
del corredor Av. Luis Elizondo, Distrito Tec).

Ejecutar:  python3 build_notebook.py
Produce:   OndaVerde_Elizondo.ipynb  (subir a Google Colab / Jupyter)
"""
import json, os

def md(*lines):
    return {"cell_type": "markdown", "metadata": {}, "source": "\n".join(lines)}

def code(src):
    return {"cell_type": "code", "metadata": {}, "execution_count": None,
            "outputs": [], "source": src.strip("\n")}

cells = []

cells.append(md(
"# Coordinación semafórica — Onda verde en Av. Luis Elizondo (Distrito Tec)",
"",
"**Reto Integrador – TC2008 (Sistemas Multiagente).**",
"",
"Corredor de **3 intersecciones** sobre Av. Luis Elizondo:",
"",
"1. **Fernando García Roel / Cantú Leal**",
"2. **Junco de la Vega** (cruce en T)",
"3. **Eugenio Garza Sada**",
"",
"Este cuaderno calcula los **offsets** de coordinación, dibuja el **diagrama",
"espacio–tiempo** con bandas verdes y trayectorias, y evalúa el **ancho de banda**",
"y la **progresión vehicular** (qué tan bien se mantiene la onda verde).",
"",
"> Nota: las distancias son aproximadas medidas en Google Maps y deben confirmarse.",
"> Los volúmenes son estimaciones razonables mientras se obtienen los datos reales."
))

cells.append(code(
"import numpy as np\n"
"import pandas as pd\n"
"import matplotlib.pyplot as plt"
))

cells.append(md(
"## 1. Parámetros del corredor y justificación",
"",
"**Distancias entre intersecciones** (medidas en Google Maps, eje del corredor):",
"",
"| Tramo | Distancia |",
"|---|---|",
"| García Roel → Junco | 345 m |",
"| Junco → Garza Sada | 500 m |",
"",
"**Velocidad de sincronía: 40 km/h.** Se justifica porque es una avenida urbana",
"dentro del Distrito Tec, con presencia de peatones, paradas y campus, donde una",
"progresión de 40 km/h es realista y segura (por debajo de arterias de 50–60 km/h).",
"",
"**Ciclo semafórico:** C = 66 s, con **verde coordinado = 45 s**, amarillo = 3 s,",
"rojo = 18 s. Se da verde amplio a Av. Luis Elizondo por ser el corredor principal",
"(las transversales reciben el resto del ciclo)."
))

cells.append(code(
"# --- Datos del corredor ---\n"
"intersections = pd.DataFrame({\n"
"    'id':     ['I1', 'I2', 'I3'],\n"
"    'nombre': ['Fernando García Roel', 'Junco de la Vega', 'Eugenio Garza Sada'],\n"
"    'dist_m': [0, 345, 845],   # acumulada sobre el corredor\n"
"})\n"
"\n"
"C       = 66.0          # ciclo común (s)\n"
"verde   = 45.0          # verde coordinado (s)\n"
"amarillo = 3.0\n"
"rojo    = 18.0\n"
"v_kmh   = 40.0          # velocidad de sincronía\n"
"v       = v_kmh * 1000 / 3600   # m/s\n"
"\n"
"print(f'Velocidad de sincronía: {v_kmh} km/h = {v:.2f} m/s')\n"
"print(f'Ciclo C = {C}s  (verde {verde}s / amarillo {amarillo}s / rojo {rojo}s)')\n"
"print(f'Split de verde coordinado = {verde/C:.0%}')\n"
"intersections"
))

cells.append(md(
"## 2. Cálculo de offsets",
"",
"El **offset** es el desfase de arranque del verde entre intersecciones para que",
"un pelotón viaje sin detenerse:",
"",
"$$ t_i = \\frac{d_i - d_{i-1}}{v} \\qquad Offset_i = (Offset_{i-1} + t_i)\\bmod C $$",
"",
"Para el sentido contrario el cálculo se hace desde la última intersección."
))

cells.append(code(
"def offsets_forward(d, v, C):\n"
"    o = [0.0]\n"
"    for i in range(1, len(d)):\n"
"        o.append((o[-1] + (d[i] - d[i-1]) / v) % C)\n"
"    return o\n"
"\n"
"def offsets_reverse(d, v, C):\n"
"    o = [0.0] * len(d)\n"
"    for i in range(len(d) - 2, -1, -1):\n"
"        o[i] = (o[i+1] + (d[i+1] - d[i]) / v) % C\n"
"    return o\n"
"\n"
"d = intersections['dist_m'].values\n"
"intersections['offset_fwd'] = np.round(offsets_forward(d, v, C), 2)\n"
"intersections['offset_rev'] = np.round(offsets_reverse(d, v, C), 2)\n"
"intersections"
))

cells.append(md(
"Los offsets (0 / 31.1 / 10.1 s en sentido García Roel→Garza Sada) coinciden con el",
"plan de coordinación del corredor: cada cruce abre su verde justo cuando el pelotón",
"que viaja a 40 km/h llega a él."
))

cells.append(md(
"## 3. Diagrama espacio–tiempo (entregable clave)",
"",
"- Eje X = tiempo, eje Y = posición sobre el corredor.",
"- Las **barras verdes** son las ventanas de verde coordinado de cada intersección.",
"- Las **líneas** son trayectorias de vehículos (pendiente = velocidad de sincronía).",
"- Si una trayectoria cruza todas las barras verdes, la **onda verde se mantiene**."
))

cells.append(code(
"import os\n"
"os.makedirs('figs', exist_ok=True)\n"
"\n"
"def diagrama_espacio_tiempo(inter, C, g, v, n_ciclos=3, sentido='fwd', nombre=None):\n"
"    fig, ax = plt.subplots(figsize=(13, 7))\n"
"    dmax = inter['dist_m'].max()\n"
"    col = 'offset_fwd' if sentido == 'fwd' else 'offset_rev'\n"
"\n"
"    # Bandas verdes por intersección\n"
"    for _, row in inter.iterrows():\n"
"        y = row['dist_m']\n"
"        for k in range(n_ciclos):\n"
"            start = row[col] + k * C\n"
"            ax.hlines(y, start, start + g, color='green', linewidth=10, alpha=0.45)\n"
"        ax.text(-6, y, row['id'], va='center', ha='right', fontweight='bold')\n"
"\n"
"    # Trayectorias del pelotón\n"
"    for k in range(n_ciclos + 1):\n"
"        if sentido == 'fwd':\n"
"            t0 = inter['offset_fwd'].iloc[0] + k * C\n"
"            ax.plot([t0, t0 + dmax / v], [0, dmax], color='blue', lw=2.2,\n"
"                    label='Pelotón (onda verde)' if k == 0 else None)\n"
"        else:\n"
"            t0 = inter['offset_rev'].iloc[-1] + k * C\n"
"            ax.plot([t0, t0 + dmax / v], [dmax, 0], color='red', lw=2.2,\n"
"                    label='Pelotón (onda verde)' if k == 0 else None)\n"
"\n"
"    titulo = nombre if nombre else ('Diagrama espacio-tiempo - sentido ' + ('Garcia Roel a Garza Sada' if sentido=='fwd' else 'Garza Sada a Garcia Roel'))\n"
"    ax.set_title(titulo)\n"
"    ax.set_xlabel('Tiempo (s)')\n"
"    ax.set_ylabel('Distancia sobre el corredor (m)')\n"
"    ax.set_xlim(0, C * n_ciclos)\n"
"    ax.set_ylim(-40, dmax + 60)\n"
"    ax.grid(True, linestyle='--', alpha=0.4)\n"
"    ax.legend(loc='upper right')\n"
"    slug = ''.join(c if c.isalnum() else '_' for c in titulo)[:50]\n"
"    plt.tight_layout(); plt.savefig(f'figs/{slug}.png', dpi=120, bbox_inches='tight'); plt.show()\n"
"\n"
"diagrama_espacio_tiempo(intersections, C, verde, v, sentido='fwd')\n"
"diagrama_espacio_tiempo(intersections, C, verde, v, sentido='rev')"
))

cells.append(md(
"## 4. Ancho de banda y progresión",
"",
"El **ancho de banda** es el rango de tiempo (s) dentro del cual un vehículo puede",
"entrar al corredor y encontrar verde en **todas** las intersecciones. Es la medida",
"de qué tan buena es la coordinación."
))

cells.append(code(
"def ancho_de_banda(offsets, d, v, C, g, n=4000):\n"
"    ts = np.linspace(0, C, n, endpoint=False)\n"
"    ok = np.ones(n, dtype=bool)\n"
"    for off, di in zip(offsets, d):\n"
"        arr = (ts + di / v) % C            # instante de llegada a la intersección\n"
"        ok &= ((arr - off) % C) <= g       # ¿cae en su ventana verde?\n"
"    # corrida contigua más larga (circular)\n"
"    run = best = 0\n"
"    for b in np.concatenate([ok, ok]):\n"
"        run = run + 1 if b else 0\n"
"        best = max(best, run)\n"
"    return min(best, n) / n * C\n"
"\n"
"of = offsets_forward(d, v, C)\n"
"orv = offsets_reverse(d, v, C)\n"
"bw_f = ancho_de_banda(of, d, v, C, verde)\n"
"bw_r = ancho_de_banda(orv, d, v, C, verde)\n"
"\n"
"print(f'Ancho de banda  García Roel→Garza Sada: {bw_f:5.1f} s  ({bw_f/C:.0%} del ciclo)')\n"
"print(f'Ancho de banda  Garza Sada→García Roel: {bw_r:5.1f} s  ({bw_r/C:.0%} del ciclo)')\n"
"print(f'Eficiencia bidireccional promedio:      {(bw_f+bw_r)/2/C:.0%}')"
))

cells.append(md(
"## 5. Evaluación de la progresión (¿llega en verde?)",
"",
"Se simula un vehículo que sale en el instante óptimo y se verifica si llega en",
"verde a cada intersección."
))

cells.append(code(
"def evalua_progresion(inter, v, C, g, sentido='fwd'):\n"
"    d = inter['dist_m'].values\n"
"    nombres = inter['nombre'].values\n"
"    n = len(d)\n"
"    off = offsets_forward(d, v, C) if sentido == 'fwd' else offsets_reverse(d, v, C)\n"
"    idxs = range(n) if sentido == 'fwd' else range(n - 1, -1, -1)\n"
"    base = d[0] if sentido == 'fwd' else d[-1]\n"
"    t0 = off[0] if sentido == 'fwd' else off[-1]\n"
"    out = []\n"
"    for i in idxs:\n"
"        tt = abs(d[i] - base) / v\n"
"        arr = (t0 + tt) % C\n"
"        en_verde = ((arr - off[i]) % C) <= g + 1e-9   # tolerancia numérica\n"
"        out.append({'interseccion': nombres[i], 't_llegada_s': round(tt, 1),\n"
"                    'offset_s': round(off[i], 2), 'llega_en_verde': en_verde})\n"
"    return pd.DataFrame(out)\n"
"\n"
"ev_f = evalua_progresion(intersections, v, C, verde, 'fwd')\n"
"ev_r = evalua_progresion(intersections, v, C, verde, 'rev')\n"
"print('Sentido García Roel -> Garza Sada')\n"
"print(ev_f.to_string(index=False))\n"
"print('\\nSentido Garza Sada -> García Roel')\n"
"print(ev_r.to_string(index=False))\n"
"print(f'\\nProgresión en verde  fwd: {ev_f.llega_en_verde.mean():.0%} | rev: {ev_r.llega_en_verde.mean():.0%}')"
))

cells.append(md(
"## 5b. Comparación: con onda verde vs sin coordinar",
"",
"Se compara la estrategia **coordinada** (offsets calculados) contra un plan **sin",
"coordinar** (todos los semáforos con offset 0, mismo ciclo). El **ancho de banda**",
"evidencia el beneficio de la coordinación."
))

cells.append(code(
"zeros = [0.0] * len(d)\n"
"\n"
"def progresion_pct(offsets, fwd=True):\n"
"    start = 0 if fwd else len(d) - 1\n"
"    t0 = offsets[start]\n"
"    ok = 0\n"
"    for i in range(len(d)):\n"
"        arr = (t0 + abs(d[i] - d[start]) / v) % C\n"
"        if ((arr - offsets[i]) % C) <= verde + 1e-9: ok += 1\n"
"    return ok / len(d)\n"
"\n"
"comparacion = pd.DataFrame({\n"
"    'estrategia':  ['Coordinado (onda verde)', 'Sin coordinar (offset 0)'],\n"
"    'banda_fwd_s': [round(ancho_de_banda(of,  d, v, C, verde), 1),\n"
"                    round(ancho_de_banda(zeros, d, v, C, verde), 1)],\n"
"    'banda_rev_s': [round(ancho_de_banda(orv, d, v, C, verde), 1),\n"
"                    round(ancho_de_banda(zeros, d, v, C, verde), 1)],\n"
"})\n"
"mejora = comparacion['banda_fwd_s'][0] / max(comparacion['banda_fwd_s'][1], 0.1)\n"
"print(comparacion.to_string(index=False))\n"
"print(f'\\nMejora de ancho de banda (sentido principal): x{mejora:.1f}')\n"
"\n"
"# Diagrama espacio-tiempo SIN coordinar (offsets 0) para contraste visual\n"
"inter_sc = intersections.copy()\n"
"inter_sc['offset_fwd'] = 0.0\n"
"diagrama_espacio_tiempo(inter_sc, C, verde, v, sentido='fwd', nombre='Espacio-tiempo SIN coordinar')"
))

cells.append(md(
"## 6. Escenarios de demanda (mañana / mediodía / tarde)",
"",
"Mientras se obtienen los **datos reales** de volumen, se usan estimaciones (veh/h",
"por acceso) **justificadas con valores típicos de ingeniería de tráfico**:",
"",
"- Capacidad de arteria urbana señalizada: **~1,100–1,580 veh/h por carril**.",
"- Flujo óptimo (sin congestión): **< ~500 veh/h por carril**.",
"- Relación día↔pico: una vía de ~10,000 veh/día → ~1,000 veh en la hora pico.",
"",
"Con base en eso se estiman ~300–370 veh/h por carril en Elizondo (3 carriles) en",
"horas pico, valores realistas para una avenida urbana del Distrito Tec.",
"",
"> Sustituir por los **datos reales** del curso cuando estén disponibles.",
"> Refs: Mike on Traffic; NACTO Design Hour; VTPI Speed vs Capacity."
))

cells.append(code(
"demanda = pd.DataFrame({\n"
"    'periodo':   ['Mañana (7-9)', 'Mediodía (13-15)', 'Tarde (18-20)'],\n"
"    'Elizondo_vph': [900, 650, 1100],   # estimado, corredor principal\n"
"    'transversal_vph': [500, 350, 600], # estimado, suma de transversales\n"
"})\n"
"# Grado de saturación aproximado (capacidad ~ verde/ciclo * 1800 vph por carril)\n"
"cap_eliz = (verde / C) * 1800 * 3   # 3 carriles\n"
"demanda['x_Elizondo'] = (demanda['Elizondo_vph'] / cap_eliz).round(2)\n"
"demanda['onda_estable'] = demanda['x_Elizondo'] < 0.85\n"
"print(f'Capacidad estimada Elizondo: {cap_eliz:.0f} vph')\n"
"demanda"
))

cells.append(md(
"## 6b. Resultados de la simulación (Unity)",
"",
"Lee los CSV generados por la simulación multiagente en Unity",
"(`metrics_serie_*.csv` y `metrics_resumen_*.csv`) y compara los dos modos:",
"**coordinado** (onda verde actuada) vs **sin coordinar** (ciclo fijo).",
"",
"> En Colab: sube los 4 CSV a esta carpeta. En local: ya están en `Analisis/`.",
"> Corre la simulación en ambos modos antes de ejecutar esta celda."
))

cells.append(code(
"import os\n"
"\n"
"BASE = ''  # carpeta de los CSV (Colab: sube los archivos aquí)\n"
"\n"
"def _serie(label):\n"
"    p = os.path.join(BASE, f'metrics_serie_{label}.csv')\n"
"    return pd.read_csv(p) if os.path.exists(p) else None\n"
"\n"
"def _resumen(label):\n"
"    p = os.path.join(BASE, f'metrics_resumen_{label}.csv')\n"
"    if not os.path.exists(p): return None\n"
"    out = {}\n"
"    for line in open(p, encoding='utf-8'):\n"
"        line = line.strip()\n"
"        if line.startswith('interseccion'): break\n"
"        if not line or line.startswith('metrica'): continue\n"
"        a = line.split(',')\n"
"        if len(a) >= 2:\n"
"            try: out[a[0]] = float(a[1])\n"
"            except ValueError: pass\n"
"    return out\n"
"\n"
"sc, ss = _serie('coordinado'), _serie('sin_coordinar')\n"
"rc, rs = _resumen('coordinado'), _resumen('sin_coordinar')\n"
"\n"
"if sc is None and ss is None:\n"
"    print('Aún no hay CSV de Unity. Corre la simulación en modo Coordinado y SIN')\n"
"    print('coordinar, y coloca los metrics_*.csv en esta carpeta; luego re-ejecuta.')\n"
"else:\n"
"    # 1) Cola vs tiempo\n"
"    plt.figure(figsize=(11, 4))\n"
"    if sc is not None: plt.plot(sc.t_s, sc.cola, color='green', label='Coordinado')\n"
"    if ss is not None: plt.plot(ss.t_s, ss.cola, color='red', alpha=0.8, label='Sin coordinar')\n"
"    plt.title('Longitud de cola en el tiempo'); plt.xlabel('t (s)'); plt.ylabel('carros en cola')\n"
"    plt.legend(); plt.grid(True, ls='--', alpha=0.4); plt.tight_layout()\n"
"    plt.savefig('figs/resultados_cola.png', dpi=120, bbox_inches='tight'); plt.show()\n"
"\n"
"    # 2) Carros completados (acumulado)\n"
"    plt.figure(figsize=(11, 4))\n"
"    if sc is not None: plt.plot(sc.t_s, sc.completados, color='green', label='Coordinado')\n"
"    if ss is not None: plt.plot(ss.t_s, ss.completados, color='red', alpha=0.8, label='Sin coordinar')\n"
"    plt.title('Carros que completan la ruta (acumulado)'); plt.xlabel('t (s)'); plt.ylabel('completados')\n"
"    plt.legend(); plt.grid(True, ls='--', alpha=0.4); plt.tight_layout()\n"
"    plt.savefig('figs/resultados_completados.png', dpi=120, bbox_inches='tight'); plt.show()\n"
"\n"
"    # 3) Barras de resumen\n"
"    if rc and rs:\n"
"        mets = ['espera_promedio_s', 'throughput_veh_min', 'cola_promedio']\n"
"        etiquetas = ['Espera prom (s)', 'Throughput (veh/min)', 'Cola prom']\n"
"        x = np.arange(len(mets)); w = 0.35\n"
"        plt.figure(figsize=(9, 4))\n"
"        plt.bar(x - w/2, [rc.get(m, 0) for m in mets], w, color='green', label='Coordinado')\n"
"        plt.bar(x + w/2, [rs.get(m, 0) for m in mets], w, color='red', alpha=0.8, label='Sin coordinar')\n"
"        plt.xticks(x, etiquetas); plt.title('Resumen: coordinado vs sin coordinar')\n"
"        plt.legend(); plt.grid(True, axis='y', ls='--', alpha=0.4); plt.tight_layout()\n"
"        plt.savefig('figs/resultados_resumen.png', dpi=120, bbox_inches='tight'); plt.show()\n"
"        print('Coordinado   :', rc)\n"
"        print('Sin coordinar:', rs)"
))

cells.append(md(
"## 7. Conclusiones preliminares (preguntas clave del reto)",
"",
"- **¿Es posible coordinar el corredor?** Sí: con v=40 km/h y C=66 s, los offsets",
"  (0 / 31.1 / 10.1 s) logran una banda verde de ~45 s (68% del ciclo) en el sentido",
"  principal.",
"- **¿Qué tan estable es la progresión?** Excelente en el sentido coordinado (100%",
"  en verde); en el sentido contrario la banda baja (~21 s), típico en corredores",
"  asimétricos.",
"- **¿Qué pasa al aumentar la demanda?** En la tarde el grado de saturación sube;",
"  si supera ~0.85 la onda verde empieza a romperse por colas residuales.",
"- **¿Sensibilidad a la velocidad?** Cambiar v desplaza los offsets; conviene probar",
"  35–45 km/h y observar el ancho de banda (repetir el cálculo cambiando `v_kmh`).",
"- **Limitaciones urbanas:** cruce en T de Junco, vueltas e incorporaciones y",
"  peatones reducen la capacidad efectiva y el ancho de banda real.",
"",
"_Estos resultados se complementan con la simulación multiagente en Unity (carros y",
"semáforos como agentes) y sus métricas de cola y tiempo de espera._"
))

nb = {
    "cells": cells,
    "metadata": {
        "kernelspec": {"display_name": "Python 3", "language": "python", "name": "python3"},
        "language_info": {"name": "python", "version": "3"},
    },
    "nbformat": 4,
    "nbformat_minor": 5,
}

out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "OndaVerde_Elizondo.ipynb")
with open(out, "w", encoding="utf-8") as f:
    json.dump(nb, f, ensure_ascii=False, indent=1)
print("Notebook generado:", out, "con", len(cells), "celdas")
