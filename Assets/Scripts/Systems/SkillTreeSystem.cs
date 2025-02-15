using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SkillTreeNode
{
    public Vector3 position;
    public List<int>
        upwardNeighbours,
        downwardNeighbours;
}

//[Serializable]
public class SkillTreeSystem : MonoBehaviour
{
    public List<SkillTreeNode> nodes;
    public List<int> rowStartIndices;

    public void GenerateTree(
        int maxLinearLength,
        int maxConvergesPerRow,
        int maxWidth,
        int nodesCount)
    {
        nodes.Add(new SkillTreeNode() {
            position = GetNodePosition(0, 0, 1),
            upwardNeighbours = new List<int>(),
            downwardNeighbours = new List<int>() {1, 2, 3},
        });

        List<int> setUpwardNodeNeighbour = new List<int>() { 0, 0, 0 };

        for (int y = 1; y < 4; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                int addIndex = nodes.Count;
                nodes.Add(new SkillTreeNode()
                {
                    position = GetNodePosition(x, y, 3),
                    upwardNeighbours = new List<int>() { setUpwardNodeNeighbour[x] },
                    downwardNeighbours = new List<int>() { y + 1 < 4 ? addIndex + 3 : 1 + 3 * 3 },
                });

                setUpwardNodeNeighbour[x] = addIndex;
            }
        }

        int lastIndex = nodes.Count;
        nodes.Add(new SkillTreeNode()
        {
            position = GetNodePosition(0, 4, 1),
            upwardNeighbours = new List<int>() { lastIndex - 3, lastIndex - 2, lastIndex - 1 },
            downwardNeighbours = new List<int>() {},
        });
    }

    public void GenerateTree2(
        int maxLinearLength,
        int maxConvergesPerRow,
        int maxWidth,
        int height)
    {
        nodes.Add(new SkillTreeNode()
        {
            position = GetNodePosition(0, 0, 1),
            upwardNeighbours = new List<int>(),
            downwardNeighbours = new List<int>(),
        });

        List<int> previousRow = new List<int>() { 0 };
        List<int> currentRow = new List<int>();
        for (int y = 1; y < height - 1; y++)
        {
            int randomWidth = Random.Range(3, maxWidth + 1 /*exclusive*/);

            int rowBeginIndex = nodes.Count;
            for (int x = 0; x < randomWidth; x++)
            {
                currentRow.Add(nodes.Count);
                nodes.Add(new SkillTreeNode()
                {
                    position = GetNodePosition(x, y, randomWidth),
                    upwardNeighbours = new List<int>(),
                    downwardNeighbours = new List<int>(),
                });
            }

            int connectIndex = 0;
            int prevI = 0;
            int averageConnectionsPerNode = Mathf.RoundToInt(randomWidth / Mathf.Max(1f,(float)previousRow.Count));
            foreach (var node in previousRow)
            {
                int minConnections = previousRow.Count - 1 <= prevI ? randomWidth - connectIndex : 1;
                int maxConnections = Mathf.Max(minConnections, Mathf.Min(/*stop branching*/2, randomWidth - connectIndex));
                int connections = averageConnectionsPerNode + Random.Range(-1,2/*exclusive range*/);
                connections = Mathf.Clamp(connections, minConnections, maxConnections);

                for (int c = 0; c < connections; c++)
                {
                    nodes[node].downwardNeighbours.Add(rowBeginIndex + connectIndex + c);
                    nodes[rowBeginIndex + connectIndex + c].upwardNeighbours.Add(node);
                }
                connectIndex += connections;
                if (prevI == randomWidth - 1)
                {
                    connectIndex--;
                }
                else
                {
                    if (Random.value < 0.05f)
                    {
                        connectIndex--;
                    }
                }
                prevI++;
            }

            previousRow.Clear();
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        int lastIndex = nodes.Count;
        foreach (var node in previousRow)
        {
            nodes[node].downwardNeighbours.Add(lastIndex);
        }

        nodes.Add(new SkillTreeNode()
        {
            position = GetNodePosition(0, height - 1, 1),
            upwardNeighbours = new List<int>(previousRow),
            downwardNeighbours = new List<int>(),
        });
    }

    private void Awake()
    {
        GenerateTree2(1, 1, 5, 10);
    }

    void OnDrawGizmos()
    {
        
        // verify connections
        int i = 0;
        foreach (var node in nodes)
        {
            foreach (int upwardNeighbour in node.upwardNeighbours)
            {
                Debug.Assert(nodes[upwardNeighbour].downwardNeighbours.Contains(i));
            }

            foreach (int downwardNeighbour in node.downwardNeighbours)
            {
                Debug.Assert(nodes[downwardNeighbour].upwardNeighbours.Contains(i));
            }
            i++;
        }

        foreach (var node in nodes)
        {
            Gizmos.DrawSphere(node.position, 0.2f);

            //foreach (int upwardNeighbour in node.upwardNeighbours)
            //{
            //    Gizmos.DrawLine(node.position, nodes[upwardNeighbour].position);
            //}

            foreach (int downwardNeighbour in node.downwardNeighbours)
            {
                Gizmos.DrawLine(node.position, nodes[downwardNeighbour].position);
            }
        }

    }

    public static Vector3 GetNodePosition(int x, int y, int rowLength)
    {
        return new Vector3(
            rowLength <= 1 ? 0f :
            (x / (float)(rowLength - 1f) - 0.5f) * 3f,
            -y, 10f);
    }
}
