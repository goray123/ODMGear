using UnityEngine;

public class StartFloor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject floorObject;

    [Header("Settings")]
    [SerializeField] private Vector3 floorOffset = Vector3.zero;

    private bool gameStarted;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Start()
    {
        PlaceFloor();
        playerController.OnPlayerRespawned += HandlePlayerRespawned;
    }

    public void PlaceFloor()
    {
        if (floorObject == null)
        {
            Debug.LogWarning("[StartFloor] Floor Object가 연결되지 않았습니다.");
            return;
        }

        floorObject.transform.position = playerController.transform.position + floorOffset;
        floorObject.SetActive(true);

        playerController.OnFirstGearFired += HandleFirstGearFired;
    }

    private void HandleFirstGearFired()
    {
        if (floorObject != null)
            floorObject.SetActive(false);

        playerController.OnFirstGearFired -= HandleFirstGearFired;

        if (gameStarted)
            return;

        gameStarted = true;

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    private void HandlePlayerRespawned()
    {
        // 재시작 시 gameStarted 초기화
        gameStarted = false;
        PlaceFloor();
    }

    private void OnDestroy()
    {
        if (playerController == null)
            return;

        playerController.OnFirstGearFired -= HandleFirstGearFired;
        playerController.OnPlayerRespawned -= HandlePlayerRespawned;
    }
}