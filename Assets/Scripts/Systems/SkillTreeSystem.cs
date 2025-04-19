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

    public void GenerateTree(
        int maxLinearLength,
        int maxConvergesPerRow,
        int maxWidth,
        int height)
    {
        nodes.Clear();
        nodes.Add(new SkillTreeNode()
        {
            position = GetNodePosition(0, 0, 1),
            upwardNeighbours = new List<int>(),
            downwardNeighbours = new List<int>(),
        });

        List<int> previousRow = new List<int>() { 0 };
        List<int> currentRow = new List<int>();
        // is node i and i + 1 sharing a connection
        // Count = previousRow.Count - 1, because its only connections between nodes
        List<bool> isNodeConnectionShared = new List<bool>();
        // degree of each node in prev row
        List<int> previousRowDegrees = new List<int>();
        for (int y = 1; y < height - 1; y++)
        {
            int previousRowWidth = previousRow.Count;
            const int currentRowWidth = 4;
            int sharedConnections = //Random.Range(Mathf.Max(0, previousRowWidth - currentRowWidth), previousRowWidth - 1 + 1 /*+1 because exclusive*/);
                NormalRandom.Range(Mathf.Max(0, previousRowWidth - currentRowWidth), previousRowWidth - 1 + 1 /*+1 because exclusive*/, 1f, 0.8f);
            int uniqueConnections = previousRowWidth - sharedConnections - 1;

            // spread connection types randomly
            isNodeConnectionShared.Clear();
            for (int connectionI = 0; connectionI < previousRowWidth - 1; connectionI++)
            {
                if (Random.Range(0, sharedConnections + uniqueConnections) < sharedConnections)
                {
                    sharedConnections--;
                    isNodeConnectionShared.Add(true);
                }
                else
                {
                    uniqueConnections--;
                    isNodeConnectionShared.Add(false);
                }
            }

            // assume all prev row nodes are allocated the minimum degree
            int nodesMinAllocate = 1;
            foreach (bool isShared in isNodeConnectionShared)
            {
                nodesMinAllocate += isShared ? 0 : 1;
            }

            // degree allocation
            previousRowDegrees.Clear();
            for (int i = 0; i < previousRowWidth; i++)
            {
                previousRowDegrees.Add(1);
            }

            // how many currentRow nodes left to allocate now?
            int nodesToAllocate = currentRowWidth - nodesMinAllocate;
            for (int nodeToAllocateI = 0; nodeToAllocateI < nodesToAllocate; nodeToAllocateI++)
            {
                // TODO debug if this works
                previousRowDegrees[Random.Range(0, previousRowWidth)]++;
            }

            // map degrees to real indices
            int rowBeginIndex = nodes.Count;
            for (int x = 0; x < currentRowWidth; x++)
            {
                currentRow.Add(nodes.Count);
                nodes.Add(new SkillTreeNode()
                {
                    position = GetNodePosition(x, y, currentRowWidth),
                    upwardNeighbours = new List<int>(),
                    downwardNeighbours = new List<int>(),
                });
            }

            int previousRowI = 0;
            int currentRowI = 0;
            foreach (int previousRowNode in previousRow)
            {
                if (0 < previousRowI && isNodeConnectionShared[previousRowI - 1])
                {
                    // share last node
                    currentRowI--;
                }

                for (int degreeI = 0; degreeI < previousRowDegrees[previousRowI]; degreeI++)
                {
                    int currentRowNode = currentRow[currentRowI];
                    nodes[previousRowNode].downwardNeighbours.Add(currentRowNode);
                    nodes[currentRowNode].upwardNeighbours.Add(previousRowNode);
                    currentRowI++;
                }
                previousRowI++;
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

    public void Awake()
    {
        GenerateTree(1, 1, 5, 10);
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


    [Range(0, 2)]
    public float dist = 1f;
    [Range(0,1.1f)]
    public float bias = 0.5f;

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
 
            //System.Diagnostics.Debugger.Break();

            VisualiseDistribution.WithLineRendererInt(
                () => NormalRandom.Range(1, 21, dist, bias),
                GetComponent<LineRenderer>(),
                1, 21,
                1000000);
        }
    }
}
