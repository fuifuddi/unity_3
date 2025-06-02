using System.Collections;
using UnityEngine;
using game4automation; // Namespace des Drive-Skripts

public class DriveActivator : MonoBehaviour
{
    // Referenz zum Drive-Script, das dieses Objekt steuert
    public Drive drive;

    // Distanz, die in x-Richtung gefahren werden soll (positiver Wert: in Fahrtrichtung)
    private float distanceX = 1000f;

    // Zielgeschwindigkeit in x-Richtung (Einheiten, z. B. mm/s)
    private float driveSpeed = 3000f;

    // Flag, das angibt, ob gerade ein Drive-Vorgang ausgeführt wird
    private bool isExecuting = false;

    /// <summary>
    /// Startet den einmaligen Drive-Vorgang: Zuerst wird in x-Richtung um 'distanceX' gefahren, dann wieder zurück zur Ausgangsposition.
    /// </summary>
    public void ActivateDrive()
    {
        if (drive != null && !isExecuting)
        {
            StartCoroutine(ActivateDriveCoroutine());
        }
        else if (drive == null)
        {
            Debug.LogWarning("Kein Drive zugewiesen!");
        }
    }

    private IEnumerator ActivateDriveCoroutine()
    {
        isExecuting = true;

        // Speichere die aktuelle Position des Drives (entspricht der Position entlang der x-Achse, wie vom Drive verwaltet)
        float originalPosition = drive.CurrentPosition;

        // Setze die gewünschte Geschwindigkeit
        drive.TargetSpeed = driveSpeed;

        // Berechne die Zielposition: Aktuelle Position + distanceX
        float targetPosition = originalPosition + distanceX;

        // Fahre zur Zielposition
        drive.DriveTo(targetPosition);
        // Warte, bis der Drive sein Ziel erreicht hat
        while (!drive.IsAtTarget)
        {
            yield return null;
        }

        // Fahre zurück zur Ausgangsposition
        drive.DriveTo(originalPosition);
        while (!drive.IsAtTarget)
        {
            yield return null;
        }

        // Stoppe den Drive (optional, falls nicht automatisch gestoppt)
        drive.Stop();

        isExecuting = false;
    }
}
