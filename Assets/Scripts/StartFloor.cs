using UnityEngine;

public class StartFloor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject floorObject; // 임시 바닥 오브젝트

    [Header("Settings")]
    [SerializeField] private Vector3 floorOffset = Vector3.zero; // 플레이어 기준 오프셋

    private bool gameStarted;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Start()
    {
        if (floorObject == null)
        {
            Debug.LogWarning("[StartFloor] Floor Object가 연결되지 않았습니다.");
            return;
        }

        // 바닥을 플레이어 발 아래에 배치
        floorObject.transform.position = playerController.transform.position + floorOffset;
        floorObject.SetActive(true);

        // 앵커 최초 발사 이벤트 구독
        playerController.OnFirstGearFired += HandleFirstGearFired;
    }

    private void HandleFirstGearFired()
    {
        if (gameStarted)
            return;

        gameStarted = true;

        // 바닥 제거
        if (floorObject != null)
            floorObject.SetActive(false);

        // GameManager 게임 시작
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();

        playerController.OnFirstGearFired -= HandleFirstGearFired;
    }

    private void OnDestroy()
    {
        if (playerController != null)
            playerController.OnFirstGearFired -= HandleFirstGearFired;
    }
}