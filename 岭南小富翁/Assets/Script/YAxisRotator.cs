using UnityEngine;

/// <summary>
/// 
/// </summary>
public class YAxisRotator : MonoBehaviour
{
    [Header("/")]
    [Tooltip("X")]
    public float xRotationSpeed = 0f;

    [Tooltip("Y")]
    public float yRotationSpeed = 30f;

    [Tooltip("Z")]
    public float zRotationSpeed = 0f;

    [Header("")]
    [Tooltip("")]
    public bool useWorldSpace = false;

    void Update()
    {
        Vector3 rotation = new Vector3(
            xRotationSpeed * Time.deltaTime,
            yRotationSpeed * Time.deltaTime,
            zRotationSpeed * Time.deltaTime
        );

        if (useWorldSpace)
        {
            transform.Rotate(rotation, Space.World);
        }
        else
        {
            transform.Rotate(rotation, Space.Self);
        }
    }
}