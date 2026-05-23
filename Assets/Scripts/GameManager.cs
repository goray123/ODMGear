using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────
    // Enemy Respawn
    // ─────────────────────────────────────

    [Header("Enemy Respawn")]
    [SerializeField] private Vector3 spawnBoundsMin;
    [SerializeField] private Vector3 spawnBoundsMax;
    [SerializeField] private string wallTag = "Wall";
    [SerializeField] private float overlapCheckRadius = 0.5f;
    [SerializeField] private int maxRespawnAttempts = 30;

    // ─────────────────────────────────────
    // Kill Count
    // ─────────────────────────────────────

    private int killCount;
    public int KillCount => killCount;
    public event System.Action<int> OnKillCountChanged;

    [Header("Kill Count UI")]
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private string killCountFormat = "{0} Kills";

    // ─────────────────────────────────────
    // Timer
    // ─────────────────────────────────────

    [Header("Timer")]
    [SerializeField] private float gameDuration = 120f;
    [SerializeField] private Slider timerSlider;

    private float timeRemaining;
    private bool isGameRunning;

    public float TimeRemaining => timeRemaining;
    public bool IsGameRunning => isGameRunning;

    public event System.Action<int> OnGameOver;

    // ─────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private void Update()
    {
        if (!isGameRunning)
            return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerUI();
            EndGame();
            return;
        }

        UpdateTimerUI();
    }

    // ─────────────────────────────────────
    // Game Flow
    // ─────────────────────────────────────

    public void StartGame()
    {
        killCount = 0;
        timeRemaining = gameDuration;
        isGameRunning = true;

        // Slider 초기화: 최대값을 gameDuration으로 설정
        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = gameDuration;
            timerSlider.value = gameDuration;
        }

        UpdateKillCountUI();
    }

    private void EndGame()
    {
        isGameRunning = false;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
            enemy.SetActive(false);

        OnGameOver?.Invoke(killCount);

        Debug.Log($"[GameManager] 게임 종료 — 최종 킬 수: {killCount}");
    }

    // ─────────────────────────────────────
    // UI
    // ─────────────────────────────────────

    private void UpdateTimerUI()
    {
        if (timerSlider != null)
            timerSlider.value = timeRemaining;
    }

    private void UpdateKillCountUI()
    {
        if (killCountText != null)
            killCountText.text = string.Format(killCountFormat, killCount);
    }

    // ─────────────────────────────────────
    // Enemy Kill & Respawn
    // ─────────────────────────────────────

    public void OnEnemyKilled(GameObject enemy)
    {
        if (!isGameRunning)
        {
            enemy.SetActive(false);
            return;
        }

        killCount++;
        OnKillCountChanged?.Invoke(killCount);
        UpdateKillCountUI();

        RespawnEnemy(enemy);
    }

    private void RespawnEnemy(GameObject enemy)
    {
        Vector3 respawnPosition = GetValidRespawnPosition();
        enemy.transform.position = respawnPosition;
        enemy.SetActive(true);
    }

    private Vector3 GetValidRespawnPosition()
    {
        for (int i = 0; i < maxRespawnAttempts; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(spawnBoundsMin.x, spawnBoundsMax.x),
                Random.Range(spawnBoundsMin.y, spawnBoundsMax.y),
                Random.Range(spawnBoundsMin.z, spawnBoundsMax.z)
            );

            if (!IsBlockedByWall(candidate))
                return candidate;
        }

        Debug.LogWarning("[GameManager] 유효한 스폰 위치를 찾지 못했습니다. spawnBoundsMin 위치로 fallback합니다.");
        return spawnBoundsMin;
    }

    private bool IsBlockedByWall(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, overlapCheckRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(wallTag))
                return true;
        }

        return false;
    }

    // ─────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        Vector3 center = (spawnBoundsMin + spawnBoundsMax) * 0.5f;
        Vector3 size = spawnBoundsMax - spawnBoundsMin;
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(center, size);
    }
}