using UnityEngine;
using UnityEditor;
using System.IO;

public class MeshExtractor : MonoBehaviour
{
    [MenuItem("Tools/Save All Meshes In Scene")]
    static void SaveAllMeshes()
    {
        string folderPath = "Assets/SavedMeshes";
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        int counter = 0;

        foreach (MeshFilter mf in FindObjectsOfType<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;

            // Verhindere Duplikate
            string meshName = mf.sharedMesh.name;
            string assetPath = $"{folderPath}/{meshName}_{counter}.asset";

            if (!AssetDatabase.Contains(mf.sharedMesh))
            {
                Mesh newMesh = Object.Instantiate(mf.sharedMesh);
                AssetDatabase.CreateAsset(newMesh, assetPath);
                AssetDatabase.SaveAssets();

                mf.sharedMesh = newMesh;
                Debug.Log($"Saved mesh: {assetPath}");

                counter++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Fertig! {counter} Meshes gespeichert und ersetzt.");
    }
}
