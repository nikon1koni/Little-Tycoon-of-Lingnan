// EnhancedPlayerMovement.cs
using System.Collections;
using UnityEngine;

public class EnhancedPlayerMovement : MonoBehaviour
{
    [Header("跳跃参数")]
    public float jumpPower = 2f;
    public float jumpSpeed = 5f;
    public AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("旋转效果")]
    public float rotationSpeed = 360f;
    public bool rotateDuringJump = true;

    [Header("挤压拉伸效果")]
    public float squashAmount = 0.2f;
    public float stretchAmount = 0.3f;

    [Header("尾迹效果")]
    public bool enableTrail = true;
    public float trailTime = 0.2f;

    [Header("阴影效果")]
    public GameObject shadowPrefab;
    private GameObject shadow;

    [Header("音效")]
    public AudioClip jumpSound;
    public AudioClip landSound;

    private bool isJumping = false;
    private Vector3 originalScale;
    private TrailRenderer trail;

    void Start()
    {
        originalScale = transform.localScale;

        if (enableTrail)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.widthMultiplier = 0.1f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = GetComponent<Renderer>().material.color;
            trail.endColor = new Color(1, 1, 1, 0);
        }

        if (shadowPrefab != null)
        {
            shadow = Instantiate(shadowPrefab, transform.position, Quaternion.identity);
            shadow.transform.parent = transform;
            shadow.transform.localPosition = Vector3.down * 0.5f;
        }
    }

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
            Vector3 startPos = transform.position;
            BoardTile nextTile = GetNextTile(targetTile, i);
            Vector3 endPos = nextTile.transform.position;
            endPos.y = startPos.y;

            yield return StartCoroutine(SingleJump(startPos, endPos));
            yield return StartCoroutine(LandingEffect());
            yield return new WaitForSeconds(0.1f);
        }

        isJumping = false;

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

        if (jumpSound != null)
            AudioSource.PlayClipAtPoint(jumpSound, transform.position, 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = jumpCurve.Evaluate(t);

            Vector3 position = CalculateBezierPoint(startPos, controlPoint, endPos, curveT);
            transform.position = position;

            if (rotateDuringJump)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            ApplyJumpSquashAndStretch(t);

            if (shadow != null)
            {
                shadow.transform.position = new Vector3(position.x, 0, position.z);
                shadow.transform.localScale = Vector3.one * (1 - t * 0.5f);
            }

            yield return null;
        }

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

        if (t < 0.5f)
        {
            float stretchT = t * 2;
            newScale.y = originalScale.y * (1 + stretchAmount * stretchT);
            newScale.x = originalScale.x * (1 - squashAmount * stretchT * 0.5f);
            newScale.z = originalScale.z * (1 - squashAmount * stretchT * 0.5f);
        }
        else
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
        if (landSound != null)
            AudioSource.PlayClipAtPoint(landSound, transform.position, 0.5f);

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

        if (shadow != null)
        {
            shadow.transform.localScale = Vector3.one;
        }
    }

    BoardTile GetNextTile(BoardTile targetTile, int stepOffset)
    {
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

    void ResetToNormal()
    {
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
    }
}
