using UnityEngine;

public class FlashlightAim2D : MonoBehaviour
{
    public Animator animator;

    public string lastMoveXParam = "Horizontal";
    public string lastMoveYParam = "Vertical";

    public float angleOffset = -90f; // tersse 0 / 90 / 180 dene

    void Awake()
    {
        if (!animator)
            animator = GetComponentInParent<Animator>();
        ResolveParameterNames();
    }

    void LateUpdate()
    {
        if (!animator) return;

        float x = 0f;
        float y = 0f;

        if (!string.IsNullOrEmpty(_resolvedXParam))
            x = animator.GetFloat(_resolvedXParam);
        if (!string.IsNullOrEmpty(_resolvedYParam))
            y = animator.GetFloat(_resolvedYParam);

        Vector2 dir = new Vector2(x, y);

        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.down;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    private string _resolvedXParam;
    private string _resolvedYParam;

    private void ResolveParameterNames()
    {
        if (!animator) return;

        _resolvedXParam = ResolveOne(lastMoveXParam, "Horizontal", "LastMoveX", "MoveX");
        _resolvedYParam = ResolveOne(lastMoveYParam, "Vertical", "LastMoveY", "MoveY");
        if (string.IsNullOrEmpty(_resolvedXParam) || string.IsNullOrEmpty(_resolvedYParam))
        {
            Debug.LogWarning("FlashlightAim2D: Couldn't resolve animator parameters for last move. Using defaults (0, -1).");
        }
    }

    private string ResolveOne(params string[] names)
    {
        foreach (var n in names)
        {
            if (HasParameter(n)) return n;
        }
        return null;
    }

    private bool HasParameter(string name)
    {
        if (string.IsNullOrEmpty(name) || animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == name)
                return true;
        return false;
    }
}
