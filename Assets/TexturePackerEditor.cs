using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class TexturePackerEditor : EditorWindow
{
    private Vector2Int gridSize = new Vector2Int(4, 4);
    private int textureSize = 256;
    private List<Texture2D> textures = new List<Texture2D>();
    private Texture2D resultTexture;
    
    [MenuItem("Tools/Texture Packer")]
    public static void ShowWindow()
    {
        GetWindow<TexturePackerEditor>("Texture Packer");
    }

    void OnGUI()
    {
        GUILayout.Label("Texture Packer Settings", EditorStyles.boldLabel);
        
        gridSize = EditorGUILayout.Vector2IntField("Grid Size", gridSize);
        textureSize = EditorGUILayout.IntField("Texture Size per Cell", textureSize);
        
        GUILayout.Space(10);
        
        // Drag and drop area
        GUILayout.Label("Drag textures here:");
        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Textures Here");
        
        GUILayout.Space(10);
        
        // Selected textures list
        GUILayout.Label($"Selected textures: {textures.Count}");
        if (GUILayout.Button("Clear List"))
        {
            textures.Clear();
        }
        
        GUILayout.Space(10);
        
        // Process button
        if (GUILayout.Button("Pack Textures", GUILayout.Height(30)))
        {
            PackTextures();
        }
        
        // Display result if available
        if (resultTexture != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Result:");
            Rect textureRect = GUILayoutUtility.GetRect(200, 200);
            EditorGUI.DrawPreviewTexture(textureRect, resultTexture);
            
            if (GUILayout.Button("Save Result"))
            {
                SaveTexture();
            }
        }
        
        // Handle drag and drop
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is Texture2D texture)
                        {
                            if (!textures.Contains(texture))
                            {
                                textures.Add(texture);
                            }
                        }
                    }
                    
                    evt.Use();
                }
                break;
        }
    }

    private void PackTextures()
    {
        int maxTextures = gridSize.x * gridSize.y;
        if (textures.Count > maxTextures)
        {
            Debug.LogWarning($"Too many textures! Maximum is {maxTextures}, but got {textures.Count}");
            return;
        }
        
        if (textures.Count == 0)
        {
            Debug.LogWarning("No textures to pack!");
            return;
        }

        // Create result texture
        int resultWidth = gridSize.x * textureSize;
        int resultHeight = gridSize.y * textureSize;
        resultTexture = new Texture2D(resultWidth, resultHeight, TextureFormat.RGBA32, false);
        
        // Clear with transparent background
        Color[] clearColors = new Color[resultWidth * resultHeight];
        for (int i = 0; i < clearColors.Length; i++)
        {
            clearColors[i] = Color.clear;
        }
        resultTexture.SetPixels(clearColors);
        
        // Place textures in grid
        int textureIndex = 0;
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (textureIndex >= textures.Count)
                    break;
                
                Texture2D sourceTexture = textures[textureIndex];
                
                // Check if texture is square
                if (sourceTexture.width != sourceTexture.height)
                {
                    Debug.LogWarning($"Texture {sourceTexture.name} is not square! Skipping...");
                    textureIndex++;
                    continue;
                }
                
                // Resize and copy texture
                Texture2D resizedTexture = ResizeTexture(sourceTexture, textureSize, textureSize);
                Color[] pixels = resizedTexture.GetPixels();
                
                int startX = x * textureSize;
                int startY = (gridSize.y - 1 - y) * textureSize; // Flip Y for correct orientation
                
                resultTexture.SetPixels(startX, startY, textureSize, textureSize, pixels);
                
                textureIndex++;
            }
            
            if (textureIndex >= textures.Count)
                break;
        }
        
        resultTexture.Apply();
        Debug.Log("Textures packed successfully!");
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }

    private void SaveTexture()
    {
        if (resultTexture == null)
            return;
        
        string path = EditorUtility.SaveFilePanel(
            "Save Texture",
            "Assets",
            "packed_texture.png",
            "png"
        );
        
        if (string.IsNullOrEmpty(path))
            return;
        
        // Convert to relative path if in Assets folder
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }
        
        byte[] pngData = resultTexture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        
        AssetDatabase.Refresh();
        Debug.Log($"Texture saved to: {path}");
    }
}