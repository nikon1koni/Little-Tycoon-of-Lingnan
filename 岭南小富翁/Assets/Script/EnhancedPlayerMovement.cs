// EnhancedPlayerMovement.cs
using System.Collections;
using UnityEngine;

public class EnhancedPlayerMovement : MonoBehaviour
{
    [Header("跳跃设置")]
    public float jumpPower = 2f;
    public float jumpSpeed = 5f;
    public AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("旋转效果")]
    public float rotationSpeed = 360f;
    public bool rotateDuringJump = true;

    [Header("缩放效果")]
    public float squashAmount = 0.2f;
    public float stretchAmount = 0.3f;

    [Header("拖尾效果")]
    public bool enableTrail = true;
    public float trailTime = 0.2f;

    [Header("影子效果")]
    public GameObject shadowPrefab;
    private GameObject shadow;

    [Header("音效")]
    public AudioClip jumpSound;
    public AudioClip landSound;

    // 状态
    private bool isJumping = false;
    private Vector3 originalScale;
    private TrailRenderer trail;

    void Start()
    {
        originalScale = transform.localScale;

        // 初始化拖尾效果
        if (enableTrail)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.widthMultiplier = 0.1f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = GetComponent<Renderer>().material.color;
            trail.endColor = new Color(1, 1, 1, 0);
        }

        // 创建影子
        if (shadowPrefab != null)
        {
            shadow = Instantiate(shadowPrefab, transform.position, Quaternion.identity);
            shadow.transform.parent = transform;
            shadow.transform.localPosition = Vector3.down * 0.5f;
        }
    }

    // 主移动方法
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
            // 获取当前和目标位置
            Vector3 startPos = transform.position;
            BoardTile nextTile = GetNextTile(targetTile, i);
            Vector3 endPos = nextTile.transform.position;
            endPos.y = startPos.y;

            // 单次跳跃
            yield return StartCoroutine(SingleJump(startPos, endPos));

            // 落地效果
            yield return StartCoroutine(LandingEffect());

            // 短暂停顿
            yield return new WaitForSeconds(0.1f);
        }

        isJumping = false;

        // 触发格子事件
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

        // 播放跳跃音效
        if (jumpSound != null)
            AudioSource.PlayClipAtPoint(jumpSound, transform.position, 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = jumpCurve.Evaluate(t);

            // 贝塞尔曲线计算位置
            Vector3 position = CalculateBezierPoint(startPos, controlPoint, endPos, curveT);
            transform.position = position;

            // 旋转效果
            if (rotateDuringJump)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            // 缩放效果（拉伸）
            ApplyJumpSquashAndStretch(t);

            // 更新影子位置
            if (shadow != null)
            {
                shadow.transform.position = new Vector3(position.x, 0, position.z);
                shadow.transform.localScale = Vector3.one * (1 - t * 0.5f);
            }

            yield return null;
        }

        // 确保最终位置准确
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

        if (t < 0.5f) // 上升阶段：拉伸
        {
            float stretchT = t * 2;
            newScale.y = originalScale.y * (1 + stretchAmount * stretchT);
            newScale.x = originalScale.x * (1 - squashAmount * stretchT * 0.5f);
            newScale.z = originalScale.z * (1 - squashAmount * stretchT * 0.5f);
        }
        else // 下降阶段：压扁
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
        // 播放落地音效
        if (landSound != null)
            AudioSource.PlayClipAtPoint(landSound, transform.position, 0.5f);

        // 落地震动效果
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

        // 重置影子
        if (shadow != null)
        {
            shadow.transform.localScale = Vector3.one;
        }
    }

    BoardTile GetNextTile(BoardTile targetTile, int stepOffset)
    {
        // 根据当前玩家位置计算下一个格子
        // 这里需要结合你的BoardManager逻辑
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

    // 重置为正常状态
    void ResetToNormal()
    {
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
    }
}