# M4_Cruce — Extensión a Corredor Completo Av. Luis Elizondo
## Instrucciones para Claude Code

---

## Estado actual del proyecto ✅

La escena `SampleScene` ya tiene funcionando:
- Cruce en T de **S2: Av. Luis Elizondo × Junco de la Vega**
- Carros con waypoints moviéndose por la horizontal (Elizondo) y vertical (Junco)
- Semáforo funcional que detiene los carros
- `WaypointMover.cs` — movimiento por waypoints con Stop/Resume
- `TrafficLight.cs` — ciclo rojo/amarillo/verde
- `StopLineTrigger.cs` — zona de parada en el cruce
- `CarCollision.cs` — interacción entre carros
- `CarSpawner.cs` — generador de carros con los 4 modelos FBX

**NO tocar ni eliminar nada de lo que ya existe.**

---

## Lo que hay que construir

Extender la escena actual para tener el **corredor completo de Av. Luis Elizondo**
con 3 semáforos coordinados por onda verde:

```
[S1: Covarrubias]----345m----[S2: Junco ← YA EXISTE]----500m----[S3: Garza Sada]
        ↑                              ↑                                 ↑
   Agregar nuevo                  Ya funciona                    Agregar nuevo
```

### Escala de la escena
La escena actual usa una escala aproximada. Mantener la misma escala visual
que ya tiene. Los semáforos nuevos se ubican respecto al cruce de Junco:
- **S1 (Covarrubias)**: 34.5 unidades a la IZQUIERDA de S2
- **S3 (Garza Sada)**: 50 unidades a la DERECHA de S2

Para conocer la posición exacta de S2, leer la posición del semáforo
existente en la escena antes de colocar los nuevos.

---

## Coordenadas y offsets del corredor real

| Semáforo | Calle transversal | Distancia | Offset onda verde |
|----------|------------------|-----------|-------------------|
| S1 | Covarrubias / García Roel | 0 m (inicio) | **0 s** (arranca primero) |
| S2 | Junco de la Vega | 345 m desde S1 | **31.1 s** |
| S3 | Eugenio Garza Sada | 500 m desde S2 | **10.1 s** (76.1 % 66) |

**Ciclo semafórico:** Verde=45s · Amarillo=3s · Rojo=18s · Total=66s

**Velocidad de los carros en Elizondo:** mantener la misma que ya tienen.
Los offsets están calculados para que un carro que pase S1 en verde
llegue a S2 y S3 también en verde (onda verde).

---

## Cambios a realizar — en este orden

### PASO 1 — Actualizar TrafficLight.cs

Agregar el campo `startOffset` que permite coordinar los semáforos.
El semáforo de S2 que ya existe debe recibir `startOffset = 31.1`.

```csharp
// AGREGAR en la clase TrafficLight:
[Header("Coordinación onda verde")]
[Tooltip("S1=0  |  S2=31.1  |  S3=10.1")]
public float startOffset = 0f;

// MODIFICAR el Update() para usar effectiveTime:
void Update()
{
    float effectiveTime = Time.time - startOffset;
    if (effectiveTime < 0f) { SetPhase(Phase.Red); return; }
    float t = effectiveTime % cycleTime;
    // resto igual...
}
```

### PASO 2 — Extender la calle Elizondo

La avenida horizontal actual solo cubre el área del cruce de Junco.
Hay que extenderla a ambos lados para llegar a S1 y S3.

Crear o extender los planos/cubos de la carretera para que cubra:
- 40 unidades a la IZQUIERDA del cruce actual (hacia S1)
- 55 unidades a la DERECHA del cruce actual (hacia S3)

### PASO 3 — Agregar S1 (Covarrubias)

A 34.5 unidades a la IZQUIERDA del semáforo S2:
1. Calle transversal (plano corto perpendicular, igual al de Junco)
2. Semáforo con `startOffset = 0` (este es el primero de la onda)
3. StopLine con trigger conectado al semáforo S1
4. Waypoints para carros que vienen de Covarrubias (opcional, puede omitirse)

### PASO 4 — Agregar S3 (Garza Sada)

A 50 unidades a la DERECHA del semáforo S2:
1. Calle transversal (igual a las anteriores)
2. Semáforo con `startOffset = 10.1`
3. StopLine con trigger conectado al semáforo S3

### PASO 5 — Actualizar waypoints de Elizondo

La ruta de los carros en la horizontal (Elizondo) debe pasar por los 3 cruces:
```
WP_Entrada → WP_AnteS1 → WP_PostS1 → WP_AnteS2 → WP_PostS2 → WP_AnteS3 → WP_PostS3 → WP_Salida
```
Los waypoints "Ante" están 3 unidades ANTES de cada StopLine.
Los waypoints "Post" están 3 unidades DESPUÉS de cada cruce.

### PASO 6 — Ajustar la cámara

Reposicionar Main Camera para ver el corredor completo de los 3 semáforos.
Vista desde arriba ligeramente inclinada (como la actual pero más alejada).

---

## Reglas importantes

- **NO borrar** la escena actual ni los scripts existentes
- **NO cambiar** los prefabs FBX de los carros
- Nuevos semáforos deben ser **idénticos visualmente** al que ya existe
- Todos los nombres de GameObjects en **español o inglés consistente**
- El tag `"Car"` debe estar en todos los prefabs de carro (necesario para StopLineTrigger)
- **Commits** con tu nombre: usar el git config del sistema, NUNCA --author

---

## Prompts para Claude Code — ejecutar en orden

### Prompt 1 — Actualizar TrafficLight.cs con offset
```
Lee @CLAUDE.md sección "PASO 1".
Modifica Assets/Scripts/TrafficLight.cs para agregar el campo startOffset
y actualizar el Update() para usar effectiveTime = Time.time - startOffset.
Mantén todo lo demás igual. Haz que compile sin errores.
Haz commit: "feat: TrafficLight con startOffset para onda verde"
```

### Prompt 2 — Extender escena con S1 y S3
```
Lee @CLAUDE.md completo.
Crea un EditorScript en Assets/Scripts/Editor/ExtendCorridor.cs con un
menú M4Cruce > Extend To Corridor que:

1. Detecta la posición actual del semáforo existente (S2/Junco) en la escena
2. Extiende la carretera horizontal a ambos lados (40 uds izq, 55 uds der)
3. Crea el semáforo S1 a 34.5 uds a la izquierda de S2:
   - Calle transversal perpendicular
   - Semáforo igual al existente (mismo aspecto visual)
   - StopLine con BoxCollider trigger
   - TrafficLight.startOffset = 0
4. Crea el semáforo S3 a 50 uds a la derecha de S2:
   - Igual estructura que S1
   - TrafficLight.startOffset = 10.1
5. Actualiza el semáforo S2 existente: startOffset = 31.1
6. Crea los 8 waypoints de la ruta completa en Elizondo
7. Repositiona la cámara para ver los 3 semáforos

NO borrar nada existente. Solo agregar lo nuevo.
Haz commit: "feat: corredor completo 3 semáforos onda verde"
```

### Prompt 3 — Verificar y ajustar
```
Lee @CLAUDE.md.
Verifica que:
1. Los 3 semáforos tienen los startOffset correctos (0, 31.1, 10.1)
2. Los StopLines están conectados a sus respectivos semáforos
3. Los waypoints de Elizondo cubren los 3 cruces
4. No hay errores de compilación en la consola

Si algo falta, corrígelo. Haz commit: "fix: ajustes corredor onda verde"
```

---

## Resultado esperado

Al dar Play, los carros en Av. Luis Elizondo deben:
- Detenerse en rojo en cualquiera de los 3 semáforos
- Avanzar en verde sin detenerse si la onda verde está funcionando
- Un carro que pasa S1 en verde debería llegar a S2 y S3 también en verde
  (tarda ~31s en llegar a S2 y ~76s en llegar a S3)

