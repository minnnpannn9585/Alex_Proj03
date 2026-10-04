using UnityEngine;

[DefaultExecutionOrder(-20)]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.35f, -10f);
    [SerializeField] private Vector2 boundsMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 boundsMax = new Vector2(10f, 10f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        Snap();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 goal = Clamp(target.position + offset);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }

    private void Snap()
    {
        if (target == null)
        {
            return;
        }

        velocity = Vector3.zero;
        transform.position = Clamp(target.position + offset);
    }

    private Vector3 Clamp(Vector3 position)
    {
        position.z = offset.z;

        if (cam == null || !cam.orthographic)
        {
            return position;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float minX = boundsMin.x + halfWidth;
        float maxX = boundsMax.x - halfWidth;
        float minY = boundsMin.y + halfHeight;
        float maxY = boundsMax.y - halfHeight;

        position.x = minX > maxX ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(position.x, minX, maxX);
        position.y = minY > maxY ? (boundsMin.y + boundsMax.y) * 0.5f : Mathf.Clamp(position.y, minY, maxY);
        return position;
    }
}
