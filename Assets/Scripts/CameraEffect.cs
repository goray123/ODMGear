using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private PlayerController playerController;

    [Header("FOV")]
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float slowMotionFov = 42f;
    [SerializeField] private float relaunchFov = 82f;
    [SerializeField] private float fovSmoothSpeed = 10f;

    [Header("Slow Motion Visual")]
    [SerializeField] private float slowMotionSaturation = -80f;
    [SerializeField] private float slowMotionVignette = 0.3f;

    [Header("Mouse")]
    [SerializeField] private float slowMotionSensitivityMultiplier = 0.5f;

    [Header("Slow Motion UI")]
    [SerializeField] private Slider slowMotionSlider;

    private float slowMotionDuration;
    private float slowMotionRemaining;
    private bool slowMotionUiActive;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private float targetFov;

    private float defaultSensitivity;
    private bool initializedSensitivity;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        targetFov = normalFov;

        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out colorAdjustments);
            globalVolume.profile.TryGet(out vignette);
        }

        if (playerController != null)
        {
            defaultSensitivity = playerController.MouseSensitivity;
            initializedSensitivity = true;
        }
    }

    private void Update()
    {
        if (targetCamera == null)
            return;

        targetCamera.fieldOfView = Mathf.Lerp(
            targetCamera.fieldOfView,
            targetFov,
            Time.unscaledDeltaTime * fovSmoothSpeed
        );

        if (slowMotionUiActive)
        {
            slowMotionRemaining -= Time.unscaledDeltaTime;

            float normalized =
                slowMotionDuration > 0.001f
                    ? slowMotionRemaining / slowMotionDuration
                    : 0f;

            if (slowMotionSlider != null)
                slowMotionSlider.value = normalized;

            if (slowMotionRemaining <= 0f)
                slowMotionUiActive = false;
        }
    }

    public void EnterSlowMotion(float duration)
    {
        targetFov = slowMotionFov;

        if (colorAdjustments != null)
            colorAdjustments.saturation.value = slowMotionSaturation;

        if (vignette != null)
            vignette.intensity.value = slowMotionVignette;

        if (playerController != null && initializedSensitivity)
        {
            playerController.MouseSensitivity =
                defaultSensitivity * slowMotionSensitivityMultiplier;
        }

        // UI 시작
        slowMotionDuration = duration;
        slowMotionRemaining = duration;
        slowMotionUiActive = true;

        if (slowMotionSlider != null)
        {
            slowMotionSlider.gameObject.SetActive(true);
            slowMotionSlider.value = 1f;
        }
    }

    public void ExitSlowMotion()
    {
        targetFov = normalFov;

        if (colorAdjustments != null)
            colorAdjustments.saturation.value = 0f;

        if (vignette != null)
            vignette.intensity.value = 0f;

        if (playerController != null && initializedSensitivity)
        {
            playerController.MouseSensitivity = defaultSensitivity;
        }

        slowMotionUiActive = false;

        if (slowMotionSlider != null)
        {
            slowMotionSlider.value = 0f;
            slowMotionSlider.gameObject.SetActive(false);
        }
    }

    public void PlayRelaunchEffect()
    {
        if (targetCamera == null)
            return;

        // 순간적으로 속도감 강조
        targetCamera.fieldOfView = relaunchFov;
    }
}