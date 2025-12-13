using UnityEngine;

public class LightAimFromAnimator : MonoBehaviour
{
    public Animator animator; // Visual üstündeki Animator
    public string moveX = "Horizontal";   // projende farklı olabilir
    public string moveY = "Vertical";   // projende farklı olabilir
    public float angleOffset = -90f; // tersse 0/90/180 dene

    void Awake()
    {
        if (!animator) animator = GetComponentInParent<Animator>();
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

        if (dir.sqrMagnitude < 0.001f) return; // idle’da son yönü korumak istersen ayrı parametre kullanılır

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angle + angleOffset);
    }

    private string _resolvedXParam;
    private string _resolvedYParam;

    private void ResolveParameterNames()
    {
        if (!animator) return;
        _resolvedXParam = ResolveOne(moveX, "Horizontal", "MoveX", "LastMoveX");
        _resolvedYParam = ResolveOne(moveY, "Vertical", "MoveY", "LastMoveY");
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
