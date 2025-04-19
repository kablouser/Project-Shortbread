using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class LineRendererUI : Graphic
{
    public enum LineType
    {
        // each pair of points forms a line
        Segments,
        // all points form a line with 2 ends
        OneLine,
        // all points loop
        Loop
    }
    public LineType type;
    public float thickness;
    public List<Vector2> points;

    float width;
    float height;
    float unitWidth;
    float unitHeight;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        /*        width = rectTransform.rect.width;
                height = rectTransform.rect.height;*/

        UIVertex[] quad = new UIVertex[4];
        //public void AddUIVertexQuad(UIVertex[] verts)

        for (int i = 0; i < points.Count - 1; i++)
        {



            Vector2 point = points[i];

            Vector2 point2 = points[i + 1];



            if (i < points.Count - 1)
            {

                angle = GetAngle(points[i], points[i + 1]) + 90f;

            }



            UIVertex vertex = UIVertex.simpleVert;

            vertex.color = color;



            vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);

            vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);

            vh.AddVert(vertex);



            vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);

            vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);

            vh.AddVert(vertex);



            vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);

            vertex.position += new Vector3(unitWidth * point2.x, unitHeight * point2.y);

            vh.AddVert(vertex);



            vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);

            vertex.position += new Vector3(unitWidth * point2.x, unitHeight * point2.y);

            vh.AddVert(vertex);
        }



        for (int i = 0; i < points.Count - 1; i++)
        {

            int index = i * 4;
            vh.AddTriangle(index + 0, index + 1, index + 2);
            vh.AddTriangle(index + 1, index + 2, index + 3);
        }
    }
}
