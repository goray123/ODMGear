using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [FormerlySerializedAs("cameraRoot")]
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float maxRunSpeed = 20f;
    [SerializeField] private float groundedMaxSpeedMultiplier = 0.25f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float fastFallAcceleration = 40f;

    [Header("Camera")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.2f;

    private Rigidbody rigid;
    private Collider playerCollider;

    [Header("Gear")]
    [SerializeField] private GearManager gearManager;
    [SerializeField] private float fireOriginHeight = 4.5f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpHeld;
    private bool leftGearHeld;
    private bool rightGearHeld;
    private bool actionLocked;
    private bool lookLocked;

    public Rigidbody Rigidbody => rigid;
    public bool IsRopeLengthLockHeld => jumpHeld;
    public bool CanUseRopeLengthLock => gearManager != null && gearManager.AnchoredGearCount == 1;
    public Vector3 FireOrigin => transform.position + Vector3.up * fireOriginHeight;
    public Vector3 FireDirection => cameraPivot.forward;

    private float pitch;
    private bool isGrounded;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        rigid.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!lookLocked)
            RotateCamera();

        CheckGround();

        if (actionLocked)
        {
            jumpHeld = false;
            return;
        }

        UpdateGearInputState();
        jumpHeld = Keyboard.current.spaceKey.isPressed;
    }

    private void FixedUpdate()
    {
        if (actionLocked)
            return;

        Move();
        ApplyFastFall();
        ClampSpeedToMaxRunSpeed();
    }

    // =========================
    // Input
    // =========================

    public void OnMove(InputValue value)
    {
        if (actionLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (lookLocked)
        {
            lookInput = Vector2.zero;
            return;
        }

        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (actionLocked)
            return;

        if (!jumpHeld)
            return;

        if (!isGrounded)
            return;

        rigid.linearVelocity = new Vector3(rigid.linearVelocity.x, 0f, rigid.linearVelocity.z);

        rigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void OnAttack(InputValue value)
    {
        if (actionLocked)
            return;

        if (Mouse.current != null)
            return;

        UpdateGearSlotInput(GearSlot.Left, value.isPressed, ref leftGearHeld);
    }

    private void UpdateGearInputState()
    {
        if (actionLocked)
            return;

        if (Mouse.current == null)
            return;

        UpdateGearSlotInput(GearSlot.Left, Mouse.current.leftButton.isPressed, ref leftGearHeld);
        UpdateGearSlotInput(GearSlot.Right, Mouse.current.rightButton.isPressed, ref rightGearHeld);
    }

    private void UpdateGearSlotInput(GearSlot slot, bool isPressed, ref bool wasPressed)
    {
        if (isPressed == wasPressed)
            return;

        wasPressed = isPressed;

        if (isPressed)
            FireGear(slot);
        else
            ReleaseGear(slot);
    }

    private void FireGear(GearSlot slot)
    {
        if (gearManager == null)
            return;

        gearManager.FireGear(slot, FireOrigin, FireDirection, this);
    }

    private void ReleaseGear(GearSlot slot)
    {
        if (gearManager != null)
            gearManager.ReleaseGear(slot);
    }

    public void SetActionLock(bool locked, bool allowLook)
    {
        actionLocked = locked;
        lookLocked = locked && !allowLook;

        if (!locked)
            return;

        moveInput = Vector2.zero;
        jumpHeld = false;

        if (gearManager != null)
            gearManager.ReleaseGear();

        leftGearHeld = false;
        rightGearHeld = false;
    }

    // =========================
    // Movement
    // =========================

    private void Move()
    {
        if (!CanApplyDirectionalMovement())
            return;

        if (moveInput.sqrMagnitude <= 0.01f)
            return;

        Vector3 moveDirection =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        if (moveDirection.sqrMagnitude <= 0.01f)
            return;

        moveDirection.Normalize();
        rigid.AddForce(moveDirection * speed, ForceMode.Impulse);
        ClampSpeedToMaxRunSpeed();
    }

    private bool CanApplyDirectionalMovement()
    {
        bool isGroundMovement = isGrounded;
        bool isAnchorMovement = gearManager != null && gearManager.IsAnchorAttached && jumpHeld;
        bool isAirMovementWithoutAnchor = !isGrounded && (gearManager == null || !gearManager.IsAnchorAttached);

        return isGroundMovement || isAnchorMovement || isAirMovementWithoutAnchor;
    }

    private void ApplyFastFall()
    {
        if (isGrounded)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.leftCtrlKey.isPressed && !Keyboard.current.rightCtrlKey.isPressed)
            return;

        rigid.AddForce(Vector3.down * fastFallAcceleration, ForceMode.Impulse);
    }

    public void ClampSpeedToMaxRunSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rigid.linearVelocity.x, 0f, rigid.linearVelocity.z);
        float currentMaxRunSpeed = isGrounded ? maxRunSpeed * groundedMaxSpeedMultiplier : maxRunSpeed;

        if (horizontalVelocity.magnitude <= currentMaxRunSpeed)
            return;

        Vector3 limitedHorizontalVelocity = horizontalVelocity.normalized * currentMaxRunSpeed;
        rigid.linearVelocity = new Vector3(
            limitedHorizontalVelocity.x,
            rigid.linearVelocity.y,
            limitedHorizontalVelocity.z
        );
    }

    // =========================
    // Camera
    // =========================

    private void RotateCamera()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // =========================
    // Ground Check
    // =========================

    private void CheckGround()
    {
        if (playerCollider == null)
        {
            isGrounded = Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                groundCheckDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );

            return;
        }

        Bounds bounds = playerCollider.bounds;

        isGrounded = Physics.Raycast(
            bounds.center,
            Vector3.down,
            bounds.extents.y + groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }
}
