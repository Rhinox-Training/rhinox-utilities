using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// WIP - not finished yet
/// Idea of this component is to make cellsize fill the target area based on the constraints and make the cells respect their preferred size
/// </summary>
[AddComponentMenu("Layout/Dynamic Grid Layout Group", 152)]
public class DynamicGridLayoutGroup : LayoutGroup
{
    /// <summary>
    /// Which corner is the starting corner for the grid.
    /// </summary>
    public enum Corner
    {
        UpperLeft = 0,
        UpperRight = 1,
        LowerLeft = 2,
        LowerRight = 3
    }

    /// <summary>
    /// The grid axis we are looking at.
    /// </summary>
    /// <remarks>
    /// As the storage is a [][] we make access easier by passing a axis.
    /// </remarks>
    public enum Axis
    {
        None = 1,
        Horizontal = 2,
        Vertical = 3
    }
    
    public Vector2 CellSize;
    public Vector2 Spacing;

    [EnumToggleButtons] public Axis InitialAxis;

    [MinValue(0), HideIf(nameof(InitialAxis), Axis.None)]
    public int Amount;

    [ReadOnly, ShowInInspector] private int _affected => rectChildren.Count;

    protected override void Awake()
    {
        base.Awake();
    }

    protected void Update()
    {
        switch (InitialAxis)
        {
            case Axis.Horizontal:
                if (Amount > 0)
                    CellSize.x = rectTransform.rect.width / Amount;
                break;
            case Axis.Vertical:
                if (Amount > 0)
                    CellSize.y = rectTransform.rect.height / Amount;
                break;
        }
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        int minColumns = 0;
        int preferredColumns = 0;
        switch (InitialAxis)
        {
            case Axis.Horizontal:
                minColumns = preferredColumns = Amount;
                break;
            case Axis.Vertical:
                minColumns = preferredColumns = Mathf.CeilToInt(rectChildren.Count / (float) Amount - 0.001f);
                break;
            default:
                minColumns = 1;
                preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count));
                break;
        }
        

        SetLayoutInputForAxis(
            padding.horizontal + (CellSize.x + Spacing.x) * minColumns - Spacing.x,
            padding.horizontal + (CellSize.x + Spacing.x) * preferredColumns - Spacing.x,
            -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        int minRows = 0;
        switch (InitialAxis)
        {
            case Axis.Horizontal:
                minRows = Mathf.CeilToInt(rectChildren.Count / (float) Amount - 0.001f);
                break;
            case Axis.Vertical:
                minRows = Amount;
                break;
            default:
                float width = rectTransform.rect.width;
                int cellCountX = Mathf.Max(1,
                    Mathf.FloorToInt((width - padding.horizontal + Spacing.x + 0.001f) / (CellSize.x + Spacing.x)));
                minRows = Mathf.CeilToInt(rectChildren.Count / (float) cellCountX);
                break;
        }

        float minSpace = padding.vertical + (CellSize.y + Spacing.y) * minRows - Spacing.y;
        SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
    }

    public override void SetLayoutHorizontal()
    {
        SetCellsAlongAxis(0);
    }

    public override void SetLayoutVertical()
    {
        SetCellsAlongAxis(1);
    }

    private void SetCellsAlongAxis(int axis)
    {
        // Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
        // and only vertical values when invoked for the vertical axis.
        // However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
        // Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
        // and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.

        if (axis == 0)
        {
            // Only set the sizes when invoked for horizontal axis, not the positions.
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform rect = rectChildren[i];

                m_Tracker.Add(this, rect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.SizeDelta);

                rect.anchorMin = Vector2.up;
                rect.anchorMax = Vector2.up;
                rect.sizeDelta = CellSize;
            }

            return;
        }

        Vector2 size = rectTransform.rect.size;

        int cellCountX = 1;
        int cellCountY = 1;
        
        var initialAxis = InitialAxis;
        if (Amount == 0) initialAxis = Axis.None;
        
        // Calculate amount of columns / rows
        switch (initialAxis)
        {
            case Axis.Horizontal:
                cellCountX = Amount;

                if (rectChildren.Count > cellCountX)
                    cellCountY = rectChildren.Count / cellCountX + (rectChildren.Count % cellCountX > 0 ? 1 : 0);
                break;
            case Axis.Vertical:
                cellCountY = Amount;

                if (rectChildren.Count > cellCountY)
                    cellCountX = rectChildren.Count / cellCountY + (rectChildren.Count % cellCountY > 0 ? 1 : 0);
                break;
            default:
                if (CellSize.x + Spacing.x <= 0) // No size info available
                    cellCountX = int.MaxValue;
                else // divide available space by cell size
                    cellCountX = Mathf.Max(1, Mathf.FloorToInt((size.x - padding.horizontal + Spacing.x + 0.001f) / (CellSize.x + Spacing.x)));

                if (CellSize.y + Spacing.y <= 0)
                    cellCountY = int.MaxValue;
                else // divide available space by cell size
                    cellCountY = Mathf.Max(1, Mathf.FloorToInt((size.y - padding.vertical + Spacing.y + 0.001f) / (CellSize.y + Spacing.y)));
                break;
        }

        // int cornerX = (int)startCorner % 2; // 0 = left; 1 = right
        // int cornerY = (int)startCorner / 2; // 0 = top; 1 = bottom

        int cellsPerMainAxis, actualCellCountX, actualCellCountY;
        if (InitialAxis == Axis.Horizontal)
        {
            cellsPerMainAxis = cellCountX;
            actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildren.Count);
            actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildren.Count / (float) cellsPerMainAxis));
        }
        else
        {
            cellsPerMainAxis = cellCountY;
            actualCellCountY = Mathf.Clamp(cellCountY, 1, rectChildren.Count);
            actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(rectChildren.Count / (float) cellsPerMainAxis));
        }

        Vector2 requiredSpace = new Vector2(
            actualCellCountX * CellSize.x + (actualCellCountX - 1) * Spacing.x,
            actualCellCountY * CellSize.y + (actualCellCountY - 1) * Spacing.y
        );
        Vector2 startOffset = new Vector2(
            GetStartOffset(0, requiredSpace.x),
            GetStartOffset(1, requiredSpace.y)
        );

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int positionX;
            int positionY;
            if (InitialAxis == Axis.Horizontal)
            {
                positionX = i % cellsPerMainAxis;
                positionY = i / cellsPerMainAxis;
            }
            else
            {
                positionX = i / cellsPerMainAxis;
                positionY = i % cellsPerMainAxis;
            }

            // if (cornerX == 1) // Grid start right => switch all orientations
            //     positionX = actualCellCountX - 1 - positionX;
            // if (cornerY == 1) // Grid start bottom => switch all orientations
            //     positionY = actualCellCountY - 1 - positionY;

            SetChildAlongAxis(rectChildren[i], 0, startOffset.x + (CellSize[0] + Spacing[0]) * positionX, CellSize[0]);
            SetChildAlongAxis(rectChildren[i], 1, startOffset.y + (CellSize[1] + Spacing[1]) * positionY, CellSize[1]);
        }
    }
}