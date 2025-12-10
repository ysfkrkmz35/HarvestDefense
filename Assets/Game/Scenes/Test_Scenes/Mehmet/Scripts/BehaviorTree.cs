using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Behavior Tree System - Harvest Defense
/// Modular AI decision-making system
/// - Node-based behavior structure
/// - Composable behaviors
/// - Easy to extend and debug
/// </summary>
public class BehaviorTree
{
    private Node root;

    public BehaviorTree(Node rootNode)
    {
        root = rootNode;
    }

    public NodeState Tick()
    {
        return root?.Evaluate() ?? NodeState.FAILURE;
    }

    // ===== NODE STATES =====
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    // ===== BASE NODE =====
    public abstract class Node
    {
        protected NodeState state;
        protected List<Node> children = new List<Node>();
        protected Dictionary<string, object> dataContext = new Dictionary<string, object>();

        public Node() { }
        public Node(List<Node> children)
        {
            this.children = children;
        }

        public abstract NodeState Evaluate();

        public void SetData(string key, object value)
        {
            dataContext[key] = value;
        }

        public object GetData(string key)
        {
            if (dataContext.ContainsKey(key))
                return dataContext[key];

            Node node = null;
            foreach (Node child in children)
            {
                object value = child.GetData(key);
                if (value != null)
                    return value;
            }

            return null;
        }

        public bool ClearData(string key)
        {
            if (dataContext.ContainsKey(key))
            {
                dataContext.Remove(key);
                return true;
            }

            foreach (Node child in children)
            {
                if (child.ClearData(key))
                    return true;
            }

            return false;
        }
    }

    // ===== COMPOSITE NODES =====

    /// <summary>
    /// Sequence: Run children in order, fail if any fails
    /// </summary>
    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        state = NodeState.FAILURE;
                        return state;
                    case NodeState.SUCCESS:
                        continue;
                    case NodeState.RUNNING:
                        anyChildRunning = true;
                        continue;
                }
            }

            state = anyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return state;
        }
    }

    /// <summary>
    /// Selector: Run children until one succeeds
    /// </summary>
    public class Selector : Node
    {
        public Selector() : base() { }
        public Selector(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        continue;
                    case NodeState.SUCCESS:
                        state = NodeState.SUCCESS;
                        return state;
                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state;
                }
            }

            state = NodeState.FAILURE;
            return state;
        }
    }

    /// <summary>
    /// Inverter: Invert child result
    /// </summary>
    public class Inverter : Node
    {
        private Node child;

        public Inverter(Node child)
        {
            this.child = child;
        }

        public override NodeState Evaluate()
        {
            switch (child.Evaluate())
            {
                case NodeState.FAILURE:
                    state = NodeState.SUCCESS;
                    return state;
                case NodeState.SUCCESS:
                    state = NodeState.FAILURE;
                    return state;
                case NodeState.RUNNING:
                    state = NodeState.RUNNING;
                    return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }

    /// <summary>
    /// Repeater: Repeat child N times or until it fails
    /// </summary>
    public class Repeater : Node
    {
        private Node child;
        private int repeatCount;
        private int currentCount = 0;

        public Repeater(Node child, int repeatCount = -1)
        {
            this.child = child;
            this.repeatCount = repeatCount;
        }

        public override NodeState Evaluate()
        {
            if (repeatCount != -1 && currentCount >= repeatCount)
            {
                currentCount = 0;
                state = NodeState.SUCCESS;
                return state;
            }

            NodeState childState = child.Evaluate();

            if (childState == NodeState.RUNNING)
            {
                state = NodeState.RUNNING;
                return state;
            }

            if (childState == NodeState.FAILURE)
            {
                currentCount = 0;
                state = NodeState.FAILURE;
                return state;
            }

            currentCount++;
            state = NodeState.RUNNING;
            return state;
        }
    }

    // ===== DECORATOR NODES =====

    /// <summary>
    /// Succeeder: Always return success
    /// </summary>
    public class Succeeder : Node
    {
        private Node child;

        public Succeeder(Node child)
        {
            this.child = child;
        }

        public override NodeState Evaluate()
        {
            child.Evaluate();
            state = NodeState.SUCCESS;
            return state;
        }
    }

    /// <summary>
    /// UntilFail: Repeat until child fails
    /// </summary>
    public class UntilFail : Node
    {
        private Node child;

        public UntilFail(Node child)
        {
            this.child = child;
        }

        public override NodeState Evaluate()
        {
            NodeState childState = child.Evaluate();

            if (childState == NodeState.FAILURE)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}

// ===== ENEMY-SPECIFIC BEHAVIOR NODES =====

/// <summary>
/// Check if player is in vision
/// </summary>
public class CheckPlayerInVision : BehaviorTree.Node
{
    private EnemyAI enemy;

    public CheckPlayerInVision(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public override BehaviorTree.NodeState Evaluate()
    {
        Transform player = enemy.GetPlayerTransform();
        if (player == null)
        {
            state = BehaviorTree.NodeState.FAILURE;
            return state;
        }

        if (enemy.CanSeePlayer())
        {
            SetData("target", player);
            state = BehaviorTree.NodeState.SUCCESS;
            return state;
        }

        state = BehaviorTree.NodeState.FAILURE;
        return state;
    }
}

/// <summary>
/// Check if in attack range
/// </summary>
public class CheckInAttackRange : BehaviorTree.Node
{
    private EnemyAI enemy;
    private float attackRange;

    public CheckInAttackRange(EnemyAI enemy, float attackRange)
    {
        this.enemy = enemy;
        this.attackRange = attackRange;
    }

    public override BehaviorTree.NodeState Evaluate()
    {
        Transform target = GetData("target") as Transform;
        if (target == null)
        {
            state = BehaviorTree.NodeState.FAILURE;
            return state;
        }

        float distance = Vector2.Distance(enemy.transform.position, target.position);
        if (distance <= attackRange)
        {
            state = BehaviorTree.NodeState.SUCCESS;
            return state;
        }

        state = BehaviorTree.NodeState.FAILURE;
        return state;
    }
}

/// <summary>
/// Task: Attack target
/// </summary>
public class TaskAttack : BehaviorTree.Node
{
    private EnemyAI enemy;
    private float attackCooldown;
    private float lastAttackTime = -999f;

    public TaskAttack(EnemyAI enemy, float attackCooldown)
    {
        this.enemy = enemy;
        this.attackCooldown = attackCooldown;
    }

    public override BehaviorTree.NodeState Evaluate()
    {
        Transform target = GetData("target") as Transform;
        if (target == null)
        {
            state = BehaviorTree.NodeState.FAILURE;
            return state;
        }

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            enemy.PerformAttackBT(target);
            lastAttackTime = Time.time;
            state = BehaviorTree.NodeState.SUCCESS;
            return state;
        }

        state = BehaviorTree.NodeState.RUNNING;
        return state;
    }
}

/// <summary>
/// Task: Move to target
/// </summary>
public class TaskMoveToTarget : BehaviorTree.Node
{
    private EnemyAI enemy;

    public TaskMoveToTarget(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public override BehaviorTree.NodeState Evaluate()
    {
        Transform target = GetData("target") as Transform;
        if (target == null)
        {
            state = BehaviorTree.NodeState.FAILURE;
            return state;
        }

        enemy.MoveTowardsBT(target.position);
        state = BehaviorTree.NodeState.RUNNING;
        return state;
    }
}

/// <summary>
/// Task: Wander around
/// </summary>
public class TaskWander : BehaviorTree.Node
{
    private EnemyAI enemy;

    public TaskWander(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public override BehaviorTree.NodeState Evaluate()
    {
        enemy.WanderBT();
        state = BehaviorTree.NodeState.RUNNING;
        return state;
    }
}

/// <summary>
/// Check if stuck
/// </summary>
public class CheckIfStuck : BehaviorTree.Node
{
    private EnemyAI enemy;

    public CheckIfStuck(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public override BehaviorTree.NodeState Evaluate()
    {
        if (enemy.IsStuck())
        {
            state = BehaviorTree.NodeState.SUCCESS;
            return state;
        }

        state = BehaviorTree.NodeState.FAILURE;
        return state;
    }
}

/// <summary>
/// Task: Unstuck (random movement)
/// </summary>
public class TaskUnstuck : BehaviorTree.Node
{
    private EnemyAI enemy;

    public TaskUnstuck(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public override BehaviorTree.NodeState Evaluate()
    {
        enemy.UnstuckBT();
        state = BehaviorTree.NodeState.RUNNING;
        return state;
    }
}
