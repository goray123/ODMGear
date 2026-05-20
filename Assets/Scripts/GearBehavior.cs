using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class GearBehaviour : MonoBehaviour
{
    [Header("Gear Settings")]
    [SerializeField] private float speed = 40f;
    [SerializeField] private float pullForce = 60f;
    [SerializeField] private LayerMask attachMask = ~0;

    private Rigidbody rb;
    private LineRenderer lineRenderer;
    private PlayerController owner;
    private bool anchored;
    private Vector3 anchorPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.03f;
        lineRenderer.endWidth = 0.03f;
        lineRenderer.numCapVertices = 2;

        if (lineRenderer.material == null)
        {
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
                lineRenderer.material = new Material(lineShader) { color = Color.white };
        }
    }

    public void Initialize(PlayerController owner, Vector3 direction, float gearSpeed, LayerMask attachLayerMask)
    {
        this.owner = owner;
        speed = gearSpeed;
        attachMask = attachLayerMask;
        anchored = false;

        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * speed;
    }

    private void FixedUpdate()
    {
        if (anchored && owner != null)
        {
            ApplyPlayerPull();
        }

        UpdateLine();
    }

    private void ApplyPlayerPull()
    {
        if (owner == null)
            return;

        Vector3 toAnchor = anchorPoint - owner.transform.position;
        if (toAnchor.sqrMagnitude < 0.01f)
            return;

        Vector3 forceDirection = toAnchor.normalized;
        owner.Rigidbody.AddForce(forceDirection * pullForce, ForceMode.Acceleration);
    }

    private void UpdateLine()
    {
        if (lineRenderer == null || owner == null)
            return;

        lineRenderer.SetPosition(0, owner.transform.position);
        lineRenderer.SetPosition(1, transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (anchored)
            return;

        if (collision.collider.isTrigger)
            return;

        if (owner != null && collision.gameObject == owner.gameObject)
            return;

        if ((attachMask & (1 << collision.gameObject.layer)) == 0)
            return;

        ContactPoint contact = collision.GetContact(0);
        anchorPoint = contact.point;
        anchored = true;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = anchorPoint;
        transform.rotation = Quaternion.LookRotation(-contact.normal, Vector3.up);
    }
}
