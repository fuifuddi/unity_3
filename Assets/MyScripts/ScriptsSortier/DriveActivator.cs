using System.Collections;
using UnityEngine;

public class DriveActivator : MonoBehaviour
{
    // Distanz, die in x-Richtung gefahren werden soll (positiver Wert: in Fahrtrichtung)
    [Tooltip("Distanz, um die sich das Objekt in x-Richtung bewegen soll.")]
    public float distanceX = 1000f;

    // Zielgeschwindigkeit in x-Richtung (Einheiten pro Sekunde, z. B. mm/s)
    [Tooltip("Geschwindigkeit (Einheiten pro Sekunde), mit der sich das Objekt bewegt.")]
    public float driveSpeed = 3000f;

    // Flag, das angibt, ob gerade ein Drive-Vorgang ausgeführt wird
    private bool isExecuting = false;

    /// <summary>
    /// Startet den einmaligen Drive-Vorgang: 
    /// 1) Fahrt in x-Richtung um 'distanceX', 
    /// 2) dann Rückfahrt zur Ausgangsposition.
    /// </summary>
    public void ActivateDrive()
    {
        if (!isExecuting)
        {
            StartCoroutine(ActivateDriveCoroutine());
        }
        else
        {
            Debug.LogWarning("Drive-Vorgang läuft bereits!");
        }
    }

    private IEnumerator ActivateDriveCoroutine()
    {
        isExecuting = true;

        // 1. Originalposition in x (lokaler Raum) speichern
        float originalX = transform.localPosition.x;

        // 2. Zielposition berechnen
        float targetX = originalX + distanceX;

        // === Erster Fahrtabschnitt: von originalX bis targetX ===
        while (true)
        {
            // Aktuelle x-Position
            float currentX = transform.localPosition.x;

            // Berechne neue x-Position mit konstanter Geschwindigkeit
            float newX = Mathf.MoveTowards(currentX, targetX, driveSpeed * Time.deltaTime);

            // Setze die neue Position (lokal)
            Vector3 pos = transform.localPosition;
            pos.x = newX;
            transform.localPosition = pos;

            // Prüfen, ob das Ziel erreicht ist (nahe genug)
            if (Mathf.Approximately(newX, targetX) || Mathf.Abs(newX - targetX) < 0.0001f)
                break;

            yield return null; // Nächster Frame
        }

        // Kleines Frame‐Yield, um sicherzugehen, dass Unity die letzte Position verarbeitet
        yield return null;

        // === Zweiter Fahrtabschnitt: von targetX zurück zu originalX ===
        while (true)
        {
            float currentX = transform.localPosition.x;
            float newX = Mathf.MoveTowards(currentX, originalX, driveSpeed * Time.deltaTime);

            Vector3 pos = transform.localPosition;
            pos.x = newX;
            transform.localPosition = pos;

            if (Mathf.Approximately(newX, originalX) || Mathf.Abs(newX - originalX) < 0.0001f)
                break;

            yield return null;
        }

        // Abschließend setzen wir das Flag zurück
        isExecuting = false;
    }
}
