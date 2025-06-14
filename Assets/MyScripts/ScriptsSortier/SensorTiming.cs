using UnityEngine;

public class ColorSensorController : MonoBehaviour
{
    public float rayLength = 10f;
    public LayerMask detectionLayer;

    public GameObject redGroup;
    public GameObject whiteGroup;
    public GameObject blueGroup;

    public float redDelay = 2f;
    public float whiteDelay = 1f;
    public float blueDelay = 3f;

    private bool isActivated = false;
    private float cooldown = 2f;

    void Update()
    {
        if (isActivated) return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Debug.DrawRay(transform.position, transform.forward * rayLength, Color.yellow);

        if (Physics.Raycast(ray, out hit, rayLength, detectionLayer))
        {
            Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
            if (hitRenderer != null)
            {
                Color detectedColor = hitRenderer.material.color;
                Debug.Log($"[Sensor] Farbe erkannt: {detectedColor}");

                if (IsColorNear(detectedColor, Color.red))
                {
                    Debug.Log("[Sensor] Rot erkannt – starte Gruppe Rot nach " + redDelay + " Sekunden");
                    StartCoroutine(ActivateAfterDelay(redGroup, redDelay, "Rot"));
                }
                else if (IsColorNear(detectedColor, Color.white))
                {
                    Debug.Log("[Sensor] Weiß erkannt – starte Gruppe Weiß nach " + whiteDelay + " Sekunden");
                    StartCoroutine(ActivateAfterDelay(whiteGroup, whiteDelay, "Weiß"));
                }
                else if (IsColorNear(detectedColor, Color.blue))
                {
                    Debug.Log("[Sensor] Blau erkannt – starte Gruppe Blau nach " + blueDelay + " Sekunden");
                    StartCoroutine(ActivateAfterDelay(blueGroup, blueDelay, "Blau"));
                }
                else
                {
                    Debug.Log("[Sensor] Keine passende Farbe erkannt.");
                }
            }
        }
    }

    private System.Collections.IEnumerator ActivateAfterDelay(GameObject group, float delay, string colorName)
    {
        isActivated = true;
        Debug.Log($"[Sensor] Warte {delay} Sekunden vor der Bewegung für {colorName}...");

        yield return new WaitForSeconds(delay);

        var mover = group.GetComponent<Mover>();
        if (mover != null)
        {
            Debug.Log($"[Sensor] Bewegung gestartet für Gruppe {colorName}.");
            mover.StartMoving();
        }
        else
        {
            Debug.LogWarning($"[Sensor] Kein Mover-Skript an Gruppe {colorName} gefunden!");
        }

        yield return new WaitForSeconds(cooldown);
        isActivated = false;
        Debug.Log("[Sensor] Sensor reaktiviert.");
    }

    bool IsColorNear(Color actual, Color target, float tolerance = 0.25f)
    {
        return Mathf.Abs(actual.r - target.r) < tolerance &&
               Mathf.Abs(actual.g - target.g) < tolerance &&
               Mathf.Abs(actual.b - target.b) < tolerance;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * rayLength);
    }
}
