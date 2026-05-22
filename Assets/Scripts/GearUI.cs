using UnityEngine;
using UnityEngine.UI;

public class GearUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GearManager gearManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Image leftAnchorImage;
    [SerializeField] private Image rightAnchorImage;
    [SerializeField] private Image modeImage;

    [Header("Colors")]
    [SerializeField] private Color unlockedColor = Color.red;
    [SerializeField] private Color lockedColor = Color.blue;
    [SerializeField] private Color anchoredColor = Color.white;

    private void Update()
    {
        bool leftAnchored = gearManager != null && gearManager.IsLeftAnchorAttached;
        bool rightAnchored = gearManager != null && gearManager.IsRightAnchorAttached;
        int anchoredCount = GetAnchoredCount(leftAnchored, rightAnchored);
        bool isLockHeld = playerController != null && playerController.IsRopeLengthLockHeld;
        Color currentModeColor = isLockHeld ? lockedColor : unlockedColor;

        SetImageVisible(leftAnchorImage, leftAnchored);
        SetImageVisible(rightAnchorImage, rightAnchored);

        if (anchoredCount == 1)
        {
            SetImageVisible(modeImage, true);

            if (modeImage != null)
                modeImage.color = currentModeColor;

            if (leftAnchored && leftAnchorImage != null)
                leftAnchorImage.color = currentModeColor;

            if (rightAnchored && rightAnchorImage != null)
                rightAnchorImage.color = currentModeColor;

            return;
        }

        if (leftAnchorImage != null)
            leftAnchorImage.color = anchoredColor;

        if (rightAnchorImage != null)
            rightAnchorImage.color = anchoredColor;

        SetImageVisible(modeImage, false);
    }

    private int GetAnchoredCount(bool leftAnchored, bool rightAnchored)
    {
        int count = 0;

        if (leftAnchored)
            count++;

        if (rightAnchored)
            count++;

        return count;
    }

    private void SetImageVisible(Image image, bool isVisible)
    {
        if (image == null)
            return;

        image.gameObject.SetActive(isVisible);
    }
}
