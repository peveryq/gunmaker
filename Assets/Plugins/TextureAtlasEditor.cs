using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TextureAtlasEditor : EditorWindow
{
    [System.Serializable]
    public class ModelData
    {
        public GameObject fbxModel;
        public Mesh meshAsset;
        public Rect atlasUV;
        public bool isMeshAsset = false;
    }

    private Texture2D mainAtlas;
    private List<ModelData> modelsData = new List<ModelData>();
    private Vector2 mainScrollPosition;
    private Vector2 modelsScrollPosition;
    private int atlasSize = 4096;
    private int textureSize = 512;
    private int gridSize = 8;
    
    // Общие настройки для всех элементов
    private bool overwriteAll = false;
    private int textureNumberAll = 1;
    private bool processAll = true;

    [MenuItem("Tools/Texture Atlas Editor")]
    public static void ShowWindow()
    {
        GetWindow<TextureAtlasEditor>("Texture Atlas Editor");
    }

    private void OnGUI()
    {
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
        
        GUILayout.Label("Texture Atlas Editor", EditorStyles.boldLabel);
        
        // Drag & Drop область
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop FBX Models or Mesh Assets here");
        
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        AddDraggedObject(draggedObject);
                    }
                    
                    evt.Use();
                }
                break;
        }

        EditorGUILayout.Space();
        mainAtlas = (Texture2D)EditorGUILayout.ObjectField("Main Atlas Texture", mainAtlas, typeof(Texture2D), false);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Atlas Settings", EditorStyles.boldLabel);
        atlasSize = EditorGUILayout.IntField("Atlas Size", atlasSize);
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        if (atlasSize > 0 && textureSize > 0)
        {
            gridSize = atlasSize / textureSize;
            EditorGUILayout.LabelField($"Grid Size: {gridSize}x{gridSize}", EditorStyles.helpBox);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
        
        // Общие настройки для всех элементов
        processAll = EditorGUILayout.Toggle("Process All", processAll);
        overwriteAll = EditorGUILayout.Toggle("Overwrite Originals", overwriteAll);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Texture Number:", GUILayout.Width(120));
        int newTextureNumber = EditorGUILayout.IntField(textureNumberAll, GUILayout.Width(60));
        if (newTextureNumber != textureNumberAll)
        {
            textureNumberAll = Mathf.Clamp(newTextureNumber, 1, gridSize * gridSize);
            CalculateAllUVCoordinates();
        }
        EditorGUILayout.EndHorizontal();
        
        int maxTextureNumber = gridSize * gridSize;
        if (textureNumberAll < 1 || textureNumberAll > maxTextureNumber)
        {
            EditorGUILayout.HelpBox($"Texture number must be between 1 and {maxTextureNumber}", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Models & Meshes", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear List"))
        {
            modelsData.Clear();
        }
        EditorGUILayout.EndHorizontal();

        modelsScrollPosition = EditorGUILayout.BeginScrollView(modelsScrollPosition, GUILayout.Height(300));
        for (int i = 0; i < modelsData.Count; i++)
        {
            DrawModelEntry(i);
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Calculate UV Coordinates", GUILayout.Height(30)))
        {
            CalculateAllUVCoordinates();
        }

        if (GUILayout.Button("Process Assets", GUILayout.Height(30)))
        {
            ProcessAssets();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        int assetsCount = modelsData.Count;
        EditorGUILayout.LabelField($"Assets in list: {assetsCount}");
        
        EditorGUILayout.EndScrollView();
    }

    private void AddDraggedObject(Object draggedObject)
    {
        if (draggedObject is GameObject gameObj && PrefabUtility.GetPrefabAssetType(gameObj) != PrefabAssetType.NotAPrefab)
        {
            // Проверяем, есть ли уже этот объект в списке
            if (!modelsData.Any(d => d.fbxModel == gameObj && !d.isMeshAsset))
            {
                modelsData.Add(new ModelData
                {
                    fbxModel = gameObj,
                    isMeshAsset = false
                });
                CalculateUVForModel(modelsData[modelsData.Count - 1]);
            }
        }
        else if (draggedObject is Mesh mesh)
        {
            // Проверяем, есть ли уже этот меш в списке
            if (!modelsData.Any(d => d.meshAsset == mesh && d.isMeshAsset))
            {
                modelsData.Add(new ModelData
                {
                    meshAsset = mesh,
                    isMeshAsset = true
                });
                CalculateUVForModel(modelsData[modelsData.Count - 1]);
            }
        }
    }

    private void DrawModelEntry(int index)
    {
        ModelData data = modelsData[index];
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        if (data.isMeshAsset)
        {
            EditorGUILayout.LabelField("Mesh:", EditorStyles.boldLabel, GUILayout.Width(40));
            data.meshAsset = (Mesh)EditorGUILayout.ObjectField(data.meshAsset, typeof(Mesh), false);
        }
        else
        {
            EditorGUILayout.LabelField("FBX:", EditorStyles.boldLabel, GUILayout.Width(40));
            data.fbxModel = (GameObject)EditorGUILayout.ObjectField(data.fbxModel, typeof(GameObject), false);
        }

        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            modelsData.RemoveAt(index);
            return;
        }
        EditorGUILayout.EndHorizontal();

        bool hasValidAsset = (data.isMeshAsset && data.meshAsset != null) ||
                           (!data.isMeshAsset && data.fbxModel != null);
        
        if (hasValidAsset)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UV Coordinates:", GUILayout.Width(100));
            EditorGUILayout.LabelField(
                $"{data.atlasUV.x:F3}, {data.atlasUV.y:F3} - {data.atlasUV.width:F3}, {data.atlasUV.height:F3}");
            EditorGUILayout.EndHorizontal();
            
            string assetName = data.isMeshAsset ? data.meshAsset.name : data.fbxModel.name;
            string assetType = data.isMeshAsset ? "Mesh" : "FBX Model";
            EditorGUILayout.LabelField($"{assetType}: {assetName}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void CalculateAllUVCoordinates()
    {
        if (mainAtlas == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign main atlas first!", "OK");
            return;
        }

        if (atlasSize <= 0 || textureSize <= 0)
        {
            EditorUtility.DisplayDialog("Error", "Please set valid atlas and texture sizes!", "OK");
            return;
        }

        int calculatedCount = 0;
        foreach (var data in modelsData)
        {
            bool hasValidAsset = (data.isMeshAsset && data.meshAsset != null) ||
                               (!data.isMeshAsset && data.fbxModel != null);
            if (hasValidAsset)
            {
                CalculateUVForModel(data);
                calculatedCount++;
            }
        }

        Debug.Log($"Calculated UV coordinates for {calculatedCount} assets.");
        Repaint();
    }

    private void CalculateUVForModel(ModelData data)
    {
        int textureIndex = textureNumberAll - 1;
        if (textureIndex >= 0 && textureIndex < gridSize * gridSize)
        {
            int x = textureIndex % gridSize;
            int y = textureIndex / gridSize;
            Vector2Int gridPosition = new Vector2Int(x, y);
            data.atlasUV = GetUVFromGridPosition(gridPosition);
            
            string assetName = data.isMeshAsset ? data.meshAsset.name : data.fbxModel.name;
            Debug.Log(
                $"Asset {assetName}: Texture {textureNumberAll} -> Grid ({gridPosition.x},{gridPosition.y}) -> UV ({data.atlasUV.x:F3},{data.atlasUV.y:F3})");
        }
        else
        {
            string assetName = data.isMeshAsset ? data.meshAsset.name : data.fbxModel.name;
            Debug.LogWarning($"Invalid texture number {textureNumberAll} for asset {assetName}");
        }
    }

    private Rect GetUVFromGridPosition(Vector2Int gridPos)
    {
        gridPos.x = Mathf.Clamp(gridPos.x, 0, gridSize - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, gridSize - 1);
        float cellSize = 1f / gridSize;
        return new Rect(
            Mathf.Round(gridPos.x * cellSize * 1000f) / 1000f,
            Mathf.Round((gridSize - 1 - gridPos.y) * cellSize * 1000f) / 1000f,
            Mathf.Round(cellSize * 1000f) / 1000f,
            Mathf.Round(cellSize * 1000f) / 1000f
        );
    }

    private void ProcessAssets()
    {
        if (mainAtlas == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign main atlas first!", "OK");
            return;
        }

        int processedCount = 0;
        int errorCount = 0;
        
        foreach (var data in modelsData)
        {
            if (processAll)
            {
                bool hasValidAsset = (data.isMeshAsset && data.meshAsset != null) ||
                                   (!data.isMeshAsset && data.fbxModel != null);
                if (hasValidAsset)
                {
                    if (ProcessSingleAsset(data))
                    {
                        processedCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
            }
        }

        Debug.Log($"Processed {processedCount} assets, {errorCount} errors");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Complete",
            $"Processed {processedCount} assets successfully!\n{errorCount} errors occurred.",
            "OK");
    }

    private bool ProcessSingleAsset(ModelData data)
    {
        try
        {
            if (data.isMeshAsset)
            {
                return ProcessMeshAsset(data);
            }
            else
            {
                return ProcessFBXModel(data);
            }
        }
        catch (System.Exception e)
        {
            string assetName = data.isMeshAsset ? data.meshAsset.name : data.fbxModel.name;
            Debug.LogError($"Error processing {assetName}: {e.Message}");
            return false;
        }
    }

    private bool ProcessMeshAsset(ModelData data)
    {
        Mesh originalMesh = data.meshAsset;
        Mesh targetMesh = overwriteAll ? originalMesh : Instantiate(originalMesh);
        targetMesh.name = overwriteAll ? originalMesh.name : $"{originalMesh.name}_Atlas";

        ApplyAtlasUV(targetMesh, data.atlasUV);

        if (!overwriteAll)
        {
            string originalPath = AssetDatabase.GetAssetPath(originalMesh);
            string directory = System.IO.Path.GetDirectoryName(originalPath);
            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{targetMesh.name}.asset");
            AssetDatabase.CreateAsset(targetMesh, newPath);
        }

        Debug.Log($"Successfully processed mesh: {targetMesh.name} with UV: {data.atlasUV}");
        return true;
    }

    private bool ProcessFBXModel(ModelData data)
    {
        string modelPath = AssetDatabase.GetAssetPath(data.fbxModel);
        Mesh originalMesh = GetModelMesh(data.fbxModel);

        if (originalMesh == null)
        {
            Debug.LogWarning($"No mesh found in {data.fbxModel.name}");
            return false;
        }

        Mesh newMesh = Instantiate(originalMesh);
        newMesh.name = $"{originalMesh.name}_Atlas";
        ApplyAtlasUV(newMesh, data.atlasUV);

        string directory = System.IO.Path.GetDirectoryName(modelPath);
        string newMeshPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{newMesh.name}.asset");
        AssetDatabase.CreateAsset(newMesh, newMeshPath);

        GameObject prefabInstance = PrefabUtility.InstantiatePrefab(data.fbxModel) as GameObject;
        SetModelMesh(prefabInstance, newMesh);
        ApplyAtlasMaterial(prefabInstance);

        string newPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{data.fbxModel.name}_Atlas.prefab");
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, newPrefabPath);
        Object.DestroyImmediate(prefabInstance);

        Debug.Log($"Successfully processed FBX: {data.fbxModel.name} with UV: {data.atlasUV}");
        return true;
    }

    private Mesh GetModelMesh(GameObject model)
    {
        MeshFilter meshFilter = model.GetComponentInChildren<MeshFilter>();
        SkinnedMeshRenderer skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
        if (meshFilter != null) return meshFilter.sharedMesh;
        if (skinnedRenderer != null) return skinnedRenderer.sharedMesh;
        return null;
    }

    private void SetModelMesh(GameObject model, Mesh newMesh)
    {
        MeshFilter meshFilter = model.GetComponentInChildren<MeshFilter>();
        SkinnedMeshRenderer skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
        if (meshFilter != null) meshFilter.sharedMesh = newMesh;
        if (skinnedRenderer != null) skinnedRenderer.sharedMesh = newMesh;
    }

    private void ApplyAtlasUV(Mesh mesh, Rect uvCoords)
    {
        Vector2[] uvs = mesh.uv;
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(
                Mathf.Round((uvCoords.x + uvs[i].x * uvCoords.width) * 1000f) / 1000f,
                Mathf.Round((uvCoords.y + uvs[i].y * uvCoords.height) * 1000f) / 1000f
            );
        }

        mesh.uv = uvs;
    }

    private void ApplyAtlasMaterial(GameObject model)
    {
        Renderer renderer = model.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            Material newMaterial = new Material(renderer.sharedMaterial);
            newMaterial.mainTexture = mainAtlas;
            if (newMaterial.HasProperty("_MainTex_ST"))
            {
                Vector4 st = newMaterial.GetVector("_MainTex_ST");
                st.z = 1f;
                st.w = 1f;
                newMaterial.SetVector("_MainTex_ST", st);
            }

            string materialPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/{newMaterial.name}_Atlas.mat");
            AssetDatabase.CreateAsset(newMaterial, materialPath);
            renderer.sharedMaterial = newMaterial;
        }
    }
}