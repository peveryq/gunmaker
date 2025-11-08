using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
[DisallowMultipleComponent]
public class ResponsiveGridLayout : MonoBehaviour
{
    [SerializeField] private int columns = 3;
    [SerializeField] private Vector2 cellWidthRange = new Vector2(180f, 320f);
    [SerializeField] private float heightToWidthRatio = 1.2f;
    [SerializeField] private bool clampWidth = true;

    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    private void Awake()
    {
        CacheComponents();
        UpdateCellSize();
    }

    private void OnEnable()
    {
        CacheComponents();
        UpdateCellSize();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateCellSize();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateCellSize();
        }
    }
#endif

    private void CacheComponents()
    {
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    private void UpdateCellSize()
    {
        if (gridLayout == null || rectTransform == null || columns <= 0)
            return;

        float availableWidth = rectTransform.rect.width;
        if (availableWidth <= 0f)
            return;

        availableWidth -= gridLayout.padding.left + gridLayout.padding.right;
        float totalSpacing = gridLayout.spacing.x * Mathf.Max(0, columns - 1);
        float rawCellWidth = (availableWidth - totalSpacing) / columns;

        float cellWidth = rawCellWidth;
        if (clampWidth)
        {
            cellWidth = Mathf.Clamp(rawCellWidth, cellWidthRange.x, cellWidthRange.y);
        }

        float cellHeight = Mathf.Max(1f, cellWidth * heightToWidthRatio);
        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}
