using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct NavigationNode
{
    public const float MOMENTUM_SCALE = 10f;

    // combined scaled momentum of non-blocking colliders
    public Vector2Int scaledMomentum;
    // number of blocking colliders
    public int blocking;

    public static NavigationNode Blocking()
    {
        return new NavigationNode
        {
            scaledMomentum = new Vector2Int(),
            blocking = 1,
        };
    }

    public static NavigationNode FromCollider(Collider2D collider, Vector2 fixedDelta = new Vector2())
    {
        Rigidbody2D rigidbody = collider.attachedRigidbody;
        float movingThreshold = Time.fixedDeltaTime * 1.69f;
        if (null != rigidbody && movingThreshold * movingThreshold < fixedDelta.sqrMagnitude)
        {
            Vector2 scaledMomentum = rigidbody.mass * fixedDelta * MOMENTUM_SCALE;
            return new NavigationNode
            {
                scaledMomentum = new Vector2Int(Mathf.RoundToInt(scaledMomentum.x), Mathf.RoundToInt(scaledMomentum.y)),
                blocking = 0,
            };
        }
        else
        {
            return Blocking();
        }
    }

    public static NavigationNode operator -(in NavigationNode other)
    {
        return new NavigationNode
        {
            scaledMomentum = -other.scaledMomentum,
            blocking = -other.blocking,
        };
    }

    public void Add(in NavigationNode other)
    {
        scaledMomentum += other.scaledMomentum;
        blocking += other.blocking;
    }

    /// <summary>
    /// Predict final combined momentum when added with another momentum. Still in rounded scale.
    /// </summary>
    public Vector2Int CombineScaledMomentum(in NavigationNode other)
    {
        if (0 < blocking || 0 < other.blocking)
        {
            return new Vector2Int();
        }
        return scaledMomentum + other.scaledMomentum;
    }
}

[Serializable]
public struct NavigationGrid
{
    // in each grid spot, sum of all physics colliders momentums
    public Array2D<NavigationNode> nodes;
    public Bounds2D bounds;

    public void SetElementSizeUninitialised(in Bounds2D inLowerBounds, in Vector2 elementSize)
    {
        Vector2 boundsSize = inLowerBounds.size;
        Vector2Int nodesDimensions = new Vector2Int(
            Mathf.CeilToInt(boundsSize.x / elementSize.x),
            Mathf.CeilToInt(boundsSize.y / elementSize.y));
        nodes.dimension0 = nodesDimensions.x;
        nodes.dimension1 = nodesDimensions.y;

        bounds = new Bounds2D
        {
            min = inLowerBounds.min,
            max = inLowerBounds.min + elementSize * new Vector2(nodes.dimension0, nodes.dimension1),
        };
    }

    public void Snapshot(LayerMask layerMask)
    {
        nodes = new Array2D<NavigationNode>(nodes.dimension0, nodes.dimension1);
        Collider2D[] overlappedColliders = Physics2D.OverlapAreaAll(bounds.min, bounds.max, layerMask);
        Span<(Bounds2D, NavigationNode)> startBoundsAndMomentums = stackalloc (Bounds2D, NavigationNode)[overlappedColliders.Length];
        for (int i = 0; i < overlappedColliders.Length; ++i)
        {
            startBoundsAndMomentums[i] = (new Bounds2D(overlappedColliders[i].bounds), NavigationNode.Blocking());
        }
        AddBounds(startBoundsAndMomentums, GetElementSize());
    }

    public void AddBounds(in Bounds2D addBound, in NavigationNode momentum, in Vector2 elementSize)
    {
        GetBoundsIndex(elementSize, addBound, out Vector2Int min, out Vector2Int max);
        min = Vector2Int.Max(min, new Vector2Int(0, 0));
        max = Vector2Int.Min(max, new Vector2Int(nodes.dimension0, nodes.dimension1));

        for (int x = min.x; x < max.x; x++)
        {
            for (int y = min.y; y < max.y; y++)
            {
                nodes[x, y].Add(momentum);
            }
        }
    }

    public void AddBounds(in Span<(Bounds2D, NavigationNode)> addBoundsAndMomemtums, in Vector2 elementSize)
    {
        foreach (ref (Bounds2D, NavigationNode) boundsAndMomemtum in addBoundsAndMomemtums)
        {
            GetBoundsIndex(elementSize, boundsAndMomemtum.Item1, out Vector2Int min, out Vector2Int max);
            min = Vector2Int.Max(min, new Vector2Int(0, 0));
            max = Vector2Int.Min(max, new Vector2Int(nodes.dimension0, nodes.dimension1));

            for (int x = min.x; x < max.x; x++)
            {
                for (int y = min.y; y < max.y; y++)
                {
                    nodes[x, y].Add(boundsAndMomemtum.Item2);
                }
            }
        }
    }

    public Vector2 GetElementSize()
    {
        Vector2 boundsSize = bounds.size;
        return new Vector2(boundsSize.x / nodes.dimension0, boundsSize.y / nodes.dimension1);
    }

    public void GetBoundsIndex(
        in Vector2 elementSize, in Bounds2D elementBounds,
        out Vector2Int min, out Vector2Int max)
    {
        min = new Vector2Int(
            Mathf.FloorToInt((elementBounds.min.x - bounds.min.x) / elementSize.x),
            Mathf.FloorToInt((elementBounds.min.y - bounds.min.y) / elementSize.y));

        max = new Vector2Int(
            Mathf.CeilToInt((elementBounds.max.x - bounds.min.x) / elementSize.x),
            Mathf.CeilToInt((elementBounds.max.y - bounds.min.y) / elementSize.y));
    }

    public Vector2Int GetIndex(in Vector2 elementSize, in Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt((position.x - bounds.min.x) / elementSize.x),
            Mathf.FloorToInt((position.y - bounds.min.y) / elementSize.y));
    }

    public Vector2 GetElementWorldPosition(in Vector2 boundsSize, in Vector2 halfElementSize, in Vector2Int index)
    {
        return new Vector2(
            index.x / (float)nodes.dimension0 * boundsSize.x + bounds.min.x + halfElementSize.x,
            index.y / (float)nodes.dimension1 * boundsSize.y + bounds.min.y + halfElementSize.y);
    }

    // Snaps world position to nearest indexed point on grid clamped to grid bounds
    public Vector3 RoundClampWorldPosition(in Vector2 boundsSize, in Vector2 elementSize, in Vector2 halfElementSize, in Vector2 position)
    {
        Vector2Int index = GetIndex(elementSize, position);
        index.x = Mathf.Clamp(index.x, 0, nodes.dimension0);
        index.y = Mathf.Clamp(index.y, 0, nodes.dimension1);
        return GetElementWorldPosition(boundsSize, halfElementSize, index);
    }

    // Snaps world position to nearest indexed point on grid, if outside grid false
    public bool TryRoundWorldPosition(
        in Vector2 boundsSize, in Vector2 elementSize, in Vector2 halfElementSize, in Vector2 position,
        out Vector3 roundedPosition)
    {
        Vector2Int index = GetIndex(elementSize, position);
        if (nodes.InRange(index))
        {
            roundedPosition = GetElementWorldPosition(boundsSize, halfElementSize, index);
            return true;
        }
        else
        {
            roundedPosition = default;
            return false;
        }
    }

    public Vector2Int RoundClampWorldPositionToIndex(in Vector2 elementSize, in Vector2 position)
    {
        Vector2Int index = GetIndex(elementSize, position);
        index.x = Mathf.Clamp(index.x, 0, nodes.dimension0);
        index.y = Mathf.Clamp(index.y, 0, nodes.dimension1);
        return index;
    }

    public bool TryIndex(
        in Vector2 elementSize, in Vector2 position,
        out Vector2Int index)
    {
        index = GetIndex(elementSize, position);
        return nodes.InRange(index);
    }

    // Box with even size dimension will lie on the element boundaries. But with odd size dimension will lie on element middle.
    public static Vector2 ConvertBoxToElementCenter(in Vector2 halfElementSize, in Vector2 boxCenter, in Vector2Int boxSize)
    {
        return new Vector2(
            boxSize.x % 2 == 0 ?
                boxCenter.x - halfElementSize.x :
                boxCenter.x,
            boxSize.y % 2 == 0 ?
                boxCenter.y - halfElementSize.y :
                boxCenter.y);
    }

    // Box with even size dimension will lie on the element boundaries. But with odd size dimension will lie on element middle.
    public static Vector2 ConvertElementToBoxCenter(in Vector2 halfElementSize, in Vector2 nodeCenter, in Vector2Int boxSize)
    {
        return new Vector2(
            boxSize.x % 2 == 0 ?
                nodeCenter.x + halfElementSize.x :
                nodeCenter.x,
            boxSize.y % 2 == 0 ?
                nodeCenter.y + halfElementSize.y :
                nodeCenter.y);
    }

    public bool FindOpenElementNextToBox(
        in Vector2 elementSize, in Vector2 halfElementSize,
        in Vector2 boxCenter, in Vector2Int boxSize,
        out Vector2 openElementWorld)
    {
        Vector2 halfBoxSize = (Vector2)boxSize * 0.5f;

        // start bottom
        Vector2Int currentNodeIndex = GetIndex(elementSize, boxCenter + new Vector2(0, -halfBoxSize.y - halfElementSize.y));

        Span<Vector2Int> targets = stackalloc Vector2Int[5]
        {
            // bottom left
            GetIndex(elementSize, boxCenter - halfBoxSize - halfElementSize),
            new Vector2Int(), new Vector2Int(), new Vector2Int(), new Vector2Int(),
        };
        // top left
        targets[1] = targets[0] + new Vector2Int(0, boxSize.y + 1);
        // top right
        targets[2] = targets[0] + new Vector2Int(boxSize.x + 1, boxSize.y + 1);
        // bottom right
        targets[3] = targets[0] + new Vector2Int(boxSize.x + 1, 0);
        // back to start bottom
        targets[4] = currentNodeIndex;

        for (int i = 0; i < targets.Length; i++)
        {
            do
            {
                if (nodes.TryIndex(currentNodeIndex, out NavigationNode node))
                {
                    if (node.blocking <= 0)
                    {
                        openElementWorld = GetElementWorldPosition(bounds.size, halfElementSize, currentNodeIndex);
                        return true;
                    }
                }

                Vector2Int diff = targets[i] - currentNodeIndex;
                // set manitude to 1
                diff.x = Math.Sign(diff.x);
                diff.y = Math.Sign(diff.y);

                // move towards target
                currentNodeIndex += diff;
            }
            while (currentNodeIndex != targets[i]);
        }

        openElementWorld = default;
        return false;
    }

    // includes diagonals
    public static void GetNeighbours(in Vector2Int index, Span<Vector2Int> neighbours)
    {
        neighbours[0] = index + new Vector2Int(1, 0);
        neighbours[1] = index + new Vector2Int(1, 1);
        neighbours[2] = index + new Vector2Int(0, 1);
        neighbours[3] = index + new Vector2Int(-1, 1);
        neighbours[4] = index + new Vector2Int(-1, 0);
        neighbours[5] = index + new Vector2Int(-1, -1);
        neighbours[6] = index + new Vector2Int(0, -1);
        neighbours[7] = index + new Vector2Int(1, -1);
    }

#if UNITY_EDITOR
    public void OnSceneGUI()
    {
        // draw grid
        if (0 < nodes.dimension0 * nodes.dimension1)
        {
            Vector3[] lineSegs = new Vector3[(nodes.dimension0 + 1 + nodes.dimension1 + 1) * 2];

            Vector2 elementSize = GetElementSize();
            Vector2 min = bounds.min;
            Vector2 max = bounds.max;

            int lineSegsI = 0;
            for (int i = 0; i <= nodes.dimension0; i++)
            {
                lineSegs[lineSegsI] = new Vector3(min.x + i * elementSize.x, min.y);
                lineSegs[lineSegsI + 1] = new Vector3(min.x + i * elementSize.x, max.y);
                lineSegsI += 2;
            }
            for (int i = 0; i <= nodes.dimension1; i++)
            {
                lineSegs[lineSegsI] = new Vector3(min.x, min.y + i * elementSize.y);
                lineSegs[lineSegsI + 1] = new Vector3(max.x, min.y + i * elementSize.y);
                lineSegsI += 2;
            }
            Handles.DrawLines(lineSegs);
        }

        // draw overlap nums
        if (nodes.dimension0 * nodes.dimension1 == nodes.elements.Length)
        {
            Vector2 boundsSize = bounds.size;
            Vector2 halfElementSize = GetElementSize() * 0.5f;

            for (int x = 0; x < nodes.dimension0; x++)
            {
                for (int y = 0; y < nodes.dimension1; y++)
                {
                    Vector3 worldPos = GetElementWorldPosition(boundsSize, halfElementSize, new Vector2Int(x, y));

                    Handles.Label(
                        worldPos,
                        $"{nodes[x, y].blocking}");

                    Vector3 realMomemtum = (Vector2)nodes[x, y].scaledMomentum / NavigationNode.MOMENTUM_SCALE;

                    Handles.DrawLine(
                        worldPos,
                        worldPos + realMomemtum,
                        0.2f);
                }
            }
        }
    }
#endif
}
