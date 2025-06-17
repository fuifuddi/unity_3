using UnityEditor;
using UnityEngine;

public class FixMeshColliders : MonoBehaviour
{
    [MenuItem("Tools/Fix Missing MeshColliders")]
    static void FixAllMeshColliders()
    {
        int count = 0;

        foreach (MeshCollider collider in FindObjectsOfType<MeshCollider>())
        {
            MeshFilter mf = collider.GetComponent<MeshFilter>();
            if (collider.sharedMesh == null && mf != null && mf.sharedMesh != null)
            {
                collider.sharedMesh = mf.sharedMesh;
                count++;
            }
        }

        Debug.Log($"✅ {count} MeshCollider(s) repariert.");
    }
}
