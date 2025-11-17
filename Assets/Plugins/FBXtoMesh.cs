using UnityEngine;
using UnityEditor;

public class BatchMeshExporter : MonoBehaviour
{
    [MenuItem("Tools/Export Selected Meshes")]
    static void ExportSelectedMeshes()
    {
        // Получаем все выделенные объекты в Project
        Object[] selections = Selection.objects;

        foreach (Object obj in selections)
        {
            GameObject go = obj as GameObject;
            if (go == null) continue;

            // Проверяем MeshFilter
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Mesh mesh = mf.sharedMesh;
                string path = "Assets/_InternalAssets/Meshes/" + mesh.name + ".mesh";
                AssetDatabase.CreateAsset(Object.Instantiate(mesh), path);
                Debug.Log("Mesh saved: " + path);
            }

            // Проверяем SkinnedMeshRenderer
            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                Mesh mesh = smr.sharedMesh;
                string path = "Assets/_InternalAssets/Meshes/" + mesh.name + ".mesh";
                AssetDatabase.CreateAsset(Object.Instantiate(mesh), path);
                Debug.Log("Skinned mesh saved: " + path);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("All selected meshes exported!");
    }
}
