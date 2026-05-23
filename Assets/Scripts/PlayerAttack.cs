using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CameraEffects cameraEffects;
    [SerializeField] private PlayerController playerController;

    [Header("Enemy Search")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float maxSearchDistance = 300f;
    [SerializeField] private float maximumTargetDistance = 30f;

    [Header("Attack")]
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float slowMotionScale = 0.1f;
    [SerializeField] private float releaseWindowRealTime = 0.2f;
    [SerializeField] private float relaunchSpeed = 80f;

    [Header("Target UI")]
    [SerializeField] private RectTransform targetUI;
    [SerializeField] private Vector3 targetUiOffset = Vector3.up * 2f;

    [Header("Kill Effects")]
    [SerializeField] private GameObject slashEffectPrefab;

    [SerializeField] private GameObject burstEffectPrefab;

    [SerializeField] private GameObject sparksEffectPrefab;

    [SerializeField] private Vector3 effectOffset = Vector3.up * 1.5f;

    private readonly HashSet<Transform> checkedEnemies = new HashSet<Transform>();

    private Rigidbody rigid;
    private Coroutine attackRoutine;
    private bool isAttacking;
    private bool slowMotionActive;
    private bool previousIsKinematic;
    private bool previousUseGravity;
    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;
    private Vector3 pendingEffectPosition;
    private bool hasPendingRelaunchEffect;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        Transform currentTarget = FindClosestEnemyInCameraView();

        UpdateTargetUI(currentTarget);

        if (isAttacking)
            return;

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (currentTarget == null)
            return;

        attackRoutine = StartCoroutine(AttackSequence(currentTarget));
    }

    private void UpdateTargetUI(Transform target)
    {
        if (targetUI == null)
            return;

        Camera cam = ResolveCamera();

        if (target == null || cam == null)
        {
            targetUI.gameObject.SetActive(false);
            return;
        }

        Vector3 worldPosition = target.position + targetUiOffset;

        Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);

        // 카메라 뒤
        if (screenPosition.z <= 0f)
        {
            targetUI.gameObject.SetActive(false);
            return;
        }

        targetUI.gameObject.SetActive(true);
        targetUI.position = screenPosition;
    }

    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;
        previousIsKinematic = rigid.isKinematic;
        previousUseGravity = rigid.useGravity;

        playerController?.SetActionLock(true, false);

        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        rigid.useGravity = false;
        rigid.isKinematic = true;

        Vector3 startPosition = rigid.position;
        Vector3 targetPosition = target.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, dashDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rigid.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        rigid.position = targetPosition;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        KillEnemy(target);

        playerController?.SetActionLock(true, true);
        ActivateSlowMotion();
        cameraEffects?.EnterSlowMotion(releaseWindowRealTime);

        elapsed = 0f;

        bool previousEPressed = Keyboard.current != null && Keyboard.current.eKey.isPressed;

        while (elapsed < releaseWindowRealTime)
        {
            elapsed += Time.unscaledDeltaTime;

            bool currentEPressed = Keyboard.current != null && Keyboard.current.eKey.isPressed;

            // E를 떼는 순간 공격 종료
            if (previousEPressed && !currentEPressed)
            {
                FinishAttack(GetCameraRelaunchVelocity());
                yield break;
            }

            previousEPressed = currentEPressed;

            yield return null;
        }

        // 제한 시간 초과 시 자동 종료
        FinishAttack(GetCameraRelaunchVelocity());
    }

    private Transform FindClosestEnemyInCameraView()
    {
        Camera cam = ResolveCamera();
        if (cam == null)
            return null;

        checkedEnemies.Clear();

        Collider[] candidates = Physics.OverlapSphere(
            cam.transform.position,
            maxSearchDistance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        Transform closest = null;
        float closestForwardDistance = float.MaxValue;

        foreach (Collider candidate in candidates)
        {
            Transform enemy = GetEnemyRoot(candidate.transform);

            if (enemy == null || enemy == transform || enemy.IsChildOf(transform))
                continue;

            if (!checkedEnemies.Add(enemy))
                continue;

            // 너무 멀면 타겟 제외
            float distance = Vector3.Distance(
                transform.position,
                enemy.position
            );

            if (distance >= maximumTargetDistance)
                continue;

            Vector3 viewPosition = cam.WorldToViewportPoint(candidate.bounds.center);

            if (viewPosition.z <= 0f)
                continue;

            if (viewPosition.x < 0f || viewPosition.x > 1f ||
                viewPosition.y < 0f || viewPosition.y > 1f)
                continue;

            if (viewPosition.z >= closestForwardDistance)
                continue;

            closestForwardDistance = viewPosition.z;
            closest = enemy;
        }

        return closest;
    }

    private Transform GetEnemyRoot(Transform candidate)
    {
        Transform current = candidate;
        Transform matched = null;

        while (current != null)
        {
            if (IsEnemyObject(current.gameObject))
                matched = current;

            current = current.parent;
        }

        return matched;
    }

    private bool IsEnemyObject(GameObject candidate)
    {
        if (!string.IsNullOrEmpty(enemyTag) && candidate.tag == enemyTag)
            return true;

        return (enemyLayers.value & (1 << candidate.layer)) != 0;
    }

    private Camera ResolveCamera()
    {
        if (playerCamera != null)
            return playerCamera;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        return playerCamera;
    }

    private void ActivateSlowMotion()
    {
        previousTimeScale = Mathf.Max(Time.timeScale, 0.0001f);
        previousFixedDeltaTime = Time.fixedDeltaTime;
        slowMotionActive = true;

        float scale = Mathf.Max(0.01f, slowMotionScale);
        Time.timeScale = scale;
        Time.fixedDeltaTime = previousFixedDeltaTime * scale / previousTimeScale;
    }

    private void DeactivateSlowMotion()
    {
        if (!slowMotionActive)
            return;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = previousFixedDeltaTime / previousTimeScale;
        slowMotionActive = false;
    }

    private void FinishAttack(Vector3 relaunchVelocity)
    {
        DeactivateSlowMotion();
        cameraEffects?.ExitSlowMotion();
        cameraEffects?.PlayRelaunchEffect();
        PlayRelaunchEffects();

        rigid.isKinematic = previousIsKinematic;
        rigid.useGravity = previousUseGravity;

        if (!previousIsKinematic)
        {
            rigid.linearVelocity = relaunchVelocity.sqrMagnitude > 0.001f
                ? relaunchVelocity
                : Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }

        playerController?.SetActionLock(false, false);
        isAttacking = false;
        attackRoutine = null;
    }

    private Vector3 GetCameraRelaunchVelocity()
    {
        Vector3 direction = ResolveCamera() != null
            ? playerCamera.transform.forward
            : transform.forward;

        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        return direction.normalized * relaunchSpeed;
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        if (!isAttacking)
            return;

        FinishAttack(Vector3.zero);
    }

    private void KillEnemy(Transform enemy)
    {
        if (enemy == null)
            return;

        Vector3 effectPosition = enemy.position + effectOffset;

        SpawnEffect(burstEffectPrefab, effectPosition, Quaternion.identity);

        pendingEffectPosition = effectPosition;
        hasPendingRelaunchEffect = true;

        // 기존: enemy.gameObject.SetActive(false);
        // 변경: GameManager에 킬 처리 위임 (카운트 증가 + 리스폰)
        if (GameManager.Instance != null)
            GameManager.Instance.OnEnemyKilled(enemy.gameObject);
        else
            enemy.gameObject.SetActive(false); // GameManager 없을 때 폴백
    }

    private void PlayRelaunchEffects()
    {
        if (!hasPendingRelaunchEffect)
            return;

        Quaternion relaunchRotation =
            Quaternion.LookRotation(
                GetCameraRelaunchVelocity().normalized
            );

        // Slash
        SpawnEffect(
            slashEffectPrefab,
            pendingEffectPosition,
            relaunchRotation
        );

        // Sparks
        SpawnEffect(
            sparksEffectPrefab,
            pendingEffectPosition,
            relaunchRotation
        );

        hasPendingRelaunchEffect = false;
    }

    private void SpawnEffect(
    GameObject prefab,
    Vector3 position,
    Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.Log("Prefab is NULL");
            return;
        }

        Debug.Log("Spawn Effect : " + prefab.name);

        GameObject effect =
            Instantiate(prefab, position, rotation);

        Debug.Log("Effect Spawned");

        ParticleSystem[] particleSystems =
            effect.GetComponentsInChildren<ParticleSystem>();

        float longestLifetime = 0f;

        foreach (ParticleSystem ps in particleSystems)
        {
            var main = ps.main;

            float lifetime =
                main.duration +
                main.startLifetime.constantMax;

            if (lifetime > longestLifetime)
                longestLifetime = lifetime;
        }

        Destroy(effect, longestLifetime + 0.5f);
    }
}
