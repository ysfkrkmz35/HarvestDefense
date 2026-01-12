using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossController bossController;
    [SerializeField] private bool oneTimeUse = true;

    private bool triggered = false;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered && oneTimeUse) return;

        if (other.CompareTag("Player") && bossController != null)
        {
            bossController.WakeUp();
            triggered = true;
            
            // Optional: Lock camera, Play music, etc.
            Debug.Log("[BossTrigger] PLayer entered boss zone.");
            
            if (oneTimeUse)
            {
                // Disable trigger
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }

    private void OnValidate()
    {
        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
        }
    }
}
