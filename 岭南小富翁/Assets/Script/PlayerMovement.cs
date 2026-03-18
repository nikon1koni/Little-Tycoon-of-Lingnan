using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("跳跃移动设置")]
    public float jumpHeight = 1.5f;      // 跳跃高度
    public float jumpDuration = 0.5f;    // 单次跳跃持续时间
    public float landingDelay = 0.1f;    // 落地后停留时间

    [Header("状态")]
    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public BoardTile currentTile;

    // 引用
    private Player player;
    private Vector3 originalScale;
    private float originalY;  // 记录原始Y坐标用于动画

    void Start()
    {
        player = GetComponent<Player>();
        originalScale = transform.localScale;
        originalY = transform.position.y;

        // 初始化位置
        InitializeStartPosition();
    }

    void InitializeStartPosition()
    {
        if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
        {
            BoardTile startTile = BoardManager.Instance.GetTileByID(0);
            if (startTile != null)
            {
                MoveToTileImmediate(startTile);
                player.currentTile = startTile;
                player.currentTileIndex = 0;
            }
        }
    }

    // 移动指定步数（主入口）
    public void MoveSteps(int steps)
    {
        if (isMoving) return;
        if (BoardManager.Instance == null || BoardManager.Instance.allTiles.Count == 0) return;

        StartCoroutine(MoveWithJumpAnimation(steps));
    }

    // 跳跃移动协程
    IEnumerator MoveWithJumpAnimation(int steps)
    {
        isMoving = true;
        List<BoardTile> allTiles = BoardManager.Instance.allTiles;

        for (int i = 0; i < steps; i++)
        {
            // 计算下一个格子
            int nextIndex = (player.currentTileIndex + 1) % allTiles.Count;
            BoardTile nextTile = allTiles[nextIndex];

            // 执行跳跃动画
            yield return StartCoroutine(JumpToTile(nextTile));

            // 更新玩家位置
            player.currentTile = nextTile;
            player.currentTileIndex = nextIndex;

            // 短暂停留
            yield return new WaitForSeconds(landingDelay);

            // 如果是最后一步，触发格子事件
            if (i == steps - 1)
            {
                yield return new WaitForSeconds(0.2f);
                nextTile.OnLanded(player);
            }
        }

        isMoving = false;
    }

    // 方法1：抛物线跳跃（有弧线）
    IEnumerator JumpToTile(BoardTile targetTile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = originalY;  // 保持原始Y高度

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;

            // 计算当前高度（抛物线）
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // 水平位置（线性插值）
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);

            // 最终位置 = 水平位置 + 高度
            transform.position = new Vector3(
                horizontalPos.x,
                originalY + height,
                horizontalPos.z
            );

            // 可选：跳跃时轻微缩放（弹跳效果）
            float scaleFactor = 1 + Mathf.Sin(t * Mathf.PI) * 0.1f;
            transform.localScale = originalScale * scaleFactor;

            yield return null;
        }

        // 确保最终位置准确
        transform.position = endPos;
        transform.localScale = originalScale;
    }

    // 方法2：瞬移跳跃（无动画，直接跳）
    IEnumerator TeleportJumpToTile(BoardTile targetTile)
    {
        // 先向上跳
        Vector3 startPos = transform.position;
        Vector3 midPos = (startPos + targetTile.transform.position) / 2;
        midPos.y += jumpHeight;

        float halfDuration = jumpDuration / 2;

        // 前半段：向上跳
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(startPos, midPos, t);

            // 轻微旋转
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // 后半段：向下落
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(midPos, targetTile.transform.position, t);

            // 轻微旋转
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // 重置旋转
        transform.rotation = Quaternion.identity;
    }

    // 方法3：弹跳效果（类似弹簧）
    IEnumerator BounceJumpToTile(BoardTile targetTile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = originalY;

        int bounceCount = 3;  // 弹跳次数
        float bounceHeight = jumpHeight;

        for (int bounce = 0; bounce < bounceCount; bounce++)
        {
            float bounceDuration = jumpDuration / bounceCount;
            float elapsed = 0f;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;

                // 正弦波实现弹跳
                float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;

                // 水平移动速度不同
                float horizontalT = (bounce + t) / bounceCount;
                Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, horizontalT);

                transform.position = new Vector3(
                    horizontalPos.x,
                    originalY + height,
                    horizontalPos.z
                );

                yield return null;
            }

            // 每次弹跳高度减少
            bounceHeight *= 0.6f;
        }

        transform.position = endPos;
    }

    // 立即移动到格子（无动画）
    public void MoveToTileImmediate(BoardTile tile)
    {
        if (tile == null) return;

        Vector3 targetPos = tile.transform.position;
        targetPos.y = originalY;
        transform.position = targetPos;

        currentTile = tile;
    }

    // 直接传送到指定格子
    public void TeleportToTile(BoardTile targetTile, bool withAnimation = false)
    {
        if (targetTile == null) return;

        StopAllCoroutines();

        if (withAnimation)
        {
            StartCoroutine(TeleportWithEffect(targetTile));
        }
        else
        {
            MoveToTileImmediate(targetTile);
            UpdatePlayerTileInfo(targetTile);
            targetTile.OnLanded(player);
        }
    }

    IEnumerator TeleportWithEffect(BoardTile targetTile)
    {
        // 传送效果：先消失
        float disappearTime = 0.3f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < disappearTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disappearTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        // 移动到目标位置
        MoveToTileImmediate(targetTile);
        UpdatePlayerTileInfo(targetTile);

        // 再出现
        elapsed = 0f;
        while (elapsed < disappearTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disappearTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        targetTile.OnLanded(player);
    }

    void UpdatePlayerTileInfo(BoardTile tile)
    {
        if (player != null && BoardManager.Instance != null)
        {
            player.currentTile = tile;
            player.currentTileIndex = BoardManager.Instance.allTiles.IndexOf(tile);
        }
    }

    // 跳跃时播放音效（可选）
    void PlayJumpSound()
    {
        // 如果有AudioSource组件
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    // 跳跃时粒子效果（可选）
    void PlayJumpParticle()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }
    }
}