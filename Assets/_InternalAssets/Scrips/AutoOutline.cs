using UnityEngine;
using System;
using System.Reflection;

[ExecuteInEditMode]
public class AutoOutline : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private bool updateDynamically = true; // Update when children change
    
    private int lastChildCount = -1;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupOutlinable();
        }
    }
    
    private void Update()
    {
        // Update when child count changes (new parts added)
        if (updateDynamically && includeChildren)
        {
            int currentChildCount = transform.childCount;
            if (currentChildCount != lastChildCount)
            {
                lastChildCount = currentChildCount;
                SetupOutlinable();
            }
        }
    }
    
    [ContextMenu("Setup Outlinable")]
    public void SetupOutlinable()
    {
        // Get Outlinable component
        MonoBehaviour outlinable = GetComponent("Outlinable") as MonoBehaviour;
        
        if (outlinable == null)
        {
            Debug.LogWarning("Outlinable component not found on " + gameObject.name);
            return;
        }
        
        // Get all renderers
        Renderer[] renderers;
        if (includeChildren)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
        else
        {
            Renderer renderer = GetComponent<Renderer>();
            renderers = renderer != null ? new Renderer[] { renderer } : new Renderer[0];
        }
        
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found on " + gameObject.name);
            return;
        }
        
        // Use reflection to work with Outlinable
        System.Type outlinableType = outlinable.GetType();
        
        // Get OutlineTarget type from EPOOutline namespace
        System.Type outlineTargetType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            outlineTargetType = assembly.GetType("EPOOutline.OutlineTarget");
            if (outlineTargetType != null) break;
        }
        
        if (outlineTargetType == null)
        {
            Debug.LogError("OutlineTarget type not found!");
            return;
        }
        
        // Find TryAddTarget method
        MethodInfo tryAddTargetMethod = outlinableType.GetMethod("TryAddTarget", BindingFlags.Public | BindingFlags.Instance);
        
        if (tryAddTargetMethod == null)
        {
            Debug.LogWarning("TryAddTarget method not found on Outlinable.");
            return;
        }
        
        // Get outlineTargets field to check/clear
        FieldInfo targetsField = outlinableType.GetField("outlineTargets", BindingFlags.NonPublic | BindingFlags.Instance);
        if (targetsField != null)
        {
            var targets = targetsField.GetValue(outlinable) as System.Collections.IList;
            if (targets != null)
            {
                targets.Clear();
            }
        }
        
        // Add each renderer as OutlineTarget
        int addedCount = 0;
        foreach (Renderer renderer in renderers)
        {
            try
            {
                // Get submesh count
                int submeshCount = 1;
                if (renderer is MeshRenderer)
                {
                    MeshFilter mf = renderer.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        submeshCount = mf.sharedMesh.subMeshCount;
                    }
                }
                else if (renderer is SkinnedMeshRenderer)
                {
                    SkinnedMeshRenderer smr = renderer as SkinnedMeshRenderer;
                    if (smr.sharedMesh != null)
                    {
                        submeshCount = smr.sharedMesh.subMeshCount;
                    }
                }
                
                // Add target for each submesh
                for (int i = 0; i < submeshCount; i++)
                {
                    // Create OutlineTarget instance using default constructor
                    object outlineTarget = Activator.CreateInstance(outlineTargetType);
                    
                    // Set renderer and submesh index fields
                    FieldInfo rendererField = outlineTargetType.GetField("renderer");
                    FieldInfo submeshField = outlineTargetType.GetField("SubmeshIndex");
                    
                    if (rendererField != null)
                    {
                        rendererField.SetValue(outlineTarget, renderer);
                    }
                    
                    if (submeshField != null)
                    {
                        submeshField.SetValue(outlineTarget, i);
                    }
                    
                    // Call TryAddTarget
                    tryAddTargetMethod.Invoke(outlinable, new object[] { outlineTarget });
                    addedCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to add renderer {renderer.name} to Outlinable: {e.Message}");
            }
        }
        
        Debug.Log($"Added {addedCount} targets from {renderers.Length} renderers to Outlinable on {gameObject.name}");
    }
}

