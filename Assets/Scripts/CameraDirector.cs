using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Director de cámara para la demo: permite cambiar entre varias vistas con botones
/// en pantalla (sin usar el Input System, para que nunca falle):
///   - Panorámica (vista general del corredor)
///   - Seguir carro (cámara persecutoria; botón para saltar al siguiente carro)
///   - Cruce fijo (una vista por cada intersección)
///
/// Se coloca en la Main Camera. CorridorBuilder lo agrega y configura al construir,
/// o puedes agregarlo con el menú "M4Cruce > Agregar Cámaras (vistas)".
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraDirector : MonoBehaviour
{
    public enum View { Panoramica, Seguir, Cruce }

    [Header("Vista panorámica")]
    public Vector3 overviewPos = new Vector3(15f, 285f, -185f);
    public Vector3 overviewEuler = new Vector3(57f, 0f, 0f);

    [Header("Cruces (posiciones en el mundo)")]
    public Vector3[] intersections = new Vector3[0];
    public string[] intersectionNames = new string[0];
    public Vector3 cruceOffset = new Vector3(11f, 15f, -20f);

    [Header("Seguir carro")]
    public float followDist = 9f;
    public float followHeight = 4.5f;

    [Header("Suavizado de cámara")]
    public float smooth = 5f;

    private View view = View.Panoramica;
    private int cruceIdx = 0;
    private Transform target;     // carro seguido
    private int carCycle = 0;

    void Start()
    {
        // No pelear con el modo "puente" Python->Unity (ese mueve la cámara aparte)
        if (FindFirstObjectByType<PythonPlayback>() != null) { enabled = false; return; }

        if (intersections == null || intersections.Length == 0) AutoFindIntersections();

        var (p, r) = DesiredPose();
        transform.position = p;
        transform.rotation = r;
    }

    void AutoFindIntersections()
    {
        var centers = new List<Vector3>();
        foreach (var s in FindObjectsByType<StopLineTrigger>(FindObjectsSortMode.None))
        {
            Vector3 c = s.intersectionCenter;
            bool dup = false;
            foreach (var e in centers) if (Mathf.Abs(e.x - c.x) < 2f) dup = true;
            if (!dup) centers.Add(new Vector3(c.x, 0f, 0f));
        }
        centers.Sort((a, b) => a.x.CompareTo(b.x));
        intersections = centers.ToArray();
        intersectionNames = new string[centers.Count];
        for (int i = 0; i < centers.Count; i++) intersectionNames[i] = "Cruce " + (i + 1);
    }

    void LateUpdate()
    {
        if (view == View.Seguir) EnsureTarget();
        var (p, r) = DesiredPose();
        float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, p, k);
        transform.rotation = Quaternion.Slerp(transform.rotation, r, k);
    }

    private (Vector3, Quaternion) DesiredPose()
    {
        switch (view)
        {
            case View.Seguir:
                if (target != null)
                {
                    Vector3 fwd = target.forward;
                    Vector3 pos = target.position - fwd * followDist + Vector3.up * followHeight;
                    Quaternion rot = Quaternion.LookRotation(
                        (target.position + Vector3.up * 0.8f) - pos, Vector3.up);
                    return (pos, rot);
                }
                break;

            case View.Cruce:
                if (intersections != null && cruceIdx < intersections.Length)
                {
                    Vector3 c = intersections[cruceIdx];
                    Vector3 pos = c + cruceOffset;
                    Quaternion rot = Quaternion.LookRotation((c + Vector3.up * 0.5f) - pos, Vector3.up);
                    return (pos, rot);
                }
                break;
        }
        // Panorámica (y fallback)
        return (overviewPos, Quaternion.Euler(overviewEuler));
    }

    private void EnsureTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy) return;
        PickCar(0);
    }

    private void PickCar(int dir)
    {
        GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");
        if (cars.Length == 0) { target = null; return; }
        System.Array.Sort(cars, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        carCycle = (carCycle + dir % cars.Length + cars.Length) % cars.Length;
        target = cars[carCycle].transform;
    }

    void OnGUI()
    {
        float w = 165f, h = 28f, pad = 6f;
        float x = Screen.width - w - 12f, y = 12f;
        int rows = 2 + Mathf.Max(1, intersections.Length) + (view == View.Seguir ? 1 : 0);
        GUI.Box(new Rect(x - 8, y - 8, w + 16, (h + pad) * rows + 26), "Cámara");
        y += 20f;

        if (GUI.Button(new Rect(x, y, w, h), view == View.Panoramica ? "▶ Panorámica" : "Panorámica"))
            view = View.Panoramica;
        y += h + pad;

        if (GUI.Button(new Rect(x, y, w, h), view == View.Seguir ? "▶ Seguir carro" : "Seguir carro"))
            view = View.Seguir;
        y += h + pad;

        if (view == View.Seguir)
        {
            if (GUI.Button(new Rect(x, y, w, h), "› Siguiente carro")) PickCar(1);
            y += h + pad;
        }

        for (int i = 0; i < intersections.Length; i++)
        {
            string nm = (intersectionNames != null && i < intersectionNames.Length)
                ? intersectionNames[i] : ("Cruce " + (i + 1));
            bool activo = (view == View.Cruce && cruceIdx == i);
            if (GUI.Button(new Rect(x, y, w, h), (activo ? "▶ " : "") + nm))
            { view = View.Cruce; cruceIdx = i; }
            y += h + pad;
        }
    }
}
