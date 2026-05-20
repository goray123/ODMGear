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
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float jumpForce = 7f;

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

    private Vector2 moveInput;
    private Vector2 lookInput;

    public Rigidbody Rigidbody => rigid;

    private float pitch;
    private bool isGrounded;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        rigid.freezeRotation = true;

        if (gearManager == null)
            gearManager = FindObjectOfType<GearManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        RotateCamera();
        CheckGround();
    }

    private void FixedUpdate()
    {
        Move();
    }

    // =========================
    // Input
    // =========================

    public void OnMove(InputValue value)
    {   
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump()
    {
        if (!isGrounded)
            return;

        rigid.linearVelocity = new Vector3(rigid.linearVelocity.x, 0f, rigid.linearVelocity.z);

        rigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void OnClick(InputValue value)
    {
        if (!value.Get<bool>())
            return;

        if (gearManager == null)
            return;

        Vector3 fireDirection = cameraPivot.forward;
        Vector3 fireOrigin = transform.position + Vector3.up * 1.2f + fireDirection * 0.6f;

        gearManager.FireGear(fireOrigin, fireDirection, this);
    }

    // =========================
    // Movement
    // =========================

    private void Move()
    {
        Vector3 moveDirection =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        Vector3 velocity = moveDirection * moveSpeed;

        rigid.linearVelocity = new Vector3(
            velocity.x,
            rigid.linearVelocity.y,
            velocity.z
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
