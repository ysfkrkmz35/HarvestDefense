using UnityEngine;

public class LightAimFromAnimator : MonoBehaviour
{
    public Animator animator; // Visual üstündeki Animator
    public string moveX = "MoveX";   // projende farklı olabilir
    public string moveY = "MoveY";   // projende farklı olabilir
    public float angleOffset = -90f; // tersse 0/90/180 dene

    void Awake()
    {
        if (!animator) animator = GetComponentInParent<Animator>();
    }

    void LateUpdate()
    {
        if (!animator) return;

        float x = animator.GetFloat(moveX);
        float y = animator.GetFloat(moveY);
        Vector2 dir = new Vector2(x, y);

        if (dir.sqrMagnitude < 0.001f) return; // idle’da son yönü korumak istersen ayrı parametre kullanılır

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angle + angleOffset);
    }
}
