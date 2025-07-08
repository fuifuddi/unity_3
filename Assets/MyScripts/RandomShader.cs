using UnityEngine;

public class SceneViewCamera : MonoBehaviour
{
    public float panSpeed = 0.5f;
    public float rotationSpeed = 3.0f;
    public float zoomSpeed = 10.0f;

    private Vector3 lastMousePosition;

    void Update()
    {
        // Rechte Maustaste gedrückt: Kamera drehen (umschauen)
        if (Input.GetMouseButton(1))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            float rotX = mouseDelta.y * -rotationSpeed * 0.02f;
            float rotY = mouseDelta.x * rotationSpeed * 0.02f;
            transform.eulerAngles += new Vector3(rotX, rotY, 0);
        }

        // Mittlere Maustaste gedrückt: Kamera verschieben (pan)
        if (Input.GetMouseButton(2))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            Vector3 pan = new Vector3(-mouseDelta.x, -mouseDelta.y, 0) * panSpeed * 0.02f;
            transform.Translate(pan, Space.Self);
        }

        // Mausrad: vor/zurück (Zoom)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 zoom = transform.forward * scroll * zoomSpeed;
            transform.position += zoom;
        }

        lastMousePosition = Input.mousePosition;
    }
}
