using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraPresetUI : MonoBehaviour
{
    public GameObject buttonContainer;        // Leeres GameObject als Parent für Buttons
    public Button buttonTemplate;             // Der inaktive Vorlage-Button (im Editor gesetzt)
    public Camera cam;                        // Die zu steuernde Kamera

    public Button prevButton;                 // Rückwärts-Pfeil (im Editor zuweisen)
    public Button nextButton;                 // Vorwärts-Pfeil (im Editor zuweisen)

    private Coroutine cameraMoveCoroutine;
    private int currentIndex = 0;             // Aktuelle Kamera-Position

    void Start()
    {
        // Lösche alte Buttons (außer das Template)
        foreach (Transform child in buttonContainer.transform)
        {
            if (child.gameObject != buttonTemplate.gameObject)
                Destroy(child.gameObject);
        }

        // Für jedes Preset einen Button erzeugen
        for (int i = 0; i < CameraPresets.presets.Length; i++)
        {
            var preset = CameraPresets.presets[i];

            Button btn = Instantiate(buttonTemplate, buttonContainer.transform);
            btn.gameObject.SetActive(true);
            btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = preset.name;

            int index = i; // wichtig für Closure!

            btn.onClick.AddListener(() =>
            {
                GoToPreset(index);
            });
        }

        // Weiter- und Zurück-Button verbinden
        if (prevButton != null)
            prevButton.onClick.AddListener(GoToPrev);

        if (nextButton != null)
            nextButton.onClick.AddListener(GoToNext);

        // Direkt auf die erste Ansicht beim Start springen
        if (CameraPresets.presets.Length > 0)
            GoToPreset(0);
    }

    void GoToPrev()
    {
        if (CameraPresets.presets.Length == 0) return;
        int newIndex = (currentIndex - 1 + CameraPresets.presets.Length) % CameraPresets.presets.Length;
        GoToPreset(newIndex);
    }

    void GoToNext()
    {
        if (CameraPresets.presets.Length == 0) return;
        int newIndex = (currentIndex + 1) % CameraPresets.presets.Length;
        GoToPreset(newIndex);
    }

    void GoToPreset(int index)
    {
        currentIndex = index;

        // Smooth move to preset (stop old if running)
        if (cameraMoveCoroutine != null)
            StopCoroutine(cameraMoveCoroutine);

        cameraMoveCoroutine = StartCoroutine(SmoothMoveCamera(CameraPresets.presets[index], 0.23f));
    }

    IEnumerator SmoothMoveCamera(CameraPreset preset, float duration)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        Vector3 endPos = preset.position;
        Quaternion endRot = Quaternion.Euler(preset.rotation);

        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            time += Time.deltaTime;
            yield return null;
        }
        // Sicherstellen, dass exakt Ziel erreicht wird
        cam.transform.position = endPos;
        cam.transform.rotation = endRot;
    }
}
