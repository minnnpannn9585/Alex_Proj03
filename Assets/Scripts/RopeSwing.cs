using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class RopeSwing : MonoBehaviour
{
    [Header("Grab")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("Length along local down. The object's position is the fixed end.")]
    [SerializeField] private float ropeLength = 5f;
    [SerializeField] private float grabRadius = 0.8f;

    [Header("Swing")]
    [Tooltip("Tangential push while holding left or right. Pump with the swing to go faster.")]
    [SerializeField] private float swingForce = 8f;

    [Header("Visual")]
    [SerializeField] private float ropeThickness = 0.1f;
    [SerializeField] private Color ropeColor = new Color(0.55f, 0.33f, 0.16f, 1f);
    [SerializeField] private Color readyColor = new Color(0.95f, 0.78f, 0.4f, 1f);
    [SerializeField] private SpriteRenderer ropeVisual;

    private CircleCollider2D grabZone;
    private DistanceJoint2D joint;
    private bool isUpdatingVisual;
    private PlayerMovement playerInRange;
    private PlayerMovement attachedPlayer;
    private Rigidbody2D attachedBody;
    private float swingInput;
    private float grabbedDistance;
    private bool configureJointNextPhysicsStep;

    private void Awake()
    {
        grabZone = GetComponent<CircleCollider2D>();
        ApplyGrabZone();
    }

    private void OnValidate()
    {
        ropeLength = Mathf.Max(0.5f, ropeLength);
        grabRadius = Mathf.Max(0.05f, grabRadius);
        swingForce = Mathf.Max(0f, swingForce);
        ropeThickness = Mathf.Max(0.01f, ropeThickness);
        ApplyGrabZone();
        UpdateVisual();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        swingInput = Input.GetAxisRaw("Horizontal");

        if (attachedPlayer != null)
        {
            if (Input.GetKeyDown(interactKey))
            {
                Detach(true);
            }

            return;
        }

        if (playerInRange != null && Input.GetKeyDown(interactKey))
        {
            TryGrab();
        }
    }

    private void FixedUpdate()
    {
        if (attachedBody == null || joint == null)
        {
            return;
        }

        joint.autoConfigureDistance = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = transform.position;
        if (configureJointNextPhysicsStep)
        {
            configureJointNextPhysicsStep = false;
            joint.distance = grabbedDistance;
        }

        float maxLength = WorldRopeLength + grabRadius;
        float distance = Vector2.Distance(attachedBody.position, transform.position);
        if (distance > maxLength + 2f)
        {
            Detach(true);
            return;
        }

        if (Mathf.Abs(swingInput) < 0.01f)
        {
            return;
        }

        Vector2 offset = attachedBody.position - (Vector2)transform.position;
        if (offset.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 tangent = new Vector2(-offset.y, offset.x);
        if (tangent.x < 0f)
        {
            tangent = -tangent;
        }

        attachedBody.AddForce(tangent.normalized * swingInput * swingForce, ForceMode2D.Force);
    }

    private void LateUpdate()
    {
        if (attachedBody != null && attachedPlayer == null)
        {
            attachedBody = null;
            DisableJoint();
        }

        UpdateVisual();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying || attachedPlayer == null)
        {
            return;
        }

        Detach(true);
    }

    private void TryGrab()
    {
        if (playerInRange == null || !playerInRange.TryBeginSwing())
        {
            return;
        }

        Rigidbody2D body = playerInRange.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            playerInRange.CompleteSwing(false);
            return;
        }

        float currentDistance = Vector2.Distance(body.position, transform.position);
        grabbedDistance = Mathf.Max(0.5f, currentDistance);

        DistanceJoint2D existingJoint = body.GetComponent<DistanceJoint2D>();
        if (existingJoint != null)
        {
            existingJoint.enabled = false;
            Destroy(existingJoint);
        }

        joint = body.gameObject.AddComponent<DistanceJoint2D>();
        joint.enabled = false;
        joint.autoConfigureDistance = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.enableCollision = false;
        joint.maxDistanceOnly = true;
        joint.connectedBody = null;
        joint.anchor = Vector2.zero;
        joint.connectedAnchor = transform.position;
        joint.distance = grabbedDistance;
        joint.breakForce = Mathf.Infinity;
        joint.enabled = true;

        attachedPlayer = playerInRange;
        attachedBody = body;
        attachedPlayer.SwingCancelled += OnSwingCancelled;
        configureJointNextPhysicsStep = true;
        body.WakeUp();
    }

    private void OnSwingCancelled()
    {
        Detach(true);
    }

    private void Detach(bool keepVelocity)
    {
        Vector2 velocity = attachedBody != null ? attachedBody.velocity : Vector2.zero;
        PlayerMovement player = attachedPlayer;
        Rigidbody2D body = attachedBody;

        if (player != null)
        {
            player.SwingCancelled -= OnSwingCancelled;
        }

        attachedPlayer = null;
        attachedBody = null;
        configureJointNextPhysicsStep = false;
        DisableJoint();

        if (player != null)
        {
            player.CompleteSwing(keepVelocity);
        }

        if (keepVelocity && body != null)
        {
            body.velocity = velocity;
        }
    }

    private void DisableJoint()
    {
        if (joint == null)
        {
            return;
        }

        joint.enabled = false;
        Destroy(joint);
        joint = null;
    }

    private void ApplyGrabZone()
    {
        if (grabZone == null)
        {
            grabZone = GetComponent<CircleCollider2D>();
        }

        if (grabZone == null)
        {
            return;
        }

        grabZone.isTrigger = true;
        grabZone.offset = new Vector2(0f, -ropeLength);
        grabZone.radius = grabRadius;
    }

    private float WorldRopeLength
    {
        get { return Vector2.Distance(transform.position, transform.TransformPoint(new Vector3(0f, -ropeLength, 0f))); }
    }

    private void UpdateVisual()
    {
        if (ropeVisual == null || isUpdatingVisual)
        {
            return;
        }

        isUpdatingVisual = true;

        Vector3 endWorld = transform.TransformPoint(new Vector3(0f, -ropeLength, 0f));
        if (Application.isPlaying && attachedPlayer != null)
        {
            endWorld = attachedPlayer.transform.position;
            endWorld.z = transform.position.z;
        }

        Vector3 localEnd = transform.InverseTransformPoint(endWorld);
        localEnd.z = 0f;
        float localLength = localEnd.magnitude;

        Transform visualTransform = ropeVisual.transform;
        visualTransform.localPosition = localEnd * 0.5f;
        visualTransform.localRotation = localLength > 0.001f
            ? Quaternion.FromToRotation(Vector3.right, localEnd)
            : Quaternion.identity;
        visualTransform.localScale = new Vector3(Mathf.Max(localLength, 0.001f), ropeThickness, 1f);

        bool canGrab = Application.isPlaying && attachedPlayer == null && playerInRange != null;
        ropeVisual.color = canGrab ? readyColor : ropeColor;
        isUpdatingVisual = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            playerInRange = player;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null && player == playerInRange)
        {
            playerInRange = null;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 start = transform.position;
        Vector3 end = transform.TransformPoint(new Vector3(0f, -ropeLength, 0f));
        Gizmos.color = new Color(0.55f, 0.33f, 0.16f, 1f);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.12f);
        Gizmos.color = new Color(0.95f, 0.78f, 0.4f, 1f);
        Gizmos.DrawWireSphere(end, grabRadius);
    }
}
