using UnityEngine;

public class CylinderSpawner : MonoBehaviour
{
    public GameObject cylinderPrefab;
    public Vector3 spawnPosition = new Vector3(1.39600003f, 1, -7.55499983f);

    private Color[] colors = new Color[] { Color.red, Color.green, Color.blue };
    private int colorIndex = 0;

    // Diese Methode kann jetzt extern aufgerufen werden
    public void SpawnCylinder()
    {
        Debug.Log("[Spawner] Neuer Zylinder wird gespawnt...");

        GameObject cylinder = Instantiate(cylinderPrefab, spawnPosition, Quaternion.identity);

        Renderer cylinderRenderer = cylinder.GetComponent<Renderer>();
        cylinderRenderer.material.color = colors[colorIndex];

        colorIndex = (colorIndex + 1) % colors.Length;
    }
}
