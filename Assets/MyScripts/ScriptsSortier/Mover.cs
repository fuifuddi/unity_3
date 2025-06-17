using System.Collections;
using UnityEngine;

public class Mover : MonoBehaviour
{
    private Vector3 moveOffset = Vector3.back;
    private float moveDuration = 0.25f;

    private bool isMoving = false;
    private Vector3 startPosition;

    public void StartMoving()
    {
        if (!isMoving)
        {
            Debug.Log($"[Mover:{gameObject.name}] Bewegung gestartet.");
            startPosition = transform.position;
            StartCoroutine(MoveForwardAndBack());
        }
        else
        {
            Debug.Log($"[Mover:{gameObject.name}] Bewegung bereits aktiv – Start ignoriert.");
        }
    }

    private IEnumerator MoveForwardAndBack()
    {
        isMoving = true;
        Vector3 targetPosition = startPosition + moveOffset;

        // Vorwärts
        float elapsed = 0f;
        Debug.Log($"[Mover:{gameObject.name}] Vorwärtsbewegung beginnt zu {targetPosition}");
        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;

        // Rückwärts
        elapsed = 0f;
        Debug.Log($"[Mover:{gameObject.name}] Rückwärtsbewegung beginnt zurück zu {startPosition}");
        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(targetPosition, startPosition, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPosition;

        isMoving = false;
        Debug.Log($"[Mover:{gameObject.name}] Bewegung abgeschlossen.");
    }
}
