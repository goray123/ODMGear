using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyNavigator : MonoBehaviour
{
    [System.Serializable]
    private class NavigatorMarker
    {
        public RectTransform onScreenRect;   // 머리 위 마커
        public RectTransform offScreenRect;  // 가장자리 화살표
        public Image onScreenImage;
        public Image offScreenImage;
    }

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Canvas canvas;

    [Header("Marker Prefabs")]
    [SerializeField] private GameObject onScreenMarkerPrefab;   // 머리 위 마커 프리팹
    [SerializeField] private GameObject offScreenArrowPrefab;   // 화살표 프리팹

    [Header("On-Screen Marker")]
    [SerializeField] private Vector3 markerWorldOffset = Vector3.up * 2f;

    [Header("Off-Screen Arrow")]
    [SerializeField] private float edgeMargin = 50f;

    [Header("Distance Color")]
    [SerializeField] private float nearDistance = 10f;
    [SerializeField] private float farDistance = 60f;
    [SerializeField] private Color nearColor = Color.red;
    [SerializeField] private Color farColor = Color.white;

    [Header("Enemy Detection")]
    [SerializeField] private string enemyTag = "Enemy";

    private readonly List<GameObject> trackedEnemies = new List<GameObject>();
    private readonly List<NavigatorMarker> markerPool = new List<NavigatorMarker>();

    private RectTransform canvasRect;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        RefreshEnemyList();
    }

    private void Update()
    {
        // 비활성 적 제거, 새로 활성화된 적 추가
        RefreshEnemyList();

        EnsurePoolSize(trackedEnemies.Count);

        for (int i = 0; i < markerPool.Count; i++)
        {
            bool active = i < trackedEnemies.Count;
            SetMarkerVisible(markerPool[i], false, false);

            if (!active)
                continue;

            GameObject enemy = trackedEnemies[i];
            UpdateMarker(markerPool[i], enemy);
        }
    }

    // ─────────────────────────────────────
    // Enemy 추적
    // ─────────────────────────────────────

    private void RefreshEnemyList()
    {
        // 비활성화된 적 제거
        trackedEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);

        // 새로 활성화된 적 추가
        GameObject[] all = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (GameObject e in all)
        {
            if (!trackedEnemies.Contains(e))
                trackedEnemies.Add(e);
        }
    }

    // ─────────────────────────────────────
    // 마커 업데이트
    // ─────────────────────────────────────

    private void UpdateMarker(NavigatorMarker marker, GameObject enemy)
    {
        Vector3 worldPos = enemy.transform.position + markerWorldOffset;
        Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);

        bool isBehind = screenPos.z < 0f;

        if (isBehind)
            screenPos *= -1f;

        Vector2 canvasSize = canvasRect.sizeDelta;
        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;

        // 화면 중앙 기준 좌표로 변환
        Vector2 centeredScreen = new Vector2(screenPos.x, screenPos.y);

        // 스크린 좌표 → 캔버스 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            centeredScreen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : playerCamera,
            out Vector2 localPos
        );

        bool isOnScreen =
            !isBehind &&
            localPos.x >= -halfW + edgeMargin &&
            localPos.x <= halfW - edgeMargin &&
            localPos.y >= -halfH + edgeMargin &&
            localPos.y <= halfH - edgeMargin;

        float distance = Vector3.Distance(
            playerCamera.transform.position,
            enemy.transform.position
        );

        Color color = GetDistanceColor(distance);

        if (isOnScreen)
        {
            marker.onScreenRect.localPosition = localPos;

            if (marker.onScreenImage != null)
                marker.onScreenImage.color = color;

            SetMarkerVisible(marker, true, false);
        }
        else
        {
            Vector2 clampedPos = ClampToEdge(localPos, halfW, halfH);
            marker.offScreenRect.localPosition = clampedPos;

            float angle = Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg;
            marker.offScreenRect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            if (marker.offScreenImage != null)
                marker.offScreenImage.color = color;

            SetMarkerVisible(marker, false, true);
        }
    }

    private Vector2 ClampToEdge(Vector2 dir, float halfW, float halfH)
    {
        float margin = edgeMargin;
        float clampW = halfW - margin;
        float clampH = halfH - margin;

        if (dir == Vector2.zero)
            return new Vector2(0f, clampH);

        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);

        if (absX * clampH > absY * clampW)
        {
            // 좌우 가장자리
            float sign = Mathf.Sign(dir.x);
            float ratio = clampW / absX;
            return new Vector2(sign * clampW, dir.y * ratio);
        }
        else
        {
            // 상하 가장자리
            float sign = Mathf.Sign(dir.y);
            float ratio = clampH / absY;
            return new Vector2(dir.x * ratio, sign * clampH);
        }
    }

    private Color GetDistanceColor(float distance)
    {
        float t = Mathf.InverseLerp(nearDistance, farDistance, distance);
        return Color.Lerp(nearColor, farColor, t);
    }

    private void SetMarkerVisible(NavigatorMarker marker, bool onScreen, bool offScreen)
    {
        marker.onScreenRect.gameObject.SetActive(onScreen);
        marker.offScreenRect.gameObject.SetActive(offScreen);
    }

    // ─────────────────────────────────────
    // 오브젝트 풀
    // ─────────────────────────────────────

    private void EnsurePoolSize(int requiredCount)
    {
        while (markerPool.Count < requiredCount)
        {
            GameObject onScreenGO = Instantiate(onScreenMarkerPrefab, canvas.transform);
            GameObject offScreenGO = Instantiate(offScreenArrowPrefab, canvas.transform);

            markerPool.Add(new NavigatorMarker
            {
                onScreenRect  = onScreenGO.GetComponent<RectTransform>(),
                offScreenRect = offScreenGO.GetComponent<RectTransform>(),
                onScreenImage  = onScreenGO.GetComponent<Image>(),
                offScreenImage = offScreenGO.GetComponent<Image>(),
            });
        }
    }
}