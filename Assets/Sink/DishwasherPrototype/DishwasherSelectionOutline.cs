using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws UI dots around the selected dish's RectTransform. No texture, sprite,
/// prefab, or external art is required. This is visual only, never a hitbox.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DishwasherSelectionOutline : MaskableGraphic
{
    [Min(1f)] public float dotSize = 3f;
    [Min(3f)] public float dotSpacing = 10f;
    [Min(0f)] public float insetFromBounds = 3f;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float left = r.xMin - insetFromBounds;
        float right = r.xMax + insetFromBounds;
        float bottom = r.yMin - insetFromBounds;
        float top = r.yMax + insetFromBounds;

        AddDots(vh, new Vector2(left, top), new Vector2(right, top));
        AddDots(vh, new Vector2(left, bottom), new Vector2(right, bottom));
        AddDots(vh, new Vector2(left, bottom), new Vector2(left, top));
        AddDots(vh, new Vector2(right, bottom), new Vector2(right, top));
    }

    private void AddDots(VertexHelper vh, Vector2 a, Vector2 b)
    {
        float length = Vector2.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(length / dotSpacing));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 center = Vector2.Lerp(a, b, (float)i / steps);
            float half = dotSize * 0.5f;
            AddQuad(vh, center - Vector2.one * half, center + Vector2.one * half);
        }
    }

    private void AddQuad(VertexHelper vh, Vector2 min, Vector2 max)
    {
        int i = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = color;
        v.position = new Vector3(min.x, min.y); vh.AddVert(v);
        v.position = new Vector3(min.x, max.y); vh.AddVert(v);
        v.position = new Vector3(max.x, max.y); vh.AddVert(v);
        v.position = new Vector3(max.x, min.y); vh.AddVert(v);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }
}
