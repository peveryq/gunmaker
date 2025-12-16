using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool for building texture atlases from multiple textures
/// Textures are arranged in a grid layout (left-to-right, top-to-bottom)
/// Each texture gets an ID based on its position in the grid
/// </summary>
public class TextureAtlasBuilder : EditorWindow
{
    private Texture2D atlasTexture;
    private int textureCount = 4; // Must be multiple of 4
    private List<Texture2D> textures = new List<Texture2D>();
    private Vector2 scrollPosition;
    
    private const int MIN_TEXTURE_COUNT = 4;
    private const int MAX_TEXTURE_COUNT = 64; // Reasonable limit
    
    [MenuItem("Tools/Gunmaker/Texture Atlas Builder")]
    public static void ShowWindow()
    {
        GetWindow<TextureAtlasBuilder>("Texture Atlas Builder");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Texture Atlas Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Atlas texture input
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Atlas Settings", EditorStyles.boldLabel);
        
        atlasTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Atlas Texture (PNG, Default type)",
            atlasTexture,
            typeof(Texture2D),
            false
        );
        
        if (atlasTexture != null)
        {
            EditorGUILayout.LabelField($"Atlas Size: {atlasTexture.width}x{atlasTexture.height}");
            
            // Check texture type
            string texturePath = AssetDatabase.GetAssetPath(atlasTexture);
            if (!string.IsNullOrEmpty(texturePath))
            {
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    if (importer.textureType != TextureImporterType.Default)
                    {
                        EditorGUILayout.HelpBox(
                            $"Warning: Texture type is '{importer.textureType}', but 'Default' is recommended.",
                            MessageType.Warning
                        );
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Texture Type: Default ✓", EditorStyles.miniLabel);
                    }
                }
            }
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Texture count input
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        
        int newCount = EditorGUILayout.IntField("Texture Count (multiple of 4)", textureCount);
        if (newCount != textureCount)
        {
            // Ensure it's a multiple of 4
            if (newCount < MIN_TEXTURE_COUNT)
                newCount = MIN_TEXTURE_COUNT;
            else if (newCount > MAX_TEXTURE_COUNT)
                newCount = MAX_TEXTURE_COUNT;
            
            // Round to nearest multiple of 4
            newCount = (newCount / 4) * 4;
            if (newCount < MIN_TEXTURE_COUNT)
                newCount = MIN_TEXTURE_COUNT;
            
            textureCount = newCount;
        }
        
        // Calculate grid dimensions
        int gridSize = Mathf.RoundToInt(Mathf.Sqrt(textureCount));
        EditorGUILayout.LabelField($"Grid: {gridSize}x{gridSize} ({textureCount} slots)");
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Texture list
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Textures ({textures.Count}/{textureCount})", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        
        // Add/remove buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Texture"))
        {
            textures.Add(null);
        }
        if (GUILayout.Button("Remove Last") && textures.Count > 0)
        {
            textures.RemoveAt(textures.Count - 1);
        }
        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Clear All", "Remove all textures from list?", "Yes", "No"))
            {
                textures.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Calculate expected slot size for validation
        int expectedSlotWidth = 0;
        int expectedSlotHeight = 0;
        if (atlasTexture != null)
        {
            int calculatedGridSize = Mathf.RoundToInt(Mathf.Sqrt(textureCount));
            expectedSlotWidth = atlasTexture.width / calculatedGridSize;
            expectedSlotHeight = atlasTexture.height / calculatedGridSize;
        }
        
        // Texture fields
        for (int i = 0; i < textures.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Slot {i}:", GUILayout.Width(60));
            textures[i] = (Texture2D)EditorGUILayout.ObjectField(
                textures[i],
                typeof(Texture2D),
                false
            );
            
            // Show ID info and size validation
            if (i < textureCount)
            {
                int row = i / gridSize;
                int col = i % gridSize;
                EditorGUILayout.LabelField($"ID: {i} ({row},{col})", GUILayout.Width(100));
                
                // Check texture size
                if (textures[i] != null && atlasTexture != null)
                {
                    if (textures[i].width != expectedSlotWidth || textures[i].height != expectedSlotHeight)
                    {
                        EditorGUILayout.LabelField(
                            $"⚠ {textures[i].width}x{textures[i].height} (expected {expectedSlotWidth}x{expectedSlotHeight})",
                            EditorStyles.miniLabel,
                            GUILayout.Width(200)
                        );
                    }
                    else
                    {
                        EditorGUILayout.LabelField(
                            $"✓ {textures[i].width}x{textures[i].height}",
                            EditorStyles.miniLabel,
                            GUILayout.Width(100)
                        );
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("(Will be skipped)", GUILayout.Width(120));
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Build button
        EditorGUI.BeginDisabledGroup(atlasTexture == null || textures.Count == 0);
        
        if (GUILayout.Button("Build Atlas", GUILayout.Height(30)))
        {
            BuildAtlas();
        }
        
        EditorGUI.EndDisabledGroup();
        
        // Info
        if (atlasTexture == null)
        {
            EditorGUILayout.HelpBox("Please assign an Atlas Texture (PNG, Default type) first.", MessageType.Warning);
        }
        else if (textures.Count == 0)
        {
            EditorGUILayout.HelpBox("Please add at least one texture to the list.", MessageType.Warning);
        }
        else if (textures.Count > textureCount)
        {
            EditorGUILayout.HelpBox(
                $"Warning: {textures.Count} textures provided, but only {textureCount} slots available. " +
                $"Only the first {textureCount} textures will be used.",
                MessageType.Warning
            );
        }
    }
    
    private void BuildAtlas()
    {
        if (atlasTexture == null)
        {
            EditorUtility.DisplayDialog("Error", "Atlas Texture is not assigned!", "OK");
            return;
        }
        
        if (textures.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No textures to pack!", "OK");
            return;
        }
        
        // Verify texture type
        string texturePath = AssetDatabase.GetAssetPath(atlasTexture);
        if (!string.IsNullOrEmpty(texturePath))
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Default)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Texture Type Warning",
                    $"The atlas texture type is '{importer.textureType}', but 'Default' is recommended.\n\n" +
                    "Do you want to continue anyway?",
                    "Continue",
                    "Cancel"
                );
                if (!proceed)
                    return;
            }
        }
        
        // Calculate grid dimensions
        int gridSize = Mathf.RoundToInt(Mathf.Sqrt(textureCount));
        int slotWidth = atlasTexture.width / gridSize;
        int slotHeight = atlasTexture.height / gridSize;
        
        // Check sRGB settings for atlas texture
        bool atlasIsSRGB = false;
        string atlasPath = AssetDatabase.GetAssetPath(atlasTexture);
        if (!string.IsNullOrEmpty(atlasPath))
        {
            TextureImporter atlasImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
            if (atlasImporter != null)
            {
                atlasIsSRGB = atlasImporter.sRGBTexture;
            }
        }
        
        // Create a new empty Texture2D for the atlas - avoid RenderTexture completely
        // This ensures no filtering or interpolation artifacts
        Texture2D readableAtlas = new Texture2D(
            atlasTexture.width, 
            atlasTexture.height, 
            TextureFormat.RGBA32,  // Uncompressed format
            false,  // mipmaps
            !atlasIsSRGB  // linear = true if NOT sRGB
        );
        
        // Try to copy existing atlas content if readable, otherwise start with empty
        try
        {
            Color[] existingPixels = atlasTexture.GetPixels();
            readableAtlas.SetPixels(existingPixels);
            readableAtlas.Apply(false, false);
        }
        catch
        {
            // Atlas texture is not readable - start with empty (black) texture
            // This is fine, we'll fill it with our textures
            Color[] emptyPixels = new Color[atlasTexture.width * atlasTexture.height];
            readableAtlas.SetPixels(emptyPixels);
            readableAtlas.Apply(false, false);
        }
        
        // Pack textures into atlas
        int texturesToPack = Mathf.Min(textures.Count, textureCount);
        
        for (int i = 0; i < texturesToPack; i++)
        {
            if (textures[i] == null)
            {
                Debug.LogWarning($"Texture at index {i} is null, skipping...");
                continue;
            }
            
            // Calculate position in grid (left-to-right, top-to-bottom)
            int row = i / gridSize;
            int col = i % gridSize;
            
            int x = col * slotWidth;
            int y = atlasTexture.height - (row + 1) * slotHeight; // Flip Y for Unity coordinates
            
            // Check if texture needs scaling
            bool needsScaling = textures[i].width != slotWidth || textures[i].height != slotHeight;
            
            Texture2D sourceTexture = null;
            bool shouldDestroySource = false;
            
            if (needsScaling)
            {
                // Need to scale - make readable first, then scale
                Texture2D readableTexture = MakeTextureReadable(textures[i]);
                if (readableTexture == null)
                {
                    Debug.LogWarning($"Could not make texture {i} readable, skipping...");
                    continue;
                }
                
                sourceTexture = ScaleTexture(readableTexture, slotWidth, slotHeight);
                
                // Clean up readable copy if we created it
                if (readableTexture != textures[i])
                {
                    DestroyImmediate(readableTexture);
                }
                shouldDestroySource = true;
            }
            else
            {
                // No scaling needed - try to use texture directly without RenderTexture
                // This preserves original quality
                try
                {
                    // Try to read directly
                    textures[i].GetPixel(0, 0);
                    // Texture is readable - use it directly
                    sourceTexture = textures[i];
                    shouldDestroySource = false;
                }
                catch
                {
                    // Texture is not readable - try to make it readable via importer settings first
                    // This avoids Graphics.Blit which can add artifacts
                    string sourceTexturePath = AssetDatabase.GetAssetPath(textures[i]);
                    if (!string.IsNullOrEmpty(sourceTexturePath))
                    {
                        TextureImporter importer = AssetImporter.GetAtPath(sourceTexturePath) as TextureImporter;
                        if (importer != null && !importer.isReadable)
                        {
                            // Try to enable read/write and reimport
                            importer.isReadable = true;
                            AssetDatabase.ImportAsset(sourceTexturePath, ImportAssetOptions.ForceUpdate);
                            AssetDatabase.Refresh();
                            
                            // Try again after reimport
                            try
                            {
                                textures[i].GetPixel(0, 0);
                                sourceTexture = textures[i];
                                shouldDestroySource = false;
                                Debug.Log($"Made texture {i} readable via importer settings");
                            }
                            catch
                            {
                                // Still not readable - fallback to RenderTexture method
                                sourceTexture = MakeTextureReadableDirect(textures[i]);
                                shouldDestroySource = true;
                            }
                        }
                        else
                        {
                            // Already readable in settings but still can't read - use RenderTexture
                            sourceTexture = MakeTextureReadableDirect(textures[i]);
                            shouldDestroySource = true;
                        }
                    }
                    else
                    {
                        // No asset path - use RenderTexture method
                        sourceTexture = MakeTextureReadableDirect(textures[i]);
                        shouldDestroySource = true;
                    }
                }
            }
            
            if (sourceTexture == null)
            {
                Debug.LogWarning($"Could not prepare texture {i} for packing, skipping...");
                continue;
            }
            
            // Copy pixels to atlas - direct pixel copy, no interpolation
            // Use GetPixels with exact region to avoid any filtering
            Color[] pixels = sourceTexture.GetPixels(0, 0, slotWidth, slotHeight);
            
            // Verify pixel count matches
            if (pixels.Length != slotWidth * slotHeight)
            {
                Debug.LogError($"Pixel count mismatch for texture {i}: expected {slotWidth * slotHeight}, got {pixels.Length}");
                continue;
            }
            
            // Log texture format for debugging
            Debug.Log($"Texture {i}: Format={sourceTexture.format}, Size={sourceTexture.width}x{sourceTexture.height}, Atlas format={readableAtlas.format}");
            
            // Set pixels directly - this is a pure copy operation, no filtering
            // Make sure we're setting the exact region
            readableAtlas.SetPixels(x, y, slotWidth, slotHeight, pixels);
            
            Debug.Log($"Copied {pixels.Length} pixels from texture {i} ({sourceTexture.width}x{sourceTexture.height}) to atlas position ({x}, {y})");
            
            // Clean up if we created a temporary texture
            if (shouldDestroySource && sourceTexture != textures[i])
            {
                DestroyImmediate(sourceTexture);
            }
            
            Debug.Log($"Packed texture {i} to slot ({row}, {col}) at position ({x}, {y})");
        }
        
        // Apply all changes at once - use false, false to prevent mipmap generation
        readableAtlas.Apply(false, false);
        
        // Save the atlas
        string path = AssetDatabase.GetAssetPath(atlasTexture);
        if (string.IsNullOrEmpty(path))
        {
            // If atlas is not an asset, save to a new file
            path = EditorUtility.SaveFilePanelInProject(
                "Save Atlas",
                "TextureAtlas",
                "png",
                "Choose where to save the atlas"
            );
            
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(readableAtlas);
                return;
            }
        }
        else
        {
            // Confirm overwrite
            if (!EditorUtility.DisplayDialog(
                "Overwrite Atlas",
                $"This will overwrite the existing atlas texture at:\n{path}\n\nContinue?",
                "Yes",
                "Cancel"
            ))
            {
                DestroyImmediate(readableAtlas);
                return;
            }
        }
        
        // Write texture to file
        byte[] pngData = readableAtlas.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        
        DestroyImmediate(readableAtlas);
        
        // Refresh asset database
        AssetDatabase.Refresh();
        
        // Reimport the texture with quality-preserving settings
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (textureImporter != null)
        {
            // Save current settings
            bool wasReadable = textureImporter.isReadable;
            TextureImporterCompression compression = textureImporter.textureCompression;
            FilterMode filterMode = textureImporter.filterMode;
            
            // Set quality-preserving settings
            textureImporter.isReadable = true;  // Keep readable for future edits
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;  // No compression to avoid artifacts
            textureImporter.filterMode = FilterMode.Point;  // Point filtering to preserve exact pixels
            textureImporter.mipmapEnabled = false;  // No mipmaps
            
            // Apply settings and reimport
            EditorUtility.SetDirty(textureImporter);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            
            Debug.Log($"TextureAtlasBuilder: Applied quality settings to atlas - Uncompressed, Point filter, No mipmaps");
        }
        
        EditorUtility.DisplayDialog(
            "Atlas Built",
            $"Atlas successfully built!\n\nPacked {texturesToPack} textures into {textureCount} slots.\n\nSaved to: {path}",
            "OK"
        );
        
        Debug.Log($"TextureAtlasBuilder: Atlas built successfully with {texturesToPack} textures. Saved to {path}");
    }
    
    private Texture2D MakeTextureReadable(Texture2D texture)
    {
        if (texture == null)
            return null;
        
        // Try to get readable version directly
        try
        {
            texture.GetPixel(0, 0);
            return texture; // Already readable
        }
        catch
        {
            // Not readable, need to create a copy
        }
        
        return MakeTextureReadableDirect(texture);
    }
    
    /// <summary>
    /// Creates a readable copy of texture using RenderTexture (for when scaling is needed)
    /// </summary>
    private Texture2D MakeTextureReadableDirect(Texture2D texture)
    {
        // Check sRGB settings for source texture
        bool textureIsSRGB = false;
        string texturePath = AssetDatabase.GetAssetPath(texture);
        if (!string.IsNullOrEmpty(texturePath))
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter != null)
            {
                textureIsSRGB = textureImporter.sRGBTexture;
            }
        }
        
        // Use ARGB32 format for best quality
        RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
        
        // Create RenderTexture with exact same size - no scaling, no filtering artifacts
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            rtFormat,
            textureIsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
        );
        
        // Use Point filtering to avoid any interpolation when size matches
        renderTexture.filterMode = FilterMode.Point;
        
        // Blit with point filtering to preserve exact pixels
        Graphics.Blit(texture, renderTexture);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        
        // Create Texture2D with correct color space settings
        Texture2D readable = new Texture2D(
            texture.width, 
            texture.height, 
            TextureFormat.RGBA32, 
            false,  // mipmaps
            !textureIsSRGB  // linear = true if NOT sRGB
        );
        readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        readable.Apply(false, false);  // No mipmaps, make readable
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        
        return readable;
    }
    
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        // Check sRGB settings for source texture
        bool sourceIsSRGB = false;
        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (!string.IsNullOrEmpty(sourcePath))
        {
            TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            if (sourceImporter != null)
            {
                sourceIsSRGB = sourceImporter.sRGBTexture;
            }
        }
        
        // Use ARGB32 format for better quality (no compression artifacts)
        RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
        
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            targetWidth,
            targetHeight,
            0,
            rtFormat,
            sourceIsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
        );
        
        // Determine best filter mode based on scaling type
        bool isIntegerUpscale = (targetWidth >= source.width && targetHeight >= source.height) &&
                                (targetWidth % source.width == 0) && (targetHeight % source.height == 0);
        bool isIntegerDownscale = (targetWidth < source.width && targetHeight < source.height) &&
                                  (source.width % targetWidth == 0) && (source.height % targetHeight == 0);
        
        if (isIntegerUpscale || isIntegerDownscale)
        {
            // Integer scaling - use point filtering for pixel-perfect result
            renderTexture.filterMode = FilterMode.Point;
        }
        else
        {
            // Non-integer scaling - use bilinear for smoother result
            renderTexture.filterMode = FilterMode.Bilinear;
        }
        
        // Save previous render texture settings
        RenderTexture previous = RenderTexture.active;
        
        // Blit with high quality
        Graphics.Blit(source, renderTexture);
        RenderTexture.active = renderTexture;
        
        // Create Texture2D with correct color space settings and high quality format
        Texture2D scaled = new Texture2D(
            targetWidth, 
            targetHeight, 
            TextureFormat.RGBA32,  // Use RGBA32 for best quality (no compression)
            false,  // mipmaps
            !sourceIsSRGB  // linear = true if NOT sRGB
        );
        
        // Use ReadPixels with exact rectangle for better quality
        scaled.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        scaled.Apply(false, false);  // Don't update mipmaps, make readable
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        
        return scaled;
    }
}

