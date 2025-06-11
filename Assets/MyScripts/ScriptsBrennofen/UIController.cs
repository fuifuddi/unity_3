using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Tooltip("Das Image, das wir rot/grün einfärben")]
    public Image readyIndicator;

    [Tooltip("Farbwert wenn bereit (grün)")]
    public Color colorReady = Color.green;
    [Tooltip("Farbwert wenn busy (rot)")]
    public Color colorBusy = Color.red;

    /// <summary>
    /// Setzt den Indicator je nach readiness.
    /// </summary>
    public void SetReady(bool ready)
    {
        Debug.Log($"[UIController] SetReady aufgerufen mit ready={ready}");
        if (readyIndicator == null)
        {
            Debug.LogError("[UIController] ⚠️ readyIndicator ist NULL! Bitte im Inspector zuweisen.");
            return;
        }

        readyIndicator.color = ready ? colorReady : colorBusy;
        Debug.Log($"[UIController] Farbe gesetzt auf {(ready ? "Grün" : "Rot")}");
    }

    private void Start()
    {
        Debug.Log("[UIController] Start: gehe davon aus, die Maschine ist bereit.");
        SetReady(true);
    }
}
