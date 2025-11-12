using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

[ExecuteInEditMode]
public class AutoOutline : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private bool updateDynamically = true; // Update when children change

    private int lastRendererCount = -1;
    private bool setupScheduled;
    private readonly List<Renderer> rendererBuffer = new List<Renderer>(32);

    // Cache types and reflection data to avoid expensive lookups
    private static System.Type cachedOutlineTargetType;
    private static System.Type cachedOutlinableType;
    private static MethodInfo cachedTryAddTargetMethod;
    private static FieldInfo cachedRendererField;
    private static FieldInfo cachedSubmeshField;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupOutlinable();
        }

        lastRendererCount = ComputeRendererCount();
    }

    private void Update()
    {
        if (!updateDynamically) return;

        int currentRendererCount = ComputeRendererCount();
        if (currentRendererCount != lastRendererCount)
        {
            lastRendererCount = currentRendererCount;
            ScheduleSetup();
        }
    }

    private void ScheduleSetup()
    {
        if (setupScheduled) return;
        setupScheduled = true;
        Invoke(nameof(PerformScheduledSetup), 0.01f);
    }

    private void PerformScheduledSetup()
    {
        setupScheduled = false;
        SetupOutlinable();
    }

    // Public method to manually trigger update (called after part installation)
    public void RefreshOutline()
    {
        lastRendererCount = ComputeRendererCount();
        SetupOutlinable();
    }

    private int ComputeRendererCount()
    {
        if (!includeChildren)
        {
            return GetComponent<Renderer>() != null ? 1 : 0;
        }

        rendererBuffer.Clear();
#if UNITY_2020_1_OR_NEWER
        GetComponentsInChildren(true, rendererBuffer);
        return rendererBuffer.Count;
#else
        return GetComponentsInChildren<Renderer>(true).Length;
#endif
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

        // Cache outlinable type
        if (cachedOutlinableType == null)
        {
            cachedOutlinableType = outlinableType;
        }

        // Get OutlineTarget type (cached to avoid expensive assembly search)
        if (cachedOutlineTargetType == null)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                cachedOutlineTargetType = assembly.GetType("EPOOutline.OutlineTarget");
                if (cachedOutlineTargetType != null) break;
            }
        }

        if (cachedOutlineTargetType == null)
        {
            Debug.LogError("OutlineTarget type not found!");
            return;
        }

        // Find TryAddTarget method (cached)
        if (cachedTryAddTargetMethod == null && cachedOutlinableType != null)
        {
            cachedTryAddTargetMethod = cachedOutlinableType.GetMethod("TryAddTarget", BindingFlags.Public | BindingFlags.Instance);
        }

        if (cachedTryAddTargetMethod == null)
        {
            Debug.LogWarning("TryAddTarget method not found on Outlinable.");
            return;
        }

        // Cache field info
        if (cachedRendererField == null && cachedOutlineTargetType != null)
        {
            cachedRendererField = cachedOutlineTargetType.GetField("renderer");
            cachedSubmeshField = cachedOutlineTargetType.GetField("SubmeshIndex");
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
                    // Create OutlineTarget instance using default constructor (cached type)
                    object outlineTarget = Activator.CreateInstance(cachedOutlineTargetType);

                    // Set renderer and submesh index fields (use cached fields)
                    if (cachedRendererField != null)
                    {
                        cachedRendererField.SetValue(outlineTarget, renderer);
                    }

                    if (cachedSubmeshField != null)
                    {
                        cachedSubmeshField.SetValue(outlineTarget, i);
                    }

                    // Call TryAddTarget (use cached method)
                    cachedTryAddTargetMethod.Invoke(outlinable, new object[] { outlineTarget });
                    addedCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to add renderer {renderer.name} to Outlinable: {e.Message}");
            }
        }

        lastRendererCount = ComputeRendererCount();
        // Debug.Log($"Added {addedCount} targets from {renderers.Length} renderers to Outlinable on {gameObject.name}");
    }
}

