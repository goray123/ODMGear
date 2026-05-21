using UnityEngine;
using UnityEngine.UI;

public class AimUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject aimChecker;

    [Header("Raycast")]
    [SerializeField] private LayerMask wallMask = ~0;
    [SerializeField] private float maxDistance = 300f;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        bool isWallHit = IsAimingAtWall();

        if (aimChecker != null && aimChecker.activeSelf != isWallHit)
            aimChecker.SetActive(isWallHit);
    }

    private bool IsAimingAtWall()
    {
        if (playerController == null)
            return false;

        return Physics.Raycast(
            playerController.FireOrigin,
            playerController.FireDirection,
            maxDistance,
            wallMask,
            QueryTriggerInteraction.Ignore
        );
    }
}
