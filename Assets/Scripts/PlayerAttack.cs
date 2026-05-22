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
    [SerializeField] private PlayerController playerController;

    [Header("Enemy Search")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float maxSearchDistance = 300f;

    [Header("Attack")]
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float slowMotionScale = 0.1f;
    [SerializeField] private float releaseWindowRealTime = 0.2f;
    [SerializeField] private float relaunchSpeed = 80f;

    private readonly HashSet<Transform> checkedEnemies = new HashSet<Transform>();

    private Rigidbody rigid;
    private Coroutine attackRoutine;
    private bool isAttacking;
    private bool slowMotionActive;
    private bool previousIsKinematic;
    private bool previousUseGravity;
    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;

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
        if (isAttacking)
            return;

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        Transform target = FindClosestEnemyInCameraView();
        if (target == null)
            return;

        attackRoutine = StartCoroutine(AttackSequence(target));
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

        playerController?.SetActionLock(true, true);
        ActivateSlowMotion();

        elapsed = 0f;

        while (elapsed < releaseWindowRealTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

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

            Vector3 viewPosition = cam.WorldToViewportPoint(candidate.bounds.center);
            if (viewPosition.z <= 0f)
                continue;

            if (viewPosition.x < 0f || viewPosition.x > 1f || viewPosition.y < 0f || viewPosition.y > 1f)
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
}
