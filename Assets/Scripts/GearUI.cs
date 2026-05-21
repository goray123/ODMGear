using UnityEngine;
using UnityEngine.UI;

public class GearUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GearManager gearManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Image gearImage;

    [Header("Colors")]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color ropeLockedColor;

    private void Update()
    {
        bool isAnchorAttached = gearManager != null && gearManager.IsAnchorAttached;
        SetVisible(isAnchorAttached);


        gearImage.color = playerController != null && playerController.IsRopeLengthLockHeld
            ? ropeLockedColor
            : defaultColor;
    }

    private void SetVisible(bool isVisible)
    {
        gearImage.gameObject.SetActive(isVisible);
    }
}
