using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A* Pathfinding System - Harvest Defense
/// Grid-based A* pathfinding for smart navigation
/// - Dynamic grid generation
/// - Obstacle detection
/// - Path smoothing
/// - Path caching
/// </summary>
public class AStarPathfinding : MonoBehaviour
{
    [Header("=== GRID SETTINGS ===")]
    [SerializeField] private Vector2 gridWorldSize = new Vector2(50, 50);
    [SerializeField] private float nodeRadius = 0.5f;
    [SerializeField] private LayerMask unwalkableMask;

    [Header("=== PATH SETTINGS ===")]
    [SerializeField] private bool smoothPath = true;
    [SerializeField] private float pathUpdateInterval = 0.5f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showGrid = false;
    [SerializeField] private bool showPath = true;

    private Node[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    public static AStarPathfinding Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        CreateGrid();
    }

    private void Start()
    {
        unwalkableMask = LayerMask.GetMask("Wall");
    }

    /// <summary>
    /// Create grid
    /// </summary>
    private void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) +
                                                       Vector3.up * (y * nodeDiameter + nodeRadius);

                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeRadius * 0.9f, unwalkableMask);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    /// <summary>
    /// Update grid (call when obstacles change)
    /// </summary>
    public void UpdateGrid()
    {
        CreateGrid();
    }

    /// <summary>
    /// Find path from start to target
    /// </summary>
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null || !targetNode.walkable)
        {
            return null;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost ||
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // No path found
    }

    /// <summary>
    /// Retrace path from end to start
    /// </summary>
    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        List<Vector3> waypoints = new List<Vector3>();
        if (smoothPath)
        {
            waypoints = SmoothPath(path);
        }
        else
        {
            foreach (Node node in path)
            {
                waypoints.Add(node.worldPosition);
            }
        }

        return waypoints;
    }

    /// <summary>
    /// Smooth path (remove unnecessary waypoints)
    /// </summary>
    private List<Vector3> SmoothPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX,
                                               path[i - 1].gridY - path[i].gridY);
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i - 1].worldPosition);
            }
            directionOld = directionNew;
        }

        if (path.Count > 0)
        {
            waypoints.Add(path[path.Count - 1].worldPosition);
        }

        return waypoints;
    }

    /// <summary>
    /// Get neighbors of a node
    /// </summary>
    private List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Get node from world position
    /// </summary>
    private Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - transform.position;
        float percentX = (localPos.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (localPos.y + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
        {
            return grid[x, y];
        }

        return null;
    }

    /// <summary>
    /// Get distance between two nodes
    /// </summary>
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    /// <summary>
    /// Check if position is walkable
    /// </summary>
    public bool IsWalkable(Vector3 worldPosition)
    {
        Node node = NodeFromWorldPoint(worldPosition);
        return node != null && node.walkable;
    }

    /// <summary>
    /// Gizmos
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));

        if (grid != null && showGrid)
        {
            foreach (Node node in grid)
            {
                Gizmos.color = node.walkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.3f);
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }

    /// <summary>
    /// Node class
    /// </summary>
    public class Node
    {
        public bool walkable;
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;

        public int gCost;
        public int hCost;
        public Node parent;

        public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY)
        {
            walkable = _walkable;
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
        }

        public int fCost
        {
            get { return gCost + hCost; }
        }
    }
}
