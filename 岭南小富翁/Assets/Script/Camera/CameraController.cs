using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.125f;

    [Header("Angle Control")]
    [Range(-90, 90)]
    public float pitch = 60f;
    [Range(-180, 180)]
    public float yaw = 45f;
    [Range(-180, 180)]
    public float roll = 0f;

    [Header("Distance")]
    public float distance = 15f;
    public float minDistance = 3f;
    public float maxDistance = 40f;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Movement Bounds")]
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    [Header("Follow Settings")]
    public float playerMoveThreshold = 0.01f;
    public float stopFollowDelay = 1f;

    private Quaternion currentRotation;
    private Vector3 lastPlayerPosition;
    private bool isFollowingPlayer = false;
    private float stopFollowTimer = 0f;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        currentRotation = Quaternion.Euler(pitch, yaw, roll);

        if (player != null)
        {
            Vector3 rotatedOffset = currentRotation * new Vector3(0, 0, -distance);
            transform.position = player.position + rotatedOffset;
            transform.LookAt(player);
            lastPlayerPosition = player.position;
        }
    }

    private void LateUpdate()
    {
        CheckPlayerMovement();

        if (isFollowingPlayer && player != null)
        {
            FollowPlayer();
        }
        else
        {
            HandleFreeMovement();
        }
    }

    private void CheckPlayerMovement()
    {
        if (player == null) return;

        float distanceMoved = Vector3.Distance(player.position, lastPlayerPosition);
        
        if (distanceMoved > playerMoveThreshold)
        {
            isFollowingPlayer = true;
            stopFollowTimer = 0f;
        }
        else if (isFollowingPlayer)
        {
            stopFollowTimer += Time.deltaTime;
            if (stopFollowTimer >= stopFollowDelay)
            {
                isFollowingPlayer = false;
                stopFollowTimer = 0f;
            }
        }

        lastPlayerPosition = player.position;
    }

    public void StopFollowing()
    {
        isFollowingPlayer = false;
    }

    private void FollowPlayer()
    {
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, 5f * Time.deltaTime);

        Vector3 rotatedOffset = currentRotation * new Vector3(0, 0, -distance);
        Vector3 desiredPosition = player.position + rotatedOffset;

        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.z = Mathf.Clamp(desiredPosition.z, minZ, maxZ);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }

    private void HandleFreeMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;
        
        if (movement.magnitude > 0.1f)
        {
            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            moveDirection.y = 0f;
            moveDirection.Normalize();

            Vector3 targetPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);

            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            new Vector3((minX + maxX) / 2, 0, (minZ + maxZ) / 2),
            new Vector3(maxX - minX, 0.1f, maxZ - minZ)
        );
    }

    private void OnValidate()
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        if (Application.isPlaying && player != null)
        {
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);
            Vector3 rotatedOffset = targetRotation * new Vector3(0, 0, -distance);
            transform.position = player.position + rotatedOffset;
            transform.LookAt(player);
        }
    }
}