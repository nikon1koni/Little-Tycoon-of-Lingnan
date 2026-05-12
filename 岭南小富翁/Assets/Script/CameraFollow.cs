using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;  // Ҫ���������
    public Vector3 offset = new Vector3(0, 2, -5);  // ����������Ŀ���ƫ��
    public float smoothSpeed = 0.125f;  // ƽ��������ٶ�

    [Header("������Ƕȿ���")]
    [Range(-90, 90)]
    public float pitch = 20f;  // ������ (X����ת)
    [Range(-180, 180)]
    public float yaw = 0f;    // ƫ���� (Y����ת)
    [Range(-180, 180)]
    public float roll = 0f;    // ��ת�� (Z����ת)

    [Header("�������")]
    public float distance = 5f;  // ���������
    public float minDistance = 1f;
    public float maxDistance = 20f;

    [Header("�ǶȲ�ֵ")]
    public bool smoothRotation = true;
    public float rotationSmoothSpeed = 5f;

    // ���ڴ洢��ǰ��ת
    private Quaternion currentRotation;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: û������Ŀ�꣬��������ʱ����Player��ǩ������");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // ��ʼ����ǰ��ת
        currentRotation = Quaternion.Euler(pitch, yaw, roll);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // ���ݽǶȴ�����ת
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);

        // ƽ����ת������Ӧ��
        if (smoothRotation)
        {
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation,
                rotationSmoothSpeed * Time.deltaTime);
        }
        else
        {
            currentRotation = targetRotation;
        }

        // ����ƫ�Ʒ���
        Vector3 rotatedOffset = currentRotation * new Vector3(0, 0, -distance);

        // ����Ŀ��λ��
        Vector3 desiredPosition = target.position + rotatedOffset;

        // ʹ�ò�ֵƽ���ƶ������
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // �������ʼ�տ���Ŀ��
        transform.LookAt(target);
    }

    // �ڱ༭����ʵʱԤ��
    private void OnValidate()
    {
        // ���ƾ����ں����Χ��
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // �ڱ༭��ģʽ��Ԥ���Ƕȱ仯
        if (Application.isPlaying && target != null)
        {
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);
            Vector3 rotatedOffset = targetRotation * new Vector3(0, 0, -distance);
            transform.position = target.position + rotatedOffset;
            transform.LookAt(target);
        }
    }
}