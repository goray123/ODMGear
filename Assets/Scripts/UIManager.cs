using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GearManager gearManager;

    [Header("FOV Effect")]
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float ropeFov = 75f;
    [SerializeField] private float fovLerpSpeed = 8f;

    [Header("Edge Blur UI")]
    [SerializeField] private CanvasGroup edgeBlurGroup;
    [SerializeField] private float maxBlurAlpha = 0.75f;
    [SerializeField] private float blurLerpSpeed = 8f;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
            normalFov = targetCamera.fieldOfView;

        if (edgeBlurGroup != null)
            edgeBlurGroup.alpha = 0f;
    }

    private void Update()
    {
        bool shouldShowEffect = ShouldShowRopeMovementEffect();

        UpdateCameraFov(shouldShowEffect);
        UpdateEdgeBlur(shouldShowEffect);
    }

    private bool ShouldShowRopeMovementEffect()
    {
        if (playerController == null || gearManager == null)
            return false;

        if (!gearManager.IsAnchorAttached)
            return false;

        bool isPullingToAnchor = !playerController.IsRopeLengthLockHeld;
        bool isMovingWithAnchor = playerController.HasMoveInput;

        return isPullingToAnchor || isMovingWithAnchor;
    }

    private void UpdateCameraFov(bool active)
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

    private void UpdateEdgeBlur(bool active)
    {
        if (edgeBlurGroup == null)
            return;

        float targetAlpha = active ? maxBlurAlpha : 0f;

        edgeBlurGroup.alpha = Mathf.Lerp(
            edgeBlurGroup.alpha,
            targetAlpha,
            Time.deltaTime * blurLerpSpeed
        );
    }
}