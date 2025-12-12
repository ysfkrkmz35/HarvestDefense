using UnityEngine;

public class FlashlightAim2D : MonoBehaviour
{
    public Animator animator;

    public string lastMoveXParam = "LastMoveX";
    public string lastMoveYParam = "LastMoveY";

    public float angleOffset = -90f; // tersse 0 / 90 / 180 dene

    void Awake()
    {
        if (!animator)
            animator = GetComponentInParent<Animator>();
    }

    void LateUpdate()
    {
        if (!animator) return;

        float x = animator.GetFloat(lastMoveXParam);
        float y = animator.GetFloat(lastMoveYParam);

        Vector2 dir = new Vector2(x, y);

        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.down;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }
}
