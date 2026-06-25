using UnityEngine;

/// <summary>
/// 让物体沿着指定轴持续旋转
/// </summary>
public class YAxisRotator : MonoBehaviour
{
    [Header("旋转速度（度/秒）")]
    [Tooltip("X轴旋转速度")]
    public float xRotationSpeed = 0f;

    [Tooltip("Y轴旋转速度")]
    public float yRotationSpeed = 30f;

    [Tooltip("Z轴旋转速度")]
    public float zRotationSpeed = 0f;

    [Header("设置")]
    [Tooltip("使用世界坐标系还是局部坐标系")]
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