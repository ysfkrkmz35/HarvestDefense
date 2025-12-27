using UnityEngine;


[ExecuteAlways]
public class SwordVisualAligner : MonoBehaviour
{
    public enum DebugFacing { None, Up, Down, Left, Right }
    [Header("Debug")]
    public DebugFacing TestDirection = DebugFacing.None;

    [Header("Right (DirX > 0)")]
    public Vector3 PosRight = new Vector3(0, 0, -0.5f);
    public Vector3 RotRight = new Vector3(0, 0, -90);
    public Vector3 ScaleRight = new Vector3(1.29f, 1.09f, 1f);

    [Header("Left (DirX < 0)")]
    public Vector3 PosLeft = new Vector3(0, 0, -0.5f);
    public Vector3 RotLeft = new Vector3(0, 0, 90); // Blade points left
    public Vector3 ScaleLeft = new Vector3(1.29f, 1.09f, 1f);

    [Header("Up (DirY > 0)")]
    public Vector3 PosUp = new Vector3(0, 0, 0.5f); // Behind player
    public Vector3 RotUp = new Vector3(0, 0, -45); // Angled up-right
    public Vector3 ScaleUp = new Vector3(1.29f, 1.09f, 1f);

    [Header("Down (DirY < 0)")]
    public Vector3 PosDown = new Vector3(0, 0, -0.5f); // In front
    public Vector3 RotDown = new Vector3(0, 0, -135); // Angled down-right
    public Vector3 ScaleDown = new Vector3(1.29f, 1.09f, 1f);

    private Transform _transform;
    private Animator _playerAnimator;
    private Renderer _renderer;
    
    [Header("Sorting Order (Higher = In Front)")]
    public int SortingOrderFront = 50;  // When facing down (front view)
    public int SortingOrderBack = 0;    // When facing up (back view)
    public int SortingOrderSide = 25;   // When facing left/right

    void Start()
    {
        _transform = transform;
        _renderer = GetComponentInChildren<Renderer>();
        
        if (Application.isPlaying) {
            _playerAnimator = GetComponentInParent<Animator>();
            if (_playerAnimator == null) 
                Debug.LogError($"[SwordVisualAligner] Could not find Animator in parents of {gameObject.name}!");

            // DISABLE any Animator ON THIS OBJECT
            var localAnimator = GetComponent<Animator>();
            if (localAnimator != null) {
                localAnimator.enabled = false;
            }
            var childAnimator = GetComponentInChildren<Animator>();
            if (childAnimator != null && childAnimator != _playerAnimator) {
                childAnimator.enabled = false;
            }
        }
    }

    void LateUpdate()
    {
        if (_transform == null) _transform = transform;

        float dirY = 0;
        float dirX = 0;

        // In Editor (or if TestDirection is set), override input
        if (TestDirection != DebugFacing.None)
        {
            switch (TestDirection) {
                case DebugFacing.Up: dirY = 1; break;
                case DebugFacing.Down: dirY = -1; break;
                case DebugFacing.Left: dirX = -1; break;
                case DebugFacing.Right: dirX = 1; break;
            }
        }
        else if (Application.isPlaying)
        {
            // Try to get input from Animator, but don't fail if missing
            if (_playerAnimator != null)
            {
                dirY = _playerAnimator.GetFloat("DirY");
                dirX = _playerAnimator.GetFloat("DirX");
            }
            // If Animator is null, we default to (0,0) which is Down
        }

        Vector3 targetPos, targetRot, targetScale;

        if (Mathf.Abs(dirX) > Mathf.Abs(dirY)) // Side Dominant
        {
            if (dirX > 0) // Right
            {
                targetPos = PosRight;
                targetRot = RotRight;
                targetScale = ScaleRight;
            }
            else // Left
            {
                targetPos = PosLeft;
                targetRot = RotLeft;
                targetScale = ScaleLeft;
            }
        }
        else // Vertical Dominant
        {
            if (dirY > 0.1f) // Up
            {
                targetPos = PosUp;
                targetRot = RotUp;
                targetScale = ScaleUp;
            }
            else // Down
            {
                targetPos = PosDown;
                targetRot = RotDown;
                targetScale = ScaleDown;
            }
        }

        // Apply transform
        _transform.localPosition = targetPos;
        _transform.localEulerAngles = targetRot;
        _transform.localScale = targetScale;
        
        // Apply sorting order
        if (_renderer != null)
        {
            if (Mathf.Abs(dirX) > Mathf.Abs(dirY)) // Side
            {
                _renderer.sortingOrder = SortingOrderSide;
            }
            else if (dirY > 0.1f) // Up (back)
            {
                _renderer.sortingOrder = SortingOrderBack;
            }
            else // Down (front)
            {
                _renderer.sortingOrder = SortingOrderFront;
            }
        }
    }
    
    string GetFullPath(Transform t) {
        string path = t.name;
        while (t.parent != null) {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    void OnValidate()
    {
        // Force update when values change in Inspector
        LateUpdate();
    }
}
