using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dieses Skript übernimmt dieselbe Aufgabe wie das Game4Automation-TransportSurface,
/// jedoch nur mit Unity-Standard-Collisionen. 
/// Es erkennt alle Objekte mit Layer "g4a MU", die auf dem Fließband liegen,
/// und schiebt sie gleichmäßig in eine definierbare Richtung mit konstanter Geschwindigkeit.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class TransportSurfaceSimple : MonoBehaviour
{
    [Header("Fließband‐Einstellungen")]
    [Tooltip("Richtung, in die Objekte transportiert werden (im lokalen Raum des Fließbands).")]
    public Vector3 transportDirection = Vector3.forward;

    [Tooltip("Geschwindigkeit, mit der das Fließband Objekte bewegt (Einheiten pro Sekunde).")]
    public float speed = 1.0f;

    [Tooltip("Name des Layers, dessen Objekte transportiert werden (z. B. 'g4a MU').")]
    public string targetLayerName = "g4a MU";

    // Interne Liste der aktuell auf dem Band liegenden Rigidbody-Objekte
    private readonly List<Rigidbody> _loadedRigidbodies = new List<Rigidbody>();

    // Interner Layer-Index (wird im Awake geholt)
    private int _targetLayer;

    private void Awake()
    {
        // 1) Den Layer-Index aus dem Layer-Namen ermitteln
        _targetLayer = LayerMask.NameToLayer(targetLayerName);
        if (_targetLayer < 0)
        {
            Debug.LogWarning($"[TransportSurfaceSimple] Layer '{targetLayerName}' existiert nicht oder falsch geschrieben!");
        }

        // 2) BoxCollider konfigurieren: Er soll KEIN Trigger sein
        var bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            bc.isTrigger = false;
        }
        else
        {
            Debug.LogError($"[TransportSurfaceSimple] Kein BoxCollider gefunden auf {name}!");
        }

        // 3) Rigidbody konfigurieren: Band bleibt statisch, wird nicht von Physik beeinflusst
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            Debug.LogError($"[TransportSurfaceSimple] Kein Rigidbody gefunden auf {name}!");
        }

        // 4) Normiere die transportDirection, damit nur die Richtung zählt
        if (transportDirection == Vector3.zero)
            transportDirection = Vector3.forward;
        else
            transportDirection = transportDirection.normalized;
    }

    // Wenn ein Rigidbody-Kollision mit dem Band beginnt, prüfen wir, ob es auf dem richtigen Layer ist.
    private void OnCollisionEnter(Collision collision)
    {
        // Nur Objekte mit exakt diesem Layer bearbeiten
        if (collision.gameObject.layer != _targetLayer)
            return;

        // Versuche, das Rigidbody des anderen Objekts zu bekommen
        var otherRb = collision.rigidbody;
        if (otherRb != null && !_loadedRigidbodies.Contains(otherRb))
        {
            _loadedRigidbodies.Add(otherRb);
            // Optional: Debug-Log
            // Debug.Log($"[TransportSurfaceSimple] Objekt '{otherRb.name}' auf Band übernommen.");
        }
    }

    // Wenn der Kontakt endet, entfernen wir das Objekt aus der Liste
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer != _targetLayer)
            return;

        var otherRb = collision.rigidbody;
        if (otherRb != null && _loadedRigidbodies.Contains(otherRb))
        {
            _loadedRigidbodies.Remove(otherRb);
            // Optional: Debug-Log
            // Debug.Log($"[TransportSurfaceSimple] Objekt '{otherRb.name}' verlassen.");
        }
    }

    // In FixedUpdate werden alle geladenen Objekte fortbewegt
    private void FixedUpdate()
    {
        if (speed == 0f || _loadedRigidbodies.Count == 0)
            return;

        // Für jedes geladene Rigidbody wende MovePosition an
        // So wird es entlang der lokalen transportDirection bewegt
        foreach (var rb in _loadedRigidbodies)
        {
            if (rb == null)
                continue;

            // Berechne Welt-Richtung: lokale Richtungs-Vektoren des Bands
            Vector3 worldDir = transform.TransformDirection(transportDirection);

            // Neue Position per MovePosition setzen (Respektiert Physik-Kollisionen)
            Vector3 newPos = rb.position + worldDir * speed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    // Im Editor: Wenn man transportDirection ändert, immer normalisieren
    private void OnValidate()
    {
        if (transportDirection == Vector3.zero)
        {
            transportDirection = Vector3.forward;
        }
        else
        {
            transportDirection = transportDirection.normalized;
        }
    }
}
