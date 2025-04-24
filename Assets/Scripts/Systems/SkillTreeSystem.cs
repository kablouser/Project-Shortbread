using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

[System.Serializable]
public struct SkillTreeNode
{
    // column and row in the skill tree
    public int treeX, treeY;
    // in range [0,1] x width weighted using node's degree
    public float weightedPositionX;
    public List<int>
        upwardNeighbours,
        downwardNeighbours;

    public bool unlocked;
    public bool canBeUnlocked;
}

//[Serializable]
public class SkillTreeSystem : MonoBehaviour
{
    public List<SkillTreeNode> nodes;
    // node index for every row beginning. Nodes in a row are continous
    public List<int> rowBegins;

    public ColorBlock unlockedColors = ColorBlock.defaultColorBlock;

    [Header("UI Spacing")]
    public float rowUIHeight = 100f;
    public float horizontalPadding = 100f;
    public float verticalPadding = 20f;

    [Header("References")]
    public GameObject rootUI;
    public RectTransform scrollableContentUI;
    public LineRendererUI lineRendererUI;
    public GameObject nodeUIPrefab;
    public List<GameObject> spawnedNodeUIs;

    public void GenerateTree(
        int maxWidth = 4,
        int height = 4)
    {
        nodes.Clear();
        rowBegins.Clear();

        // first node
        rowBegins.Add(0);
        nodes.Add(new SkillTreeNode()
        {
            treeX = 0, treeY = 0,
            upwardNeighbours = new List<int>(),
            downwardNeighbours = new List<int>(),
            canBeUnlocked = true,
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

        // last node
        rowBegins.Add(nodes.Count);
        nodes.Add(new SkillTreeNode()
        {
            treeX = 0, treeY = height - 1,
            upwardNeighbours = new List<int>(previousRow),
            downwardNeighbours = new List<int>(),
        });

        // position nodes
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

                node.weightedPositionX = rowLength <= 1 ? 0.5f :
                    (degreeI + degree * 0.5f) / sumDegrees;

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

    public void GenerateUI()
    {
        scrollableContentUI.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            (rowBegins.Count - 1) * rowUIHeight + 2 * verticalPadding);
        float contentWidth = scrollableContentUI.rect.width;
        float contentWidthMinusPadding = contentWidth - 2f * horizontalPadding;

        List<Vector2> uiNodePositions = new(nodes.Count);

        int nodeI = 0;
        foreach (ref SkillTreeNode node in nodes.AsSpan())
        {
            // re-use exist old nodes
            GameObject nodeUI;
            if (nodeI < spawnedNodeUIs.Count)
                nodeUI = spawnedNodeUIs[nodeI];
            else
            {
                nodeUI = Instantiate(nodeUIPrefab, scrollableContentUI);
                spawnedNodeUIs.Add(nodeUI);
            }

            uiNodePositions.Add(new Vector2(
                horizontalPadding + contentWidthMinusPadding * node.weightedPositionX,
                -(verticalPadding + rowUIHeight * node.treeY)));
            nodeUI.GetComponent<RectTransform>().anchoredPosition = uiNodePositions[^1];

            Button button = nodeUI.GetComponent<Button>();
            button.interactable = node.canBeUnlocked;
            button.onClick.RemoveAllListeners();
            int localNodeI = nodeI;
            button.onClick.AddListener(() => { OnTreeNodeClicked(localNodeI); });

            nodeI++;
        }

        // delete extra spawnNodeUIs
        for (; nodeI < spawnedNodeUIs.Count; nodeI++)
        {
            Destroy(spawnedNodeUIs[nodeI]);
        }
        spawnedNodeUIs.RemoveRange(nodes.Count, spawnedNodeUIs.Count - nodes.Count);

        lineRendererUI.points.Clear();
        lineRendererUI.type = LineRendererUI.LineType.Segments;
        nodeI = 0;
        foreach (ref SkillTreeNode node in nodes.AsSpan())
        {
            foreach (int downwardNeighbour in node.downwardNeighbours)
            {
                lineRendererUI.points.Add(uiNodePositions[nodeI]);
                lineRendererUI.points.Add(uiNodePositions[downwardNeighbour]);
            }
            nodeI++;
        }
    }

    public void OnTreeNodeClicked(int nodeI)
    {
        if (0 <= nodeI && nodeI < nodes.Count)
        {
            ref SkillTreeNode node = ref nodes.AsSpan()[nodeI];
            node.unlocked = true;
            spawnedNodeUIs[nodeI].GetComponent<Button>().colors = unlockedColors;

            foreach (int neighbour in node.downwardNeighbours)
            {
                nodes.AsSpan()[neighbour].canBeUnlocked = true;
                spawnedNodeUIs[neighbour].GetComponent<Button>().interactable = true;
            }
            foreach (int neighbour in node.upwardNeighbours)
            {
                nodes.AsSpan()[neighbour].canBeUnlocked = true;
                spawnedNodeUIs[neighbour].GetComponent<Button>().interactable = true;
            }
        }
    }

    public void Start()
    {
        GenerateTree(5, 10);
        GenerateUI();
    }

    public void OnDestroy()
    {
        foreach (GameObject nodeUI in spawnedNodeUIs)
        {
            Button button;
            if (null != nodeUI &&
                null != (button = nodeUI.GetComponent<Button>()))
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }

    void OnDrawGizmos()
    {
        return;
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

        Vector3 GetNodePosition(in SkillTreeNode node)
        {
            return new Vector3(node.weightedPositionX * 6.0f, node.treeY);
        }

        foreach (var node in nodes)
        {
            Gizmos.DrawSphere(GetNodePosition(node), 0.2f);

            //foreach (int upwardNeighbour in node.upwardNeighbours)
            //{
            //    Gizmos.DrawLine(node.position, nodes[upwardNeighbour].position);
            //}

            foreach (int downwardNeighbour in node.downwardNeighbours)
            {
                Gizmos.DrawLine(GetNodePosition(node), GetNodePosition(nodes[downwardNeighbour]));
            }
        }
    }
}
