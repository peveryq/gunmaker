using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool for remapping UV coordinates of 3D models to match texture atlas slots
/// Converts full UV range [0,1] to the appropriate sub-rectangle for the selected atlas slot
/// </summary>
public class UVRemapper : EditorWindow
{
    private GameObject targetModel;
    private int textureCount = 4; // Must match atlas configuration
    private int textureID = 0; // ID of the texture slot in atlas (0-based, left-to-right, top-to-bottom)
    private bool applyToAllMeshes = true;
    private bool createBackup = true;
    
    private Vector2 scrollPosition;
    
    private const int MIN_TEXTURE_COUNT = 4;
    private const int MAX_TEXTURE_COUNT = 64;
    
    [MenuItem("Tools/Gunmaker/UV Remapper")]
    public static void ShowWindow()
    {
        GetWindow<UVRemapper>("UV Remapper");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("UV Remapper", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "This tool remaps UV coordinates of 3D models to match texture atlas slots.\n\n" +
            "The model's UV coordinates (originally [0,1]) will be scaled and offset to match the selected atlas slot.\n\n" +
            "Note: For imported meshes (.fbx, .obj, etc.), a new .asset file will be created next to the original file.",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Model selection
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
        
        targetModel = (GameObject)EditorGUILayout.ObjectField(
            "Target Model",
            targetModel,
            typeof(GameObject),
            true
        );
        
        applyToAllMeshes = EditorGUILayout.Toggle("Apply to All Meshes", applyToAllMeshes);
        createBackup = EditorGUILayout.Toggle("Create Backup", createBackup);
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Atlas settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Atlas Settings", EditorStyles.boldLabel);
        
        int newCount = EditorGUILayout.IntField("Texture Count (multiple of 4)", textureCount);
        if (newCount != textureCount)
        {
            if (newCount < MIN_TEXTURE_COUNT)
                newCount = MIN_TEXTURE_COUNT;
            else if (newCount > MAX_TEXTURE_COUNT)
                newCount = MAX_TEXTURE_COUNT;
            
            newCount = (newCount / 4) * 4;
            if (newCount < MIN_TEXTURE_COUNT)
                newCount = MIN_TEXTURE_COUNT;
            
            textureCount = newCount;
            
            // Clamp texture ID to valid range
            if (textureID >= textureCount)
                textureID = textureCount - 1;
        }
        
        int gridSize = Mathf.RoundToInt(Mathf.Sqrt(textureCount));
        EditorGUILayout.LabelField($"Grid: {gridSize}x{gridSize} ({textureCount} slots)");
        
        EditorGUILayout.Space(5);
        
        // Texture ID selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Texture ID:", GUILayout.Width(100));
        textureID = EditorGUILayout.IntSlider(textureID, 0, textureCount - 1);
        EditorGUILayout.EndHorizontal();
        
        // Show position info
        int row = textureID / gridSize;
        int col = textureID % gridSize;
        EditorGUILayout.LabelField($"Slot Position: Row {row}, Column {col}");
        
        // Calculate UV bounds
        float slotWidth = 1f / gridSize;
        float slotHeight = 1f / gridSize;
        float uMin = col * slotWidth;
        float uMax = (col + 1) * slotWidth;
        float vMin = 1f - (row + 1) * slotHeight; // Flip Y for Unity
        float vMax = 1f - row * slotHeight;
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("UV Bounds:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  U: [{uMin:F3}, {uMax:F3}]");
        EditorGUILayout.LabelField($"  V: [{vMin:F3}, {vMax:F3}]");
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Remap button
        EditorGUI.BeginDisabledGroup(targetModel == null);
        
        if (GUILayout.Button("Remap UV Coordinates", GUILayout.Height(30)))
        {
            RemapUVs();
        }
        
        EditorGUI.EndDisabledGroup();
        
        if (targetModel == null)
        {
            EditorGUILayout.HelpBox("Please assign a Target Model first.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // Info section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("How it works:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "1. Original UV coordinates are in range [0, 1] (full texture)\n" +
            "2. These are scaled and offset to match the selected atlas slot\n" +
            "3. For example, if slot is at position (1, 0) in a 2x2 grid:\n" +
            "   - U coordinates are scaled by 0.5 and offset by 0.5\n" +
            "   - V coordinates are scaled by 0.5 and offset by 0.0",
            EditorStyles.wordWrappedLabel
        );
        EditorGUILayout.EndVertical();
    }
    
    private void RemapUVs()
    {
        if (targetModel == null)
        {
            EditorUtility.DisplayDialog("Error", "Target Model is not assigned!", "OK");
            return;
        }
        
        if (textureID < 0 || textureID >= textureCount)
        {
            EditorUtility.DisplayDialog("Error", $"Texture ID must be between 0 and {textureCount - 1}!", "OK");
            return;
        }
        
        // Calculate grid dimensions
        int gridSize = Mathf.RoundToInt(Mathf.Sqrt(textureCount));
        float slotWidth = 1f / gridSize;
        float slotHeight = 1f / gridSize;
        
        // Calculate slot position
        int row = textureID / gridSize;
        int col = textureID % gridSize;
        
        // Calculate UV offset and scale
        float uOffset = col * slotWidth;
        float vOffset = 1f - (row + 1) * slotHeight; // Flip Y for Unity
        
        Debug.Log($"UVRemapper: ===== Starting UV Remapping =====");
        Debug.Log($"UVRemapper: Texture ID: {textureID}");
        Debug.Log($"UVRemapper: Grid: {gridSize}x{gridSize} ({textureCount} slots)");
        Debug.Log($"UVRemapper: Slot position: Row {row}, Column {col}");
        Debug.Log($"UVRemapper: Slot size: {slotWidth:F4} x {slotHeight:F4}");
        Debug.Log($"UVRemapper: UV Offset: U={uOffset:F4}, V={vOffset:F4}");
        Debug.Log($"UVRemapper: Target UV range: U[{uOffset:F4}, {uOffset + slotWidth:F4}], V[{vOffset:F4}, {vOffset + slotHeight:F4}]");
        
        // Collect all meshes to process
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        
        if (applyToAllMeshes)
        {
            meshFilters.AddRange(targetModel.GetComponentsInChildren<MeshFilter>(true));
        }
        else
        {
            MeshFilter mf = targetModel.GetComponent<MeshFilter>();
            if (mf != null)
                meshFilters.Add(mf);
        }
        
        if (meshFilters.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No MeshFilter components found on the target model!", "OK");
            return;
        }
        
        int processedCount = 0;
        
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"UVRemapper: MeshFilter on {meshFilter.name} has no mesh, skipping...");
                continue;
            }
            
            Mesh originalMesh = meshFilter.sharedMesh;
            
            // Check if mesh is an asset or instance
            bool isAsset = AssetDatabase.Contains(originalMesh);
            
            // Register undo
            Undo.RecordObject(meshFilter, "Remap UV Coordinates");
            if (isAsset)
            {
                Undo.RegisterCompleteObjectUndo(originalMesh, "Remap UV Coordinates");
            }
            
            // Get original UVs (need to make a copy since we can't modify sharedMesh directly)
            Vector2[] originalUVs = originalMesh.uv;
            if (originalUVs == null || originalUVs.Length == 0)
            {
                Debug.LogWarning($"UVRemapper: Mesh {originalMesh.name} has no UV coordinates, skipping...");
                continue;
            }
            
            // Log original UV range for debugging
            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;
            foreach (Vector2 uv in originalUVs)
            {
                minU = Mathf.Min(minU, uv.x);
                maxU = Mathf.Max(maxU, uv.x);
                minV = Mathf.Min(minV, uv.y);
                maxV = Mathf.Max(maxV, uv.y);
            }
            Debug.Log($"UVRemapper: Original UV range - U: [{minU:F3}, {maxU:F3}], V: [{minV:F3}, {maxV:F3}]");
            
            // Create new mesh (we always need a new instance to modify)
            Mesh newMesh = new Mesh();
            newMesh.name = originalMesh.name;
            
            // Copy all mesh data
            newMesh.vertices = originalMesh.vertices;
            newMesh.triangles = originalMesh.triangles;
            newMesh.normals = originalMesh.normals;
            newMesh.tangents = originalMesh.tangents;
            newMesh.colors = originalMesh.colors;
            newMesh.uv2 = originalMesh.uv2;
            newMesh.uv3 = originalMesh.uv3;
            newMesh.uv4 = originalMesh.uv4;
            newMesh.bounds = originalMesh.bounds;
            
            // Remap UVs
            Vector2[] remappedUVs = new Vector2[originalUVs.Length];
            for (int i = 0; i < originalUVs.Length; i++)
            {
                // Scale original UV [0,1] to slot size, then offset to slot position
                remappedUVs[i] = new Vector2(
                    originalUVs[i].x * slotWidth + uOffset,
                    originalUVs[i].y * slotHeight + vOffset
                );
            }
            
            // Apply remapped UVs
            newMesh.uv = remappedUVs;
            
            // Log remapped UV range for debugging
            minU = float.MaxValue; maxU = float.MinValue;
            minV = float.MaxValue; maxV = float.MinValue;
            foreach (Vector2 uv in remappedUVs)
            {
                minU = Mathf.Min(minU, uv.x);
                maxU = Mathf.Max(maxU, uv.x);
                minV = Mathf.Min(minV, uv.y);
                maxV = Mathf.Max(maxV, uv.y);
            }
            Debug.Log($"UVRemapper: Remapped UV range - U: [{minU:F3}, {maxU:F3}], V: [{minV:F3}, {maxV:F3}]");
            
            if (isAsset)
            {
                string assetPath = AssetDatabase.GetAssetPath(originalMesh);
                string extension = Path.GetExtension(assetPath).ToLower();
                
                // Check if this is an imported mesh from .fbx or other model file
                bool isImportedMesh = extension == ".fbx" || extension == ".obj" || extension == ".dae" || 
                                     extension == ".3ds" || extension == ".blend" || extension == ".max";
                
                if (isImportedMesh)
                {
                    // For imported meshes (.fbx, etc.), we cannot modify the source file
                    // Instead, create a new .asset file next to the original
                    string directory = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                    string fileName = Path.GetFileNameWithoutExtension(assetPath);
                    string newAssetPath = $"{directory}/{fileName}_remapped_uv{textureID}.asset";
                    
                    // Check if file already exists
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(newAssetPath) != null)
                    {
                        // Update existing asset
                        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(newAssetPath);
                        EditorUtility.CopySerialized(newMesh, existingMesh);
                        EditorUtility.SetDirty(existingMesh);
                        AssetDatabase.SaveAssets();
                        meshFilter.sharedMesh = existingMesh;
                        Debug.Log($"UVRemapper: Updated existing remapped mesh: {newAssetPath}");
                    }
                    else
                    {
                        // Create new asset
                        AssetDatabase.CreateAsset(newMesh, newAssetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        
                        Mesh reloadedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(newAssetPath);
                        if (reloadedMesh != null)
                        {
                            meshFilter.sharedMesh = reloadedMesh;
                            Debug.Log($"UVRemapper: Created new remapped mesh asset: {newAssetPath}");
                        }
                        else
                        {
                            Debug.LogError($"UVRemapper: Failed to create mesh asset: {newAssetPath}");
                            meshFilter.sharedMesh = newMesh; // Fallback to instance
                        }
                    }
                }
                else
                {
                    // For .asset meshes, we can replace the original
                    if (createBackup)
                    {
                        string backupPath = assetPath.Replace(".asset", "_backup.asset");
                        if (!AssetDatabase.LoadAssetAtPath<Mesh>(backupPath))
                        {
                            AssetDatabase.CopyAsset(assetPath, backupPath);
                            Debug.Log($"UVRemapper: Created backup: {backupPath}");
                        }
                    }
                    
                    // Replace the asset file
                    bool wasDeleted = AssetDatabase.DeleteAsset(assetPath);
                    if (wasDeleted)
                    {
                        AssetDatabase.CreateAsset(newMesh, assetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        
                        Mesh reloadedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                        if (reloadedMesh != null)
                        {
                            meshFilter.sharedMesh = reloadedMesh;
                            Debug.Log($"UVRemapper: Successfully replaced mesh asset: {assetPath}");
                        }
                        else
                        {
                            Debug.LogError($"UVRemapper: Failed to reload mesh from {assetPath}");
                            meshFilter.sharedMesh = newMesh; // Fallback to instance
                        }
                    }
                    else
                    {
                        Debug.LogError($"UVRemapper: Failed to delete original mesh asset: {assetPath}");
                        meshFilter.sharedMesh = newMesh; // Fallback to instance
                    }
                }
            }
            else
            {
                // For instance meshes, just replace the reference
                meshFilter.sharedMesh = newMesh;
                Debug.Log($"UVRemapper: Replaced instance mesh on {meshFilter.name}");
            }
            
            // Verify the changes were applied
            Mesh finalMesh = meshFilter.sharedMesh;
            if (finalMesh != null)
            {
                Vector2[] finalUVs = finalMesh.uv;
                if (finalUVs != null && finalUVs.Length > 0)
                {
                    float finalMinU = float.MaxValue, finalMaxU = float.MinValue;
                    float finalMinV = float.MaxValue, finalMaxV = float.MinValue;
                    foreach (Vector2 uv in finalUVs)
                    {
                        finalMinU = Mathf.Min(finalMinU, uv.x);
                        finalMaxU = Mathf.Max(finalMaxU, uv.x);
                        finalMinV = Mathf.Min(finalMinV, uv.y);
                        finalMaxV = Mathf.Max(finalMaxV, uv.y);
                    }
                    Debug.Log($"UVRemapper: Final UV range - U: [{finalMinU:F3}, {finalMaxU:F3}], V: [{finalMinV:F3}, {finalMaxV:F3}]");
                    
                    // Verify UVs are in expected range
                    float expectedUMin = uOffset;
                    float expectedUMax = uOffset + slotWidth;
                    float expectedVMin = vOffset;
                    float expectedVMax = vOffset + slotHeight;
                    
                    if (Mathf.Abs(finalMinU - expectedUMin) > 0.01f || Mathf.Abs(finalMaxU - expectedUMax) > 0.01f ||
                        Mathf.Abs(finalMinV - expectedVMin) > 0.01f || Mathf.Abs(finalMaxV - expectedVMax) > 0.01f)
                    {
                        Debug.LogWarning($"UVRemapper: UV range mismatch! Expected U[{expectedUMin:F3}, {expectedUMax:F3}], V[{expectedVMin:F3}, {expectedVMax:F3}]");
                    }
                    else
                    {
                        Debug.Log($"UVRemapper: ✓ UV range matches expected values");
                    }
                }
            }
            
            processedCount++;
            Debug.Log($"UVRemapper: Remapped UVs for mesh: {originalMesh.name} on {meshFilter.name}");
        }
        
        // Force scene update
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
        
        EditorUtility.DisplayDialog(
            "UV Remapping Complete",
            $"Successfully remapped UV coordinates for {processedCount} mesh(es).\n\n" +
            $"Texture ID: {textureID} (Row {row}, Column {col})\n" +
            $"UV Range: U[{uOffset:F3}, {uOffset + slotWidth:F3}], V[{vOffset:F3}, {vOffset + slotHeight:F3}]\n\n" +
            $"Check Console for detailed logs.",
            "OK"
        );
        
        Debug.Log($"UVRemapper: ===== Completed remapping for {processedCount} mesh(es) =====");
        
        // Select the target model to see changes
        if (targetModel != null)
        {
            Selection.activeGameObject = targetModel;
        }
    }
}

