using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(InputReader))]
public class PlayerController : MonoBehaviour
{
    [Header("Bileşen Referansları")]
    [SerializeField] private InputReader input;
    [SerializeField] private Transform cameraHolder;

    [Header("İvme ve Hız Sınırları")]
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float maxGroundSpeed = 5f;
    [SerializeField] private float maxAirSpeed = 6f;
    [SerializeField] private float groundDrag = 1f;
    [SerializeField] private float staticDrag = 2f;
    [SerializeField] private float airDrag = 0f;
    [SerializeField] private float dragToAirSpeed = 10f;
    [SerializeField] private float dragToGroundSpeed = 10f;
    [SerializeField] private float dragToStaticSpeed = 10f;

    [Header("Merdiven / Basamak (Automatic Step Height)")]
    [SerializeField] private float stepHeight = 0.35f; // Çıkılabilecek maks basamak yüksekliği
    [SerializeField] private float stepSmooth = 15f;   // Basamağa tırmanma yumuşaklığı
    [SerializeField] private float stepCheckDistance = 0.4f; // Önü ne kadar uzaklıkta tarayacağı

    [Header("Hava Kontrol Ayarları")]
    [Range(0f, 3f)]
    [SerializeField] private float airControl = 0.5f;

    [Header("Zıplama Ayarları")]
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Kamera Ayarları")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float maxUpAngle = 80f;
    [SerializeField] private float maxDownAngle = -80f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;
    private float cameraPitch = 0f;
    private float currentDrag;
    private float nextJumpTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;
        currentDrag = groundDrag;
        rb.linearDamping = currentDrag;

        if (input == null) input = GetComponent<InputReader>();
    }

    private void Update()
    {
        // 1. Zemin Kontrolü
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        // 2. Kamera Bakışı
        HandleCamera();

        // 3. Zıplama
        if (input.Jump && isGrounded && Time.time >= nextJumpTime)
        {
            ApplyJump();
        }
    }

    private void FixedUpdate()
    {
        HandleDrag();
        HandleMovement();
        HandleStepClimb();
    }

    private void HandleDrag()
    {
        bool isMoving = input.Move.sqrMagnitude > 0.01f;

        float targetDrag = isGrounded
            ? (isMoving ? groundDrag : staticDrag)
            : airDrag;

        float lerpSpeed = !isGrounded
            ? dragToAirSpeed
            : (isMoving ? dragToGroundSpeed : dragToStaticSpeed);

        currentDrag = Mathf.Lerp(currentDrag, targetDrag, lerpSpeed * Time.fixedDeltaTime);
        rb.linearDamping = currentDrag;
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = (transform.right * input.Move.x + transform.forward * input.Move.y).normalized;

        if (moveDirection == Vector3.zero) return;

        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentSpeedInDir = Vector3.Dot(currentHorizontalVel, moveDirection);

        if (isGrounded)
        {
            if (currentSpeedInDir < maxGroundSpeed)
            {
                rb.AddForce(moveDirection * acceleration, ForceMode.Acceleration);
            }
        }
        else
        {
            if (currentSpeedInDir < maxAirSpeed)
            {
                rb.AddForce(moveDirection * acceleration * airControl, ForceMode.Acceleration);
            }
        }
    }

    private void HandleStepClimb()
    {
        if (!isGrounded) return;

        Vector3 moveDirection = (transform.right * input.Move.x + transform.forward * input.Move.y).normalized;
        if (moveDirection == Vector3.zero) return;

        // Capsule Collider tabanının Y pozisyonu
        float colliderBottomY = transform.position.y + capsuleCollider.center.y - (capsuleCollider.height / 2f);

        Vector3 rayLowerPos = new Vector3(transform.position.x, colliderBottomY + 0.05f, transform.position.z);
        Vector3 rayUpperPos = new Vector3(transform.position.x, colliderBottomY + stepHeight, transform.position.z);

        // DEBUG RAYCASTS
        Debug.DrawRay(rayLowerPos, moveDirection * stepCheckDistance, Color.red);
        Debug.DrawRay(rayUpperPos, moveDirection * (stepCheckDistance + 0.1f), Color.green);

        // 1. Alt Ray basamağa değdi mi?
        if (Physics.Raycast(rayLowerPos, moveDirection, out RaycastHit hitLower, stepCheckDistance))
        {
            // Basamağın eğim/yüzey açısını kontrol et (Duvarı basamak sanmasın)
            if (Vector3.Angle(hitLower.normal, Vector3.up) > 80f)
            {
                // 2. Üst Ray boş mu?
                if (!Physics.Raycast(rayUpperPos, moveDirection, stepCheckDistance + 0.1f))
                {
                    // Yer çekimi çelişkisini çözmek için dikey düşüş hızını sıfırla
                    if (rb.linearVelocity.y < 0)
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    }

                    // Titretmeden pürüzsüzce yukarı tırmandır
                    rb.AddForce(Vector3.up * stepSmooth, ForceMode.Acceleration);
                }
            }
        }
    }

    private void ApplyJump()
    {
        nextJumpTime = Time.time + jumpCooldown;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        input.Jump = false;
    }

    private void HandleCamera()
    {
        if (cameraHolder == null) return;

        float mouseX = input.Look.x * mouseSensitivity;
        float mouseY = input.Look.y * mouseSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, maxDownAngle, maxUpAngle);
        cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }
}