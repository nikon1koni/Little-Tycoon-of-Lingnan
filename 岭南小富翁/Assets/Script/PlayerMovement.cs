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
        int stepsMoved = 0;

        // 关键修复1：验证当前位置
        if (player.currentTile.tileID < 0)
        {
            Debug.LogError($"错误：玩家当前位置 {player.currentTile.tileName} 是建筑格子 (ID: {player.currentTile.tileID})");
            // 自动纠正到最近的可寻路格子
            BoardTile correctedTile = FindNearestWalkableTile();
            if (correctedTile != null)
            {
                Debug.Log($"纠正位置到: {correctedTile.tileName}");
                MoveToTileImmediate(correctedTile);
                player.currentTile = correctedTile;
                player.currentTileIndex = allTiles.IndexOf(correctedTile);
            }
        }

        Debug.Log($"移动开始: 从 {player.currentTile.tileName} (ID: {player.currentTile.tileID}) 开始，目标 {steps} 步");

        while (stepsMoved < steps)
        {
            // 关键修复2：确保从正确的起点开始
            int currentTileIndex = allTiles.IndexOf(player.currentTile);
            if (currentTileIndex < 0)
            {
                Debug.LogError("找不到玩家当前位置的索引");
                break;
            }

            int startIndex = (currentTileIndex + 1) % allTiles.Count;
            int currentSearchIndex = startIndex;
            bool foundValidTile = false;

            // 查找下一个可寻路格子
            int searchCount = 0;
            do
            {
                BoardTile candidateTile = allTiles[currentSearchIndex];
                searchCount++;

                if (candidateTile.tileID >= 0) // 合法可寻路格子
                {
                    Debug.Log($"第{stepsMoved + 1}步: 从 {player.currentTile.tileName} 移动到 {candidateTile.tileName} (ID: {candidateTile.tileID})");

                    // 移动动画
                    yield return StartCoroutine(JumpToTile(candidateTile));

                    // 更新位置
                    player.currentTile = candidateTile;
                    player.currentTileIndex = currentSearchIndex;
                    stepsMoved++;
                    foundValidTile = true;

                    yield return new WaitForSeconds(0.05f);
                    break;
                }
                else
                {
                    Debug.Log($"跳过了建筑格子: {candidateTile.tileName} (ID: {candidateTile.tileID})");
                }

                currentSearchIndex = (currentSearchIndex + 1) % allTiles.Count;

            } while (currentSearchIndex != startIndex && searchCount < allTiles.Count);

            if (!foundValidTile)
            {
                Debug.LogError("没有找到可移动的目标格子！已搜索所有格子");
                break;
            }
        }

        Debug.Log($"移动完成: 总共移动了 {stepsMoved} 步");

        isMoving = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerMoveComplete();
        }
    }

    // 查找最近的可寻路格子
    private BoardTile FindNearestWalkableTile()
    {
        List<BoardTile> allTiles = BoardManager.Instance.allTiles;
        int currentIndex = allTiles.IndexOf(player.currentTile);

        if (currentIndex < 0) return null;

        // 向前查找
        for (int i = 1; i < allTiles.Count; i++)
        {
            int forwardIndex = (currentIndex + i) % allTiles.Count;
            if (allTiles[forwardIndex].tileID >= 0)
            {
                return allTiles[forwardIndex];
            }
        }

        return null;
    }

    // 方法1：抛物线跳跃（有弧线）
    IEnumerator JumpToTile(BoardTile targetTile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = originalY;

        // 使用public的jumpDuration变量 (应该是0.5f)
        float duration = jumpDuration;  // 重要：使用变量而不是硬编码
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 计算当前高度（抛物线）
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // 水平位置
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);

            // 最终位置 = 水平位置 + 高度
            transform.position = new Vector3(
                horizontalPos.x,
                originalY + height,
                horizontalPos.z
            );

            yield return null;
        }

        // 确保位置精确
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