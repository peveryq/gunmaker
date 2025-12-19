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
    private Mesh targetMesh; // Direct mesh selection (alternative to GameObject)
    private int textureCount = 4; // Must match atlas configuration
    private int textureID = 0; // ID of the texture slot in atlas (0-based, left-to-right, top-to-bottom)
    private bool applyToAllMeshes = true;
    private bool createBackup = true;
    private bool reverseMode = false; // If true, remaps UV from atlas slot back to full [0,1] range
    
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
        
        // Add scroll view for content that doesn't fit
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Model/Mesh selection
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Target Selection", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("Select either GameObject or Mesh:", EditorStyles.miniLabel);
        
        targetModel = (GameObject)EditorGUILayout.ObjectField(
            "Target GameObject (optional)",
            targetModel,
            typeof(GameObject),
            true
        );
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("OR", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(5);
        
        targetMesh = (Mesh)EditorGUILayout.ObjectField(
            "Target Mesh (optional)",
            targetMesh,
            typeof(Mesh),
            false
        );
        
        // Clear one when the other is set
        if (targetModel != null && targetMesh != null)
        {
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.ExecuteCommand)
            {
                // Determine which was just set (this is approximate, but works for most cases)
                if (GUI.GetNameOfFocusedControl() == "Target GameObject (optional)")
                {
                    targetMesh = null;
                }
                else if (GUI.GetNameOfFocusedControl() == "Target Mesh (optional)")
                {
                    targetModel = null;
                }
            }
        }
        
        EditorGUILayout.Space(5);
        
        // Show which mode is active
        if (targetMesh != null)
        {
            EditorGUILayout.HelpBox($"Processing single mesh: {targetMesh.name}", MessageType.Info);
            applyToAllMeshes = false; // Disable when mesh is selected
        }
        else if (targetModel != null)
        {
            EditorGUILayout.HelpBox($"Processing GameObject: {targetModel.name}", MessageType.Info);
            applyToAllMeshes = EditorGUILayout.Toggle("Apply to All Meshes", applyToAllMeshes);
        }
        
        createBackup = EditorGUILayout.Toggle("Create Backup", createBackup);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);
        
        // Reverse mode toggle
        reverseMode = EditorGUILayout.Toggle("Reverse Mode", reverseMode);
        if (reverseMode)
        {
            EditorGUILayout.HelpBox(
                "Reverse Mode: Remaps UV coordinates from atlas slot back to full [0,1] range.\n" +
                "Use this to restore original UV mapping after remapping to atlas slot.",
                MessageType.Info
            );
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Atlas settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Atlas Settings", EditorStyles.boldLabel);
        
        // Disable texture ID selection in reverse mode (not needed)
        EditorGUI.BeginDisabledGroup(reverseMode);
        
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
        
        EditorGUI.EndDisabledGroup();
        
        if (reverseMode)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "In Reverse Mode, the tool will automatically detect which atlas slot the UVs are currently in and remap them back to [0,1] range.",
                MessageType.Info
            );
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Remap button
        EditorGUI.BeginDisabledGroup(targetModel == null && targetMesh == null);
        
        if (GUILayout.Button("Remap UV Coordinates", GUILayout.Height(30)))
        {
            RemapUVs();
        }
        
        EditorGUI.EndDisabledGroup();
        
        if (targetModel == null && targetMesh == null)
        {
            EditorGUILayout.HelpBox("Please assign either a Target GameObject or Target Mesh first.", MessageType.Warning);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void RemapUVs()
    {
        if (targetModel == null && targetMesh == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign either a Target GameObject or Target Mesh!", "OK");
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
        
        // Collect meshes to process
        List<Mesh> meshesToProcess = new List<Mesh>();
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        
        if (targetMesh != null)
        {
            // Direct mesh mode - process only the selected mesh
            meshesToProcess.Add(targetMesh);
            Debug.Log($"UVRemapper: Processing direct mesh: {targetMesh.name}");
        }
        else if (targetModel != null)
        {
            // GameObject mode - collect mesh filters
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
            
            // Extract meshes from filters
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    meshesToProcess.Add(mf.sharedMesh);
                }
            }
        }
        
        if (meshesToProcess.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No meshes found to process!", "OK");
            return;
        }
        
        int processedCount = 0;
        
        for (int meshIndex = 0; meshIndex < meshesToProcess.Count; meshIndex++)
        {
            Mesh originalMesh = meshesToProcess[meshIndex];
            MeshFilter meshFilter = null;
            
            // Try to find corresponding MeshFilter if we're in GameObject mode
            if (targetModel != null && meshIndex < meshFilters.Count)
            {
                meshFilter = meshFilters[meshIndex];
            }
            
            // Check if mesh is an asset or instance
            bool isAsset = AssetDatabase.Contains(originalMesh);
            
            // Register undo
            if (meshFilter != null)
            {
                Undo.RecordObject(meshFilter, "Remap UV Coordinates");
            }
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
            
            if (reverseMode)
            {
                // Reverse mode: remap from atlas slot back to full [0,1] range
                // First, try to detect which slot the UVs are currently in
                // Then apply inverse transformation: (uv - offset) / slotSize
                
                // Detect current slot from UV coordinates
                float avgU = 0f, avgV = 0f;
                foreach (Vector2 uv in originalUVs)
                {
                    avgU += uv.x;
                    avgV += uv.y;
                }
                avgU /= originalUVs.Length;
                avgV /= originalUVs.Length;
                
                // Find which slot contains the average UV
                int detectedRow = Mathf.FloorToInt((1f - avgV) / slotHeight);
                int detectedCol = Mathf.FloorToInt(avgU / slotWidth);
                detectedRow = Mathf.Clamp(detectedRow, 0, gridSize - 1);
                detectedCol = Mathf.Clamp(detectedCol, 0, gridSize - 1);
                
                float detectedUOffset = detectedCol * slotWidth;
                float detectedVOffset = 1f - (detectedRow + 1) * slotHeight;
                
                Debug.Log($"UVRemapper: Reverse mode - Detected slot: Row {detectedRow}, Col {detectedCol}");
                Debug.Log($"UVRemapper: Reverse mode - Detected offset: U={detectedUOffset:F4}, V={detectedVOffset:F4}");
                
                // Apply inverse transformation: scale and offset back to [0,1]
                for (int i = 0; i < originalUVs.Length; i++)
                {
                    remappedUVs[i] = new Vector2(
                        (originalUVs[i].x - detectedUOffset) / slotWidth,
                        (originalUVs[i].y - detectedVOffset) / slotHeight
                    );
                }
            }
            else
            {
                // Normal mode: remap from [0,1] to atlas slot
                for (int i = 0; i < originalUVs.Length; i++)
                {
                    // Scale original UV [0,1] to slot size, then offset to slot position
                    remappedUVs[i] = new Vector2(
                        originalUVs[i].x * slotWidth + uOffset,
                        originalUVs[i].y * slotHeight + vOffset
                    );
                }
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
                    
                    // Use mesh name to create unique filename for each mesh from the same FBX
                    // This prevents overwriting when processing multiple meshes from one FBX
                    string meshName = originalMesh.name;
                    // Sanitize mesh name for filename (remove invalid characters)
                    meshName = System.Text.RegularExpressions.Regex.Replace(meshName, @"[^\w\s-]", "");
                    meshName = meshName.Replace(" ", "_");
                    
                    // Create unique filename: {fbxName}_{meshName}_remapped_uv{textureID}.asset
                    string newAssetPath = $"{directory}/{fileName}_{meshName}_remapped_uv{textureID}.asset";
                    
                    // If filename is too long or same as original, add index
                    if (newAssetPath.Length > 200 || meshName == fileName || string.IsNullOrEmpty(meshName))
                    {
                        // Use index instead of mesh name if name is too long
                        newAssetPath = $"{directory}/{fileName}_mesh{meshIndex}_remapped_uv{textureID}.asset";
                    }
                    
                    // Check if file already exists
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(newAssetPath) != null)
                    {
                        // Update existing asset
                        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(newAssetPath);
                        EditorUtility.CopySerialized(newMesh, existingMesh);
                        EditorUtility.SetDirty(existingMesh);
                        AssetDatabase.SaveAssets();
                        if (meshFilter != null)
                        {
                            meshFilter.sharedMesh = existingMesh;
                        }
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
                            if (meshFilter != null)
                            {
                                meshFilter.sharedMesh = reloadedMesh;
                            }
                            Debug.Log($"UVRemapper: Created new remapped mesh asset: {newAssetPath}");
                            
                            // If processing direct mesh, update the reference
                            if (targetMesh != null && targetMesh == originalMesh)
                            {
                                targetMesh = reloadedMesh;
                            }
                        }
                        else
                        {
                            Debug.LogError($"UVRemapper: Failed to create mesh asset: {newAssetPath}");
                            if (meshFilter != null)
                            {
                                meshFilter.sharedMesh = newMesh; // Fallback to instance
                            }
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
                            if (meshFilter != null)
                            {
                                meshFilter.sharedMesh = reloadedMesh;
                            }
                            Debug.Log($"UVRemapper: Successfully replaced mesh asset: {assetPath}");
                            
                            // If processing direct mesh, update the reference
                            if (targetMesh != null && targetMesh == originalMesh)
                            {
                                targetMesh = reloadedMesh;
                            }
                        }
                        else
                        {
                            Debug.LogError($"UVRemapper: Failed to reload mesh from {assetPath}");
                            if (meshFilter != null)
                            {
                                meshFilter.sharedMesh = newMesh; // Fallback to instance
                            }
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
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = newMesh;
                    Debug.Log($"UVRemapper: Replaced instance mesh on {meshFilter.name}");
                }
                else
                {
                    // Direct mesh mode - update the mesh reference
                    if (targetMesh != null && targetMesh == originalMesh)
                    {
                        targetMesh = newMesh;
                    }
                    Debug.Log($"UVRemapper: Created new instance mesh for direct mesh processing");
                }
            }
            
            // Verify the changes were applied
            Mesh finalMesh = meshFilter != null ? meshFilter.sharedMesh : (targetMesh != null && targetMesh == originalMesh ? targetMesh : newMesh);
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
            string meshLocation = meshFilter != null ? $"on {meshFilter.name}" : "(direct mesh)";
            Debug.Log($"UVRemapper: Remapped UVs for mesh: {originalMesh.name} {meshLocation}");
        }
        
        // Force scene update
        if (targetModel != null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }
        
        EditorUtility.DisplayDialog(
            "UV Remapping Complete",
            $"Successfully remapped UV coordinates for {processedCount} mesh(es).\n\n" +
            $"Texture ID: {textureID} (Row {row}, Column {col})\n" +
            $"UV Range: U[{uOffset:F3}, {uOffset + slotWidth:F3}], V[{vOffset:F3}, {vOffset + slotHeight:F3}]\n\n" +
            $"Check Console for detailed logs.",
            "OK"
        );
        
        Debug.Log($"UVRemapper: ===== Completed remapping for {processedCount} mesh(es) =====");
        
        // Select the target model or ping the mesh asset
        if (targetModel != null)
        {
            Selection.activeGameObject = targetModel;
        }
        else if (targetMesh != null)
        {
            EditorGUIUtility.PingObject(targetMesh);
        }
    }
}

