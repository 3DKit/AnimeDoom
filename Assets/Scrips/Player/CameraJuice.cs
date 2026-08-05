using UnityEngine;

public class CameraJuice : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera cam;

    [Header("Dynamic FOV (Asimetrik)")]
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float maxFOV = 70f;
    [SerializeField] private float fovSpeedThreshold = 10f; 
    [SerializeField] private float fovInSpeed = 1f;         
    [SerializeField] private float fovOutSpeed = 1f;       

    [Header("Landing Impact (Yükseklik Tabanlı)")]
    [SerializeField] private float minFallHeightToImpact = 1.2f;
    [SerializeField] private float heightImpactMultiplier = 0.08f;
    [SerializeField] private float maxImpactDepth = 0.5f;
    [SerializeField] private float recoverySpeed = 10f;

    private Vector3 originalCameraPos;
    private Vector3 impactOffset;
    private bool wasGrounded;
    private float peakY; // Havadayken ulaşılan en yüksek Y noktası

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        originalCameraPos = transform.localPosition;
    }

    private void Update()
    {
        HandleDynamicFOV();
        HandleLandingImpact();
    }

    private void HandleDynamicFOV()
    {
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentSpeed = horizontalVel.magnitude;

        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, currentSpeed / fovSpeedThreshold);

        bool isAccelerating = targetFOV > cam.fieldOfView;
        float smoothSpeed = isAccelerating ? fovInSpeed : fovOutSpeed;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }

    private void HandleLandingImpact()
    {
        // Karakterin Y hızına göre basit zemin tespiti
        bool isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        // 1. Havaya İlk Sıçrama / Düşüş Anı
        if (!isGrounded && wasGrounded)
        {
            peakY = transform.position.y;
        }

        // 2. Havadayken Ulaşılan En Yüksek Noktayı Güncelle
        if (!isGrounded)
        {
            if (transform.position.y > peakY)
            {
                peakY = transform.position.y;
            }
        }

        // 3. Yere İniş Anı
        if (isGrounded && !wasGrounded)
        {
            float fallHeight = peakY - transform.position.y;

            // Sadece belirlenen minimum yükseklikten daha fazla düşüldüyse tepki ver
            if (fallHeight >= minFallHeightToImpact)
            {
                OnLand(fallHeight);
            }
        }

        wasGrounded = isGrounded;

        // Kamerayı orijinal konumuna geri getir
        impactOffset = Vector3.Lerp(impactOffset, Vector3.zero, Time.deltaTime * recoverySpeed);
        transform.localPosition = originalCameraPos + impactOffset;
    }

    private void OnLand(float fallHeight)
    {
        // Yükseklik farkı ile orantılı darbe hesabı
        float impactAmount = (fallHeight - minFallHeightToImpact) * heightImpactMultiplier;
        impactAmount = Mathf.Clamp(impactAmount, 0f, maxImpactDepth);

        impactOffset = new Vector3(0f, -impactAmount, 0f);
    }
}