using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tests irregular 2D polygons in UI-local coordinates. Polygons can be concave,
/// and edge-to-edge contact does not count as an overlap.
/// </summary>
public static class DishwasherPlacementValidator
{
    private const float EdgeEpsilon = 0.08f;

    public static List<DishwasherDraggableDish> FindInvalidLoadedDishes(
        List<DishwasherDraggableDish> loaded,
        RectTransform dishwasherArea,
        RectTransform sharedLayer)
    {
        var invalid = new HashSet<DishwasherDraggableDish>();
        var allShapes = new List<List<List<Vector2>>>(loaded.Count);
        Rect boundary = dishwasherArea.rect;

        foreach (DishwasherDraggableDish dish in loaded)
        {
            List<List<Vector2>> inArea = dish.GetOutlinesInSpace(dishwasherArea);
            foreach (List<Vector2> polygon in inArea)
            {
                foreach (Vector2 point in polygon)
                {
                    if (point.x < boundary.xMin - EdgeEpsilon || point.x > boundary.xMax + EdgeEpsilon ||
                        point.y < boundary.yMin - EdgeEpsilon || point.y > boundary.yMax + EdgeEpsilon)
                    {
                        invalid.Add(dish);
                        break;
                    }
                }
            }
            allShapes.Add(dish.GetOutlinesInSpace(sharedLayer));
        }

        for (int a = 0; a < loaded.Count; a++)
        {
            for (int b = a + 1; b < loaded.Count; b++)
            {
                if (ShapesOverlap(allShapes[a], allShapes[b]))
                {
                    invalid.Add(loaded[a]);
                    invalid.Add(loaded[b]);
                }
            }
        }

        var ordered = new List<DishwasherDraggableDish>();
        foreach (DishwasherDraggableDish dish in loaded)
            if (invalid.Contains(dish)) ordered.Add(dish);
        return ordered;
    }

    private static bool ShapesOverlap(List<List<Vector2>> a, List<List<Vector2>> b)
    {
        foreach (List<Vector2> polyA in a)
            foreach (List<Vector2> polyB in b)
                if (PolygonsOverlap(polyA, polyB)) return true;
        return false;
    }

    private static bool PolygonsOverlap(List<Vector2> a, List<Vector2> b)
    {
        if (a.Count < 3 || b.Count < 3) return false;
        Rect boundsA = BoundsFor(a);
        Rect boundsB = BoundsFor(b);
        if (boundsA.xMax <= boundsB.xMin + EdgeEpsilon || boundsB.xMax <= boundsA.xMin + EdgeEpsilon ||
            boundsA.yMax <= boundsB.yMin + EdgeEpsilon || boundsB.yMax <= boundsA.yMin + EdgeEpsilon)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            Vector2 a0 = a[i];
            Vector2 a1 = a[(i + 1) % a.Count];
            for (int j = 0; j < b.Count; j++)
            {
                if (SegmentsCrossProperly(a0, a1, b[j], b[(j + 1) % b.Count]))
                    return true;
            }
        }

        foreach (Vector2 point in a)
            if (PointStrictlyInsidePolygon(point, b)) return true;
        foreach (Vector2 point in b)
            if (PointStrictlyInsidePolygon(point, a)) return true;

        // Handles coincident polygons and overlapping collinear edges where
        // neither set of vertices lies strictly inside the other polygon.
        Rect intersection = Rect.MinMaxRect(
            Mathf.Max(boundsA.xMin, boundsB.xMin), Mathf.Max(boundsA.yMin, boundsB.yMin),
            Mathf.Min(boundsA.xMax, boundsB.xMax), Mathf.Min(boundsA.yMax, boundsB.yMax));
        for (int x = 1; x <= 5; x++)
            for (int y = 1; y <= 5; y++)
            {
                Vector2 sample = new Vector2(
                    intersection.xMin + intersection.width * x / 6f,
                    intersection.yMin + intersection.height * y / 6f);
                if (PointStrictlyInsidePolygon(sample, a) && PointStrictlyInsidePolygon(sample, b))
                    return true;
            }
        return false;
    }

    private static Rect BoundsFor(List<Vector2> polygon)
    {
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Vector2 p in polygon)
        {
            minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y);
            maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    private static bool SegmentsCrossProperly(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float ac = Cross(b - a, c - a);
        float ad = Cross(b - a, d - a);
        float ca = Cross(d - c, a - c);
        float cb = Cross(d - c, b - c);
        return ((ac > EdgeEpsilon && ad < -EdgeEpsilon) || (ad > EdgeEpsilon && ac < -EdgeEpsilon)) &&
               ((ca > EdgeEpsilon && cb < -EdgeEpsilon) || (cb > EdgeEpsilon && ca < -EdgeEpsilon));
    }

    public static bool PointInsidePolygon(Vector2 p, List<Vector2> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            if ((a.y > p.y) != (b.y > p.y) &&
                p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    private static bool PointStrictlyInsidePolygon(Vector2 p, List<Vector2> polygon)
    {
        return !NearPolygonEdge(p, polygon, EdgeEpsilon) && PointInsidePolygon(p, polygon);
    }

    public static bool NearPolygonEdge(Vector2 p, List<Vector2> polygon, float distance)
    {
        float squared = distance * distance;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 0.000001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            if ((p - (a + t * ab)).sqrMagnitude <= squared) return true;
        }
        return false;
    }
}
