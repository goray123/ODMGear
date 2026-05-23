using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GearManager gearManager;
    [SerializeField] private Material edgeBlurMaterial;

    [Header("FOV")]
    [SerializeField] private float ropeFov = 75f;
    [SerializeField] public float fovLerpSpeed = 8f;

    [Header("Blur")]
    [SerializeField] private float activeBlurStrength = 1f;
    [SerializeField] private float blurLerpSpeed = 8f;
    [SerializeField] private float effectSpeedThreshold = 18f;

    private float normalFov;
    private float currentBlurStrength;

    private static readonly int BlurStrengthID =
        Shader.PropertyToID("_BlurStrength");

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
            normalFov = targetCamera.fieldOfView;

        if (edgeBlurMaterial != null)
            edgeBlurMaterial.SetFloat(BlurStrengthID, 0f);
    }

    private void Update()
    {
        bool active =
            gearManager != null &&
            playerController.Rigidbody.linearVelocity.magnitude >= effectSpeedThreshold;

        UpdateFov(active);
        UpdateBlur(active);
    }

    private void UpdateFov(bool active)
    {
        if (targetCamera == null)
            return;

        float targetFov = active ? ropeFov : normalFov;

        targetCamera.fieldOfView = Mathf.Lerp(
            targetCamera.fieldOfView,
            targetFov,
            Time.deltaTime * fovLerpSpeed
        );
    }

    private void UpdateBlur(bool active)
    {
        if (edgeBlurMaterial == null)
            return;

        float targetBlur = active ? activeBlurStrength : 0f;

        currentBlurStrength = Mathf.Lerp(
            currentBlurStrength,
            targetBlur,
            Time.deltaTime * blurLerpSpeed
        );

        edgeBlurMaterial.SetFloat(BlurStrengthID, currentBlurStrength);
    }
}