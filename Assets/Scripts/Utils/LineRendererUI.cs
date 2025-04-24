using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class LineRendererUI : MaskableGraphic
{
    public enum LineType
    {
        // all points form a line with 2 ends
        OneLine,
        // all points loop
        Loop,
        // each pair of points forms a line
        Segments,
    }
    public LineType type;
    public float thickness;
    // origin is the RectTransform's pivot, distances are in pixels with scaling
    // please call SetVerticesDirty() whenever this changes
    public List<Vector2> points;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Count < 2) return;

        UIVertex vertex = UIVertex.simpleVert;
        UIVertex[] quad = new UIVertex[4];

        switch (type)
        {
            default:
            case LineType.OneLine:
            case LineType.Loop:
                for (int i = 0; i + 1 < points.Count; i++)
                {
                    AddLineBetween2Points(vh, quad, points[i], points[i + 1], ref vertex);
                }
                if (type == LineType.Loop)
                {
                    AddLineBetween2Points(vh, quad, points[0], points[points.Count - 1], ref vertex);
                }
                break;

            case LineType.Segments:
                for (int i = 0; i + 1 < points.Count; i += 2)
                {
                    AddLineBetween2Points(vh, quad, points[i], points[i + 1], ref vertex);
                }
                break;
        }

        void AddLineBetween2Points(VertexHelper vh, UIVertex[] quad, in Vector2 point, in Vector2 point2, ref UIVertex vertex)
        {
            vertex.color = color;

            Vector3 lineNormal = thickness * 0.5f * Vector2.Perpendicular(point2 - point).normalized;

            vertex.position = (Vector3)point + lineNormal;
            quad[0] = vertex;

            vertex.position = (Vector3)point - lineNormal;
            quad[1] = vertex;

            vertex.position = (Vector3)point2 - lineNormal;
            quad[2] = vertex;

            vertex.position = (Vector3)point2 + lineNormal;
            quad[3] = vertex;

            vh.AddUIVertexQuad(quad);
        }
    }
}
