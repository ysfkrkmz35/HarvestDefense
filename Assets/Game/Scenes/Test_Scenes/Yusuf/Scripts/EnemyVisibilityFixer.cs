using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Enemy prefab'larda Sorting Layer ve Z pozisyon sorunlarını otomatik düzeltir
    /// Enemy prefab'larınıza bu componenti ekleyin
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyVisibilityFixer : MonoBehaviour
    {
        [Header("Sorting Settings")]
        [Tooltip("Düşmanın hangi sorting layer'da görüneceği")]
        [SerializeField] private string sortingLayerName = "Default";

        [Tooltip("Sorting order (yüksek değer = daha önde)")]
        [SerializeField] private int sortingOrder = 5;

        [Header("Position Settings")]
        [Tooltip("Z pozisyonunu 0'a sabitle (2D oyunlar için)")]
        [SerializeField] private bool fixZPosition = true;

        private SpriteRenderer spriteRenderer;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            FixVisibility();
        }

        void OnEnable()
        {
            FixVisibility();
        }

        void FixVisibility()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Sorting Layer ayarla
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;

            // Z pozisyonunu düzelt
            if (fixZPosition)
            {
                Vector3 pos = transform.position;
                if (Mathf.Abs(pos.z) > 0.01f)
                {
                    transform.position = new Vector3(pos.x, pos.y, 0f);
                }
            }
        }

        void Update()
        {
            // Her frame Z pozisyonunu kontrol et (emin olmak için)
            if (fixZPosition)
            {
                Vector3 pos = transform.position;
                if (Mathf.Abs(pos.z) > 0.01f)
                {
                    transform.position = new Vector3(pos.x, pos.y, 0f);
                }
            }
        }
    }
}
