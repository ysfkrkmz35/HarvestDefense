using UnityEngine;


[ExecuteAlways]
public class SwordVisualAligner : MonoBehaviour
{
    public enum DebugFacing { None, Up, Down, Left, Right }
    [Header("Debug")]
    public DebugFacing TestDirection = DebugFacing.None;

    [Header("Right (DirX > 0)")]
    public Vector3 RightPos = new Vector3(0, 0, -0.5f);
    public Vector3 RightRot = new Vector3(0, 0, -90);
    public Vector3 RightScale = new Vector3(1.29f, 1.09f, 1f);

    [Header("Left (DirX < 0)")]
    public Vector3 LeftPos = new Vector3(0, 0, -0.5f);
    public Vector3 LeftRot = new Vector3(0, 180, -90);
    public Vector3 LeftScale = new Vector3(1.29f, 1.09f, 1f);

    [Header("Up (DirY > 0)")]
    public Vector3 UpPos = new Vector3(0, 0, 0.5f);
    public Vector3 UpRot = new Vector3(0, 0, -45);
    public Vector3 UpScale = new Vector3(1.29f, 1.09f, 1f);

    [Header("Down (DirY < 0)")]
    public Vector3 DownPos = new Vector3(0, 0, -0.5f);
    public Vector3 DownRot = new Vector3(0, 0, -135);
    public Vector3 DownScale = new Vector3(1.29f, 1.09f, 1f);

    private Transform _transform;
    private Animator _playerAnimator;

    void Start()
    {
        _transform = transform;
        if (Application.isPlaying) {
             _playerAnimator = GetComponentInParent<Animator>();
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
        else if (Application.isPlaying && _playerAnimator != null)
        {
             dirY = _playerAnimator.GetFloat("DirY");
             dirX = _playerAnimator.GetFloat("DirX");
        }
        else
        {
             // Editor logic without ForceDirection? Do nothing or default?
             // If we do nothing, it allows manual transform manipulation without fighting script.
             return;
        }

        Vector3 targetPos, targetRot, targetScale;

        if (Mathf.Abs(dirX) > Mathf.Abs(dirY)) // Side Dominant
        {
            if (dirX > 0) // Right
            {
                targetPos = RightPos;
                targetRot = RightRot;
                targetScale = RightScale;
            }
            else // Left
            {
                targetPos = LeftPos;
                targetRot = LeftRot;
                targetScale = LeftScale;
            }
        }
        else // Vertical Dominant
        {
            if (dirY > 0.1f) // Up
            {
                targetPos = UpPos;
                targetRot = UpRot;
                targetScale = UpScale;
            }
            else // Down
            {
                targetPos = DownPos;
                targetRot = DownRot;
                targetScale = DownScale;
            }
        }

        // Apply
        _transform.localPosition = targetPos;
        _transform.localEulerAngles = targetRot;
        
        // Handle Parent Flip for Scale
        // If parent is flipped negative, and we want positive result, we might need to flip our local scale?
        // Actually, if user gives us the DESIRED world appearance in config, we should respect it relative to parent.
        // If parent.x is -1, and we set local.x = 1, world is -1.
        // If we want world 1, we set local -1.
        // Let's assume the user fields define "Local Transform assuming Parent is (1,1,1)".
        // So if parent is (-1,1,1), we should flip X.
        
        if (_transform.parent != null && _transform.parent.lossyScale.x < 0)
        {
             targetScale.x = -targetScale.x;
        }
        
        _transform.localScale = targetScale;
    }
}
