using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SkillTreeNode
{
    // column and row in the skill tree
    public int treeX, treeY;
    // world space position
    public Vector3 worldPosition;
    public List<int>
        upwardNeighbours,
        downwardNeighbours;
}

//[Serializable]
public class SkillTreeSystem : MonoBehaviour
{
    public List<SkillTreeNode> nodes;
    // node index for every row beginning. Nodes in a row are continous
    public List<int> rowBegins;
    public Bounds2D bounds;

    public void GenerateTree(
        int maxWidth = 4,
        int height = 4)
    {
        nodes.Clear();
        rowBegins.Clear();
        bounds = new();

        rowBegins.Add(0);
        nodes.Add(new SkillTreeNode()
        {
            treeX = 0, treeY = 0,
            worldPosition = new()/*GetNodePosition(0, 0, 1)*/,
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
            int currentRowWidth = maxWidth;
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
            rowBegins.Add(rowBeginIndex);
            for (int x = 0; x < currentRowWidth; x++)
            {
                currentRow.Add(nodes.Count);
                nodes.Add(new SkillTreeNode()
                {
                    treeX = x, treeY = y,
                    worldPosition = new()/*GetNodePosition(x, y, currentRowWidth)*/,
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

        rowBegins.Add(nodes.Count);
        nodes.Add(new SkillTreeNode()
        {
            treeX = 0, treeY = height - 1,
            worldPosition = new()/*GetNodePosition(0, height - 1, 1)*/,
            upwardNeighbours = new List<int>(previousRow),
            downwardNeighbours = new List<int>(),
        });

        for (int y = 0; y < rowBegins.Count; y++)
        {
            int rowLength = GetRowLength(y);

            int sumDegrees = 0;
            for (int x = 0; x < rowLength; x++)
            {
                ref var node = ref nodes.AsSpan()[rowBegins[y] + x];
                sumDegrees += node.upwardNeighbours.Count + node.downwardNeighbours.Count;
            }

            int degreeI = 0;
            for (int x = 0; x < rowLength; x++)
            {
                ref var node = ref nodes.AsSpan()[rowBegins[y] + x];
                int degree = node.upwardNeighbours.Count + node.downwardNeighbours.Count;

                const float WORLD_WIDTH = 6.0f;
                node.worldPosition = new Vector3(
                    rowLength <= 1 ? WORLD_WIDTH * 0.5f :
                    (degreeI + degree * 0.5f) / (sumDegrees) * WORLD_WIDTH,
                    -y, 10f);

                degreeI += degree;
            }
        }
    }

    public int GetRowLength(int y)
    {
        if (0 <= y && y < rowBegins.Count)
        {
            if (y + 1 < rowBegins.Count)
                return rowBegins[y + 1] - rowBegins[y];
            else
                return 1;
        }
        return 0;
    }

    public void Awake()
    {
        GenerateTree(5, 10);
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
            Gizmos.DrawSphere(node.worldPosition, 0.2f);

            //foreach (int upwardNeighbour in node.upwardNeighbours)
            //{
            //    Gizmos.DrawLine(node.position, nodes[upwardNeighbour].position);
            //}

            foreach (int downwardNeighbour in node.downwardNeighbours)
            {
                Gizmos.DrawLine(node.worldPosition, nodes[downwardNeighbour].worldPosition);
            }
        }

    }
}
