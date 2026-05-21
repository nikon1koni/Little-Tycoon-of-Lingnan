using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("跳跃移动参数")]
    public float jumpHeight = 1.5f;      // 跳跃高度
    public float jumpDuration = 0.5f;    // 单次跳跃持续时间
    public float landingDelay = 0.1f;    // 落地后停顿时间
    public float heightOffset = 0.375f;   // 玩家在地块上的高度偏移（地块高度的一半+玩家高度的一半）

    [Header("状态")]
    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public BoardTile currentTile;

    // 私有变量
    private Player player;
    private Vector3 originalScale;
    private float baseY;  // 基础Y坐标

    void Start()
    {
        player = GetComponent<Player>();
        originalScale = transform.localScale;

        // 计算基础Y坐标（基于起始地块位置+高度偏移）
        if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
        {
            BoardTile startTile = BoardManager.Instance.GetTileByID(0);
            if (startTile != null)
            {
                baseY = startTile.transform.position.y + heightOffset;
            }
            else
            {
                baseY = transform.position.y + heightOffset; // 回退方案
            }
        }
        else
        {
            baseY = transform.position.y + heightOffset; // 回退方案
        }

        // 初始化位置
        InitializeStartPosition();

        // 延迟修正：确保在其他脚本（如GameManager）设置位置后，最终位置正确
        StartCoroutine(FinalPositionCorrection());
    }

    // 最终位置修正协程（解决初始化时序问题）
    IEnumerator FinalPositionCorrection()
    {
        yield return null;
        yield return null;

        if (player != null && player.currentTile != null)
        {
            Vector3 correctedPos = transform.position;
            correctedPos.y = player.currentTile.transform.position.y + heightOffset;
            transform.position = correctedPos;
        }
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

    // 移动指定的步数，带动画
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

        if (player.currentTile.tileID < 0)
        {
            BoardTile correctedTile = FindNearestWalkableTile();
            if (correctedTile != null)
            {
                MoveToTileImmediate(correctedTile);
                player.currentTile = correctedTile;
                player.currentTileIndex = allTiles.IndexOf(correctedTile);
            }
        }

        while (stepsMoved < steps)
        {
            int currentTileIndex = allTiles.IndexOf(player.currentTile);
            if (currentTileIndex < 0) break;

            int startIndex = (currentTileIndex + 1) % allTiles.Count;
            int currentSearchIndex = startIndex;
            bool foundValidTile = false;
            int searchCount = 0;

            do
            {
                BoardTile candidateTile = allTiles[currentSearchIndex];
                searchCount++;

                if (candidateTile.tileID >= 0)
                    {
                        yield return StartCoroutine(JumpToTile(candidateTile));

                        player.currentTile = candidateTile;
                        player.currentTileIndex = currentSearchIndex;
                        stepsMoved++;
                        foundValidTile = true;

                        candidateTile.OnPassed(player);

                    // 检查是否到达或经过起点（tileID == 0 或 tileType == Start）
                    if (candidateTile.tileID == 0 || candidateTile.tileType == BoardTile.TileType.Start)
                    {
                        Debug.Log($"玩家到达起点（tileID: {candidateTile.tileID}），停留在此处");

                        // 确保玩家在起点的正确位置
                        MoveToTileImmediate(candidateTile);

                        isMoving = false;

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.OnPlayerMoveComplete();
                        }

                        yield break;  // 停止继续移动
                    }

                    yield return new WaitForSeconds(0.05f);
                    break;
                }

                currentSearchIndex = (currentSearchIndex + 1) % allTiles.Count;

            } while (currentSearchIndex != startIndex && searchCount < allTiles.Count);

            if (!foundValidTile) break;
        }

        isMoving = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerMoveComplete();
        }
    }

    // 寻找附近的可行走地块
    private BoardTile FindNearestWalkableTile()
    {
        List<BoardTile> allTiles = BoardManager.Instance.allTiles;
        int currentIndex = allTiles.IndexOf(player.currentTile);

        if (currentIndex < 0) return null;

        // 向前寻找
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

    // 方案1：简单的正弦波跳跃动画
    IEnumerator JumpToTile(BoardTile targetTile)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerJump);

        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = baseY;

        // 使用public的jumpDuration参数 (应该是0.5f)
        float duration = jumpDuration;  // 注意：使用参数而不是硬编码
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 计算当前高度（正弦曲线）
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // 水平位置
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);

            // 最终位置 = 水平位置 + 高度
            transform.position = new Vector3(
                horizontalPos.x,
                baseY + height,
                horizontalPos.z
            );

            yield return null;
        }

        // 确保位置正确
        transform.position = endPos;
        transform.localScale = originalScale;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerLand);
    }

    // 方案2：抛物线跳跃（瞬移方式，直接跳过去）
    IEnumerator TeleportJumpToTile(BoardTile targetTile)
    {
        // 起始位置
        Vector3 startPos = transform.position;
        Vector3 midPos = (startPos + targetTile.transform.position) / 2;
        midPos.y += jumpHeight;

        float halfDuration = jumpDuration / 2;

        // 前半段：跳到空中
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

        // 后半段：跳下来
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

    // 方案3：弹跳效果（多次弹跳完成）
    IEnumerator BounceJumpToTile(BoardTile targetTile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = baseY;

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

                // 使用正弦实现弹跳
                float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;

                // 水平移动速度不同
                float horizontalT = (bounce + t) / bounceCount;
                Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, horizontalT);

                transform.position = new Vector3(
                    horizontalPos.x,
                    baseY + height,
                    horizontalPos.z
                );

                yield return null;
            }

            // 每次弹跳高度递减
            bounceHeight *= 0.6f;
        }

        transform.position = endPos;
    }

    // 立即移动到目标位置，无动画
    public void MoveToTileImmediate(BoardTile tile)
    {
        if (tile == null) return;

        Vector3 targetPos = tile.transform.position;
        targetPos.y = baseY;
        transform.position = targetPos;

        currentTile = tile;
    }

    // 直接传送到指定地块
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
        // 消失效果：逐渐消失
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

        // 再次出现
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
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerJump);
    }

    // 跳跃时播放粒子效果（可选）
    void PlayJumpParticle()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }
    }
}