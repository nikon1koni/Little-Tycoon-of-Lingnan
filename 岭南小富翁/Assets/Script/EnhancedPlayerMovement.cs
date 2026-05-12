// EnhancedPlayerMovement.cs
using System.Collections;
using UnityEngine;

public class EnhancedPlayerMovement : MonoBehaviour
{
    [Header("��Ծ����")]
    public float jumpPower = 2f;
    public float jumpSpeed = 5f;
    public AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("��תЧ��")]
    public float rotationSpeed = 360f;
    public bool rotateDuringJump = true;

    [Header("����Ч��")]
    public float squashAmount = 0.2f;
    public float stretchAmount = 0.3f;

    [Header("��βЧ��")]
    public bool enableTrail = true;
    public float trailTime = 0.2f;

    [Header("Ӱ��Ч��")]
    public GameObject shadowPrefab;
    private GameObject shadow;

    [Header("��Ч")]
    public AudioClip jumpSound;
    public AudioClip landSound;

    // ״̬
    private bool isJumping = false;
    private Vector3 originalScale;
    private TrailRenderer trail;

    void Start()
    {
        originalScale = transform.localScale;

        // ��ʼ����βЧ��
        if (enableTrail)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.widthMultiplier = 0.1f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = GetComponent<Renderer>().material.color;
            trail.endColor = new Color(1, 1, 1, 0);
        }

        // ����Ӱ��
        if (shadowPrefab != null)
        {
            shadow = Instantiate(shadowPrefab, transform.position, Quaternion.identity);
            shadow.transform.parent = transform;
            shadow.transform.localPosition = Vector3.down * 0.5f;
        }
    }

    // ���ƶ�����
    public void JumpToNextTile(BoardTile targetTile, int stepCount = 1)
    {
        if (isJumping) return;
        StartCoroutine(JumpSequence(targetTile, stepCount));
    }

    IEnumerator JumpSequence(BoardTile targetTile, int steps)
    {
        isJumping = true;

        for (int i = 0; i < steps; i++)
        {
            // ��ȡ��ǰ��Ŀ��λ��
            Vector3 startPos = transform.position;
            BoardTile nextTile = GetNextTile(targetTile, i);
            Vector3 endPos = nextTile.transform.position;
            endPos.y = startPos.y;

            // ������Ծ
            yield return StartCoroutine(SingleJump(startPos, endPos));

            // ���Ч��
            yield return StartCoroutine(LandingEffect());

            // ����ͣ��
            yield return new WaitForSeconds(0.1f);
        }

        isJumping = false;

        // ���������¼�
        if (steps > 0)
        {
            targetTile.OnLanded(GetComponent<Player>());
        }
    }

    IEnumerator SingleJump(Vector3 startPos, Vector3 endPos)
    {
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / jumpSpeed;
        float elapsed = 0f;

        Vector3 controlPoint = (startPos + endPos) / 2;
        controlPoint.y += jumpPower;

        // ������Ծ��Ч
        if (jumpSound != null)
            AudioSource.PlayClipAtPoint(jumpSound, transform.position, 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = jumpCurve.Evaluate(t);

            // ���������߼���λ��
            Vector3 position = CalculateBezierPoint(startPos, controlPoint, endPos, curveT);
            transform.position = position;

            // ��תЧ��
            if (rotateDuringJump)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            // ����Ч�������죩
            ApplyJumpSquashAndStretch(t);

            // ����Ӱ��λ��
            if (shadow != null)
            {
                shadow.transform.position = new Vector3(position.x, 0, position.z);
                shadow.transform.localScale = Vector3.one * (1 - t * 0.5f);
            }

            yield return null;
        }

        // ȷ������λ��׼ȷ
        transform.position = endPos;
    }

    Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;

        return p;
    }

    void ApplyJumpSquashAndStretch(float t)
    {
        Vector3 newScale = originalScale;

        if (t < 0.5f) // �����׶Σ�����
        {
            float stretchT = t * 2;
            newScale.y = originalScale.y * (1 + stretchAmount * stretchT);
            newScale.x = originalScale.x * (1 - squashAmount * stretchT * 0.5f);
            newScale.z = originalScale.z * (1 - squashAmount * stretchT * 0.5f);
        }
        else // �½��׶Σ�ѹ��
        {
            float squashT = (t - 0.5f) * 2;
            newScale.y = originalScale.y * (1 - squashAmount * squashT);
            newScale.x = originalScale.x * (1 + stretchAmount * squashT * 0.5f);
            newScale.z = originalScale.z * (1 + stretchAmount * squashT * 0.5f);
        }

        transform.localScale = newScale;
    }

    IEnumerator LandingEffect()
    {
        // ���������Ч
        if (landSound != null)
            AudioSource.PlayClipAtPoint(landSound, transform.position, 0.5f);

        // �����Ч��
        Vector3 originalPos = transform.position;
        float shakeAmount = 0.1f;
        float shakeDuration = 0.1f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;
            float shake = Mathf.Sin(t * Mathf.PI * 10) * shakeAmount * (1 - t);
            transform.position = originalPos + Vector3.up * shake;
            yield return null;
        }

        transform.position = originalPos;
        transform.localScale = originalScale;

        // ����Ӱ��
        if (shadow != null)
        {
            shadow.transform.localScale = Vector3.one;
        }
    }

    BoardTile GetNextTile(BoardTile targetTile, int stepOffset)
    {
        // ���ݵ�ǰ���λ�ü�����һ������
        // ������Ҫ������BoardManager�߼�
        if (BoardManager.Instance == null) return targetTile;

        Player player = GetComponent<Player>();
        if (player != null && player.currentTile != null)
        {
            int currentIndex = BoardManager.Instance.allTiles.IndexOf(player.currentTile);
            int nextIndex = (currentIndex + stepOffset + 1) % BoardManager.Instance.allTiles.Count;
            return BoardManager.Instance.allTiles[nextIndex];
        }

        return targetTile;
    }

    // ����Ϊ����״̬
    void ResetToNormal()
    {
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
    }
}