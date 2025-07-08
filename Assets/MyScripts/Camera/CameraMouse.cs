// SceneViewRuntimeCamera.cs
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SceneViewRuntimeCamera : MonoBehaviour
{
    [Header("Geschwindigkeiten")]
    public float translationSensitivity = 2f;  // Pan-Geschwindigkeit
    public float zoomSensitivity = 10f; // Zoom-Geschwindigkeit
    public float rotationSensitivity = 2f;  // Dreh-Geschwindigkeit

    // Achsenbelegung (Standard in Unity)
    public string mouseHorizontalAxisName = "Mouse X";
    public string mouseVerticalAxisName = "Mouse Y";
    public string scrollAxisName = "Mouse ScrollWheel";

    private Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void Update()
    {
        // 1) Pan mit mittlerer Maustaste
        if (Input.GetMouseButton(2))
        {
            float moveX = Input.GetAxis(mouseHorizontalAxisName) * translationSensitivity;
            float moveY = Input.GetAxis(mouseVerticalAxisName) * translationSensitivity;
            transform.Translate(-moveX, -moveY, 0f, Space.Self);
        }

        // 2) Zoom mit Mausrad
        float scroll = Input.GetAxis(scrollAxisName);
        if (Mathf.Abs(scroll) > 0.0001f)
            transform.Translate(0f, 0f, scroll * zoomSensitivity, Space.Self);

        // 3) Rotate mit rechter Maustaste
        if (Input.GetMouseButton(1))
        {
            float rotY = Input.GetAxis(mouseHorizontalAxisName) * rotationSensitivity;
            float rotX = -Input.GetAxis(mouseVerticalAxisName) * rotationSensitivity;
            // Yaw (Welt-raum) und Pitch (lokal)
            transform.Rotate(0f, rotY, 0f, Space.World);
            transform.Rotate(rotX, 0f, 0f, Space.Self);
        }

        // Optional: F‑Taste zum Fokussieren (Frame Selected)
        if (Input.GetKeyDown(KeyCode.F))
        {
            Ray ray;
            RaycastHit hit;
            ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                FocusCameraOn(hit.transform.gameObject);
        }
    }

    // Fokus-Funktion wie im Editor (via “F”)
    void FocusCameraOn(GameObject go)
    {
        Bounds b = CalculateBounds(go);
        float radius = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        float dist = radius / (Mathf.Sin(_camera.fieldOfView * Mathf.Deg2Rad / 2f));
        _camera.transform.position = go.transform.position - _camera.transform.forward * dist;
    }

    Bounds CalculateBounds(GameObject go)
    {
        var b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            b.Encapsulate(r.bounds);
        return b;
    }
}
