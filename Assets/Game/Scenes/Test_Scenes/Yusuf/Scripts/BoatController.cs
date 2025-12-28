using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Bot kontrolü - Player botta iken hareket ve yönelme kontrolü
    /// Sağa/sola dönüş ve ileri/geri hareket desteği
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BoatController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [Tooltip("Botun ileri/geri hareket hızı")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Botun dönüş hızı (derece/saniye)")]
        [SerializeField] private float rotationSpeed = 120f;

        [Header("Rotation Constraint")]
        [Tooltip("Rotation kısıtlamasını aktif et - Bot sadece yatay düzlemde kalır")]
        [SerializeField] private bool lockRotation = true;

        [Header("Physics")]
        [Tooltip("Hareket damping - Yavaşça durma efekti")]
        [SerializeField] private float movementDamping = 0.95f;

        [Tooltip("Dönüş damping - Yavaşça durma efekti")]
        [SerializeField] private float rotationDamping = 0.9f;

        [Header("Water Physics")]
        [Tooltip("Su sürtünmesi - Botun su üzerindeki hareketi için")]
        [SerializeField] private float waterDrag = 1.5f;

        [Header("Visual")]
        [Tooltip("Bot visueli (sprite renderer)")]
        [SerializeField] private SpriteRenderer boatSprite;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        #endregion

        #region Private Fields

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private float rotationInput;
        private bool isPlayerOnBoard = false;

        private Vector2 currentVelocity;
        private float currentAngularVelocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            ConfigureRigidbody();
        }

        private void Start()
        {
            if (boatSprite == null)
            {
                boatSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void FixedUpdate()
        {
            if (!isPlayerOnBoard)
            {
                ApplyDamping();
                return;
            }

            HandleMovement();
            HandleRotation();
        }

        private void LateUpdate()
        {
            // Rotation constraint - Bot'un X ve Y rotasyonunu sıfırla
            if (lockRotation)
            {
                Vector3 euler = transform.eulerAngles;
                euler.x = 0f;
                euler.y = 0f;
                transform.eulerAngles = euler;
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Rigidbody2D ayarlarını yapılandır
        /// </summary>
        private void ConfigureRigidbody()
        {
            rb.gravityScale = 0f; // Su üzerinde, yerçekimi yok
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterDrag;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Rotation tamamen kilitli - Bot hiç dönmez
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        #endregion

        #region Movement & Rotation

        /// <summary>
        /// Bot hareketini kontrol et - X/Y eksenlerinde serbest hareket
        /// </summary>
        private void HandleMovement()
        {
            // X ve Y eksenlerinde doğrudan hareket (WASD)
            // W = yukarı, S = aşağı, A = sol, D = sağ
            if (moveInput.sqrMagnitude > 0.01f)
            {
                // Dünya koordinat sisteminde hareket (rotation'dan bağımsız)
                Vector2 targetVelocity = moveInput * moveSpeed;

                // Smooth acceleration
                currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * 5f);
                rb.linearVelocity = currentVelocity;

                if (showDebugLogs)
                {
                    Debug.Log($"[BoatController] Moving: X={moveInput.x:F2}, Y={moveInput.y:F2}, Velocity: {rb.linearVelocity.magnitude:F2}");
                }
            }
            else
            {
                ApplyDamping();
            }
        }

        /// <summary>
        /// Bot dönüşünü kontrol et - DEVRE DIŞI (Bot rotasyon yapmaz)
        /// </summary>
        private void HandleRotation()
        {
            // Rotation tamamen devre dışı - Bot orijinal rotation'ını korur
            // Angular velocity'yi sıfırla
            rb.angularVelocity = 0f;
            currentAngularVelocity = 0f;
        }

        /// <summary>
        /// Hareket damping uygula - Yavaşça dur
        /// </summary>
        private void ApplyDamping()
        {
            currentVelocity *= movementDamping;
            rb.linearVelocity = currentVelocity;

            if (rb.linearVelocity.magnitude < 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
                currentVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Dönüş damping uygula - Yavaşça dönmeyi bırak
        /// </summary>
        private void ApplyRotationDamping()
        {
            currentAngularVelocity *= rotationDamping;
            rb.angularVelocity = currentAngularVelocity;

            if (Mathf.Abs(rb.angularVelocity) < 0.1f)
            {
                rb.angularVelocity = 0f;
                currentAngularVelocity = 0f;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Hareket input'unu ayarla (BoatInteraction tarafından çağrılır)
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
        }

        /// <summary>
        /// Dönüş input'unu ayarla (BoatInteraction tarafından çağrılır)
        /// </summary>
        public void SetRotationInput(float input)
        {
            rotationInput = input;
        }

        /// <summary>
        /// Player bota bindi
        /// </summary>
        public void PlayerBoarded()
        {
            isPlayerOnBoard = true;
            Log("Player boarded the boat");
        }

        /// <summary>
        /// Player bottan indi
        /// </summary>
        public void PlayerDisembarked()
        {
            isPlayerOnBoard = false;
            moveInput = Vector2.zero;
            rotationInput = 0f;
            Log("Player left the boat");
        }

        /// <summary>
        /// Bot'un mevcut hızını al
        /// </summary>
        public float GetCurrentSpeed()
        {
            return rb.linearVelocity.magnitude;
        }

        /// <summary>
        /// Bot'un baktığı yönü al
        /// </summary>
        public Vector2 GetForwardDirection()
        {
            return transform.up;
        }

        #endregion

        #region Debug

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[BoatController] {message}");
            }
        }

        private void OnDrawGizmos()
        {
            if (!isPlayerOnBoard) return;

            // Botun baktığı yön
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.up * 2f);

            // Hareket yönü
            if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
            }
        }

        #endregion
    }
}
