using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    // Player Death
    // ─────────────────────────────────────

    [Header("Player Death")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float deathYThreshold = -10f;
    [SerializeField] private float deathTimePenalty = 10f;
    [SerializeField] private Vector3 respawnPosition = new Vector3(0f, 2f, 0f);

    private bool isDead;

    // ─────────────────────────────────────
    // Game End UI
    // ─────────────────────────────────────

    [Header("Game End UI")]
    [SerializeField] private GameObject gameEndObject;
    [SerializeField] private TextMeshProUGUI finalKillText;
    [SerializeField] private TextMeshProUGUI myBestText;
    [SerializeField] private string finalKillFormat = "{0} Kills";
    [SerializeField] private string myBestFormat = "My Best : {0}";

    private const string BestScoreKey = "BestKillCount";

    [Header("Start Button")]
    [SerializeField] private GameObject startButtonObject; // 시작 버튼 UI 루트

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

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        // 씬 로딩 시 적 위치 랜덤 배치
        ScatterEnemiesOnLoad();
    }

    public void OnStartButtonClicked()
    {
        if (startButtonObject != null)
            startButtonObject.SetActive(false);

        // 마우스 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 플레이어 액션 잠금 해제
        if (playerController != null)
            playerController.SetActionLock(false, false);
    }


    private void ScatterEnemiesOnLoad()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Vector3 spawnPos = GetValidRespawnPosition();
            enemy.transform.position = spawnPos;
        }
    }

    private void Start()
    {
        if (gameEndObject != null)
            gameEndObject.SetActive(false);
    }

    private void Update()
    {
        // 언제든 L키로 재시작
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame)
        {
            RestartGame();
            return;
        }

        if (!isGameRunning)
            return;

        CheckPlayerFall();

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
        isDead = false;

        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = gameDuration;
            timerSlider.value = gameDuration;
        }

        if (gameEndObject != null)
            gameEndObject.SetActive(false);

        UpdateKillCountUI();
    }

    private void EndGame()
    {
        isGameRunning = false;

        // 모든 적 비활성화
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
            enemy.SetActive(false);

        // 최고 기록 갱신
        int prevBest = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (killCount > prevBest)
        {
            PlayerPrefs.SetInt(BestScoreKey, killCount);
            PlayerPrefs.Save();
        }

        ShowGameEndUI();
        OnGameOver?.Invoke(killCount);

        Debug.Log($"[GameManager] 게임 종료 — 최종 킬 수: {killCount}");
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ─────────────────────────────────────
    // Game End UI
    // ─────────────────────────────────────

    private void ShowGameEndUI()
    {
        if (gameEndObject != null)
            gameEndObject.SetActive(true);

        if (finalKillText != null)
            finalKillText.text = string.Format(finalKillFormat, killCount);

        int best = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (myBestText != null)
            myBestText.text = string.Format(myBestFormat, best);
    }

    // ─────────────────────────────────────
    // Player Death & Respawn
    // ─────────────────────────────────────

    private void CheckPlayerFall()
    {
        if (isDead)
            return;

        if (playerController == null)
            return;

        if (playerController.transform.position.y > deathYThreshold)
            return;

        HandlePlayerDeath();
    }

    private void HandlePlayerDeath()
    {
        isDead = true;

        timeRemaining = Mathf.Max(0f, timeRemaining - deathTimePenalty);
        UpdateTimerUI();

        playerController.Respawn(respawnPosition);

        isDead = false;
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
            // 게임 시작 전이라도 적은 랜덤 위치에 리스폰
            RespawnEnemy(enemy);
            return;
        }

        killCount++;
        OnKillCountChanged?.Invoke(killCount);
        UpdateKillCountUI();

        RespawnEnemy(enemy);
    }

    private void RespawnEnemy(GameObject enemy)
    {
        Vector3 spawnPos = GetValidRespawnPosition();
        enemy.transform.position = spawnPos;
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

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawLine(
            new Vector3(-1000f, deathYThreshold, 0f),
            new Vector3(1000f, deathYThreshold, 0f)
        );
    }
}