using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("跳跃参数设置")]
    public float jumpHeight = 1.5f;      // 跳跃高度
    public float jumpDuration = 0.5f;    // 每一跳的动画持续时间
    public float landingDelay = 0.1f;    // 着陆后的延迟
    public float heightOffset = 0.375f;   // 角色站在格子上时的高度偏移（格子Y坐标+此偏移）

    [Header("速度参数设置")]
    [Tooltip("跳跃速度倍率，1为正常速度")]
    [Range(0.5f, 3f)]
    public float jumpSpeedMultiplier = 1.0f;

    [Header("状态")]
    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public BoardTile currentTile;

    // 引用组件
    private Player player;
    private Vector3 originalScale;
    private float baseY;  // 基准Y坐标

    void Start()
    {
        player = GetComponent<Player>();
        originalScale = transform.localScale;

        // 确定基准Y坐标（格子Y坐标+高度偏移）
        if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
        {
            BoardTile startTile = BoardManager.Instance.GetTileByID(0);
            if (startTile != null)
            {
                baseY = startTile.transform.position.y + heightOffset;
            }
            else
            {
                baseY = transform.position.y + heightOffset; // 备用方案
            }
        }
        else
        {
            baseY = transform.position.y + heightOffset; // 备用方案
        }

        // 初始化起始位置
        InitializeStartPosition();

        // 额外的位置修正协程（确保在GameManager初始化后再修正一次位置）
        StartCoroutine(FinalPositionCorrection());
    }

    // 在Start后额外修正一次位置（解决初始化时序问题）
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

    // 移动指定步数（带跳跃动画）
    public void MoveSteps(int steps)
    {
        if (isMoving) return;
        if (BoardManager.Instance == null || BoardManager.Instance.allTiles.Count == 0) return;

        StartCoroutine(MoveWithJumpAnimation(steps));
    }

    // 带跳跃动画的移动
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

                        float effectDuration = candidateTile.OnPassed(player);
                        
                        if (effectDuration > 0f)
                        {
                            yield return new WaitForSeconds(effectDuration);
                        }

                        // 等待建筑特效播放完毕
                        while (BuildingEffectSystem.Instance != null && BuildingEffectSystem.Instance.IsPlayingEffects)
                        {
                            yield return new WaitForEndOfFrame();
                        }

                    // 检查是否经过起点（tileID == 0 或 tileType == Start）
                    if (candidateTile.tileID == 0 || candidateTile.tileType == BoardTile.TileType.Start)
                    {
                        Debug.Log($"玩家经过起点tileID: {candidateTile.tileID}，发放工资奖励");

                        // 确保位置正确
                        MoveToTileImmediate(candidateTile);

                        isMoving = false;

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.OnPlayerMoveComplete();
                        }

                        yield break;  // 提前结束移动
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

    // 查找最近的可走格子
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

    // 方案1：抛物线跳跃（正弦曲线高度+线性水平移动）
    IEnumerator JumpToTile(BoardTile targetTile)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerJump);

        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = baseY;

        // 使用public的jumpDuration变量（默认0.5f）
        float duration = jumpDuration / jumpSpeedMultiplier;  // 应用速度倍率，值越大越快
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 使用正弦波计算高度，0到π，最高点在中间
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // 水平移动
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);

            // 最终位置 = 水平位置 + 高度
            transform.position = new Vector3(
                horizontalPos.x,
                baseY + height,
                horizontalPos.z
            );

            yield return null;
        }

        // 确保最终位置正确
        transform.position = endPos;
        transform.localScale = originalScale;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerLand);
    }

    // 方案2：传送门/瞬间移动（跳起来在空中翻转然后落下）
    IEnumerator TeleportJumpToTile(BoardTile targetTile)
    {
        // 计算路径点
        Vector3 startPos = transform.position;
        Vector3 midPos = (startPos + targetTile.transform.position) / 2;
        midPos.y += jumpHeight;

        float halfDuration = jumpDuration / 2;

        // 第一阶段：跳起到中间点
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(startPos, midPos, t);

            // 旋转效果
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // 第二阶段：从中间点落下
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(midPos, targetTile.transform.position, t);

            // 旋转效果
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // 重置旋转
        transform.rotation = Quaternion.identity;
    }

    // 方案3：弹跳跳跃（像橡胶球一样弹过去）
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

                // 正弦波计算单次弹跳高度
                float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;

                // 水平移动百分比
                float horizontalT = (bounce + t) / bounceCount;
                Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, horizontalT);

                transform.position = new Vector3(
                    horizontalPos.x,
                    baseY + height,
                    horizontalPos.z
                );

                yield return null;
            }

            // 每次弹跳高度衰减
            bounceHeight *= 0.6f;
        }

        transform.position = endPos;
    }

    // 直接传送到指定格子（无动画）
    public void MoveToTileImmediate(BoardTile tile)
    {
        if (tile == null) return;

        Vector3 targetPos = tile.transform.position;
        targetPos.y = baseY;
        transform.position = targetPos;

        currentTile = tile;
    }

    // 传送到指定格子
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
        // 第一阶段：缩小消失
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

        // 瞬间传送并更新信息
        MoveToTileImmediate(targetTile);
        UpdatePlayerTileInfo(targetTile);

        // 第二阶段：放大出现
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

    // 播放跳跃音效（备用方法）
    void PlayJumpSound()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerJump);
    }

    // 播放跳跃粒子效果（备用方法）
    void PlayJumpParticle()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }
    }

    // 设置跳跃速度倍率
    public void SetJumpSpeedMultiplier(float multiplier)
    {
        jumpSpeedMultiplier = Mathf.Clamp(multiplier, 0.5f, 3f);
        Debug.Log($"PlayerMovement: 设置跳跃速度倍率={jumpSpeedMultiplier}x");
    }

    // 获取跳跃速度倍率
    public float GetJumpSpeedMultiplier()
    {
        return jumpSpeedMultiplier;
    }
}