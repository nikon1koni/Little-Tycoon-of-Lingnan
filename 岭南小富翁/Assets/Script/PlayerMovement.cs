using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("???????????")]
    public float jumpHeight = 1.5f;      // ??????
    public float jumpDuration = 0.5f;    // ????????????????
    public float landingDelay = 0.1f;    // ???????
    public float heightOffset = 0.375f;   // ???????????????????????????Y????+????

    [Header("?????????")]
    [Tooltip("??????????1????????")]
    [Range(0.5f, 3f)]
    public float jumpSpeedMultiplier = 1.0f;

    [Header("??")]
    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public BoardTile currentTile;

    private int moveCount = 0;  // ???????????????0???????????

    // ??????
    private Player player;
    private Vector3 originalScale;
    private float baseY;  // ????Y????

    void Start()
    {
        player = GetComponent<Player>();
        originalScale = transform.localScale;

        // ?????????Y????????????Y????+????
        if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
        {
            BoardTile startTile = BoardManager.Instance.GetTileByID(0);
            if (startTile != null)
            {
                baseY = startTile.transform.position.y + heightOffset;
            }
            else
            {
                baseY = transform.position.y + heightOffset; // ??????????
            }
        }
        else
        {
            baseY = transform.position.y + heightOffset; // ??????????
        }

        // ????????????
        InitializeStartPosition();

        // ???????????????????GameManager????????????????
        StartCoroutine(FinalPositionCorrection());
    }

    // ??Start?????????????????????????????????
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

    // ???????????????????????
    public void MoveSteps(int steps)
    {
        if (isMoving) return;
        if (BoardManager.Instance == null || BoardManager.Instance.allTiles.Count == 0) return;

        StartCoroutine(MoveWithJumpAnimation(steps));
    }

    // ??????????????
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

                        // ??????????????????
                        while (BuildingEffectSystem.Instance != null && BuildingEffectSystem.Instance.IsPlayingEffects)
                        {
                            yield return new WaitForEndOfFrame();
                        }

                    // ??????????tileID == 0 ?? tileType == Start
                    if (candidateTile.tileID == 0 || candidateTile.tileType == BoardTile.TileType.Start)
                    {
                        Debug.Log($"???????tileID: {candidateTile.tileID}??????????????");

                        // ????????????
                        MoveToTileImmediate(candidateTile);

                        isMoving = false;

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.OnPlayerMoveComplete();
                        }

                        yield break;  // ???????
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

    // ????????????????
    private BoardTile FindNearestWalkableTile()
    {
        List<BoardTile> allTiles = BoardManager.Instance.allTiles;
        int currentIndex = allTiles.IndexOf(player.currentTile);

        if (currentIndex < 0) return null;

        // ???????
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

    [Header("???????")]
    [Tooltip("??????????????????????????????????????????85???")]
    [Range(0, 180)]
    public float rotationThreshold = 85f;  // ????90???????????

    private bool hasLeftStartTile = false;  // ??????????????tile0

    // ????1???????????????????????+???????????
    IEnumerator JumpToTile(BoardTile targetTile)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerJump);

        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = baseY;

        // ?????????
        Vector3 moveDirection = (endPos - startPos).normalized;
        moveDirection.y = 0;  // ???????????
        
        // ?????????????
        bool needRotation = false;
        Quaternion targetRotation = transform.rotation;
        
        // ????????tileID 0??5??10??15
        int targetTileID = targetTile.tileID;
        bool isTurningPoint = targetTileID == 0 || targetTileID == 5 || targetTileID == 10 || targetTileID == 15;
        
        // ?????tile0?????????????????????
        if (targetTileID == 0 && !hasLeftStartTile)
        {
            // ?????????tile0?????????????????
            isTurningPoint = false;
        }
        
        if (isTurningPoint && moveDirection.magnitude > 0.01f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(moveDirection);
            float angleDiff = Quaternion.Angle(transform.rotation, desiredRotation);
            
            // ?????????????????????90???????????
            if (angleDiff > rotationThreshold)
            {
                needRotation = true;
                targetRotation = desiredRotation;
            }
        }

        // ???public??jumpDuration?????????0.5f??
        float duration = jumpDuration / jumpSpeedMultiplier;  // ?????????????????
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // ???????????????0????????????????
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // ?????
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);

            // ???????? = ?????? + ???
            transform.position = new Vector3(
                horizontalPos.x,
                baseY + height,
                horizontalPos.z
            );

            // ??????????????????????????
            if (needRotation)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, t);
            }

            yield return null;
        }

        // ??????????????
        transform.position = endPos;
        transform.localScale = originalScale;
        
        // ?????????????
        if (needRotation)
        {
            transform.rotation = targetRotation;
        }
        
        // ???????????????????tile0?????
        if (currentTile != null && currentTile.tileID == 0 && targetTileID != 0)
        {
            hasLeftStartTile = true;
        }

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerLand);
    }

    // ????2??????????????/?????????????????????
    IEnumerator TeleportJumpToTile(BoardTile targetTile)
    {
        // ???????????
        Vector3 startPos = transform.position;
        Vector3 midPos = (startPos + targetTile.transform.position) / 2;
        midPos.y += jumpHeight;

        float halfDuration = jumpDuration / 2;

        // ??????????????
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(startPos, midPos, t);

            // ???????
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // ??????????????
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(midPos, targetTile.transform.position, t);

            // ???????
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // ???????
        transform.rotation = Quaternion.identity;
    }

    // ????3????????????????????????
    IEnumerator BounceJumpToTile(BoardTile targetTile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = baseY;

        int bounceCount = 3;  // ????????
        float bounceHeight = jumpHeight;

        for (int bounce = 0; bounce < bounceCount; bounce++)
        {
            float bounceDuration = jumpDuration / bounceCount;
            float elapsed = 0f;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;

                // ???????????????????????
                float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;

                // ??????????
                float horizontalT = (bounce + t) / bounceCount;
                Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, horizontalT);

                transform.position = new Vector3(
                    horizontalPos.x,
                    baseY + height,
                    horizontalPos.z
                );

                yield return null;
            }

            // ????????????
            bounceHeight *= 0.6f;
        }

        transform.position = endPos;
    }

    // ????????????????????
    public void MoveToTileImmediate(BoardTile tile)
    {
        if (tile == null) return;

        Vector3 targetPos = tile.transform.position;
        targetPos.y = baseY;
        transform.position = targetPos;

        currentTile = tile;
    }

    // ????????
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
        // ??????????????
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

        // ????????????????
        MoveToTileImmediate(targetTile);
        UpdatePlayerTileInfo(targetTile);

        // ?????????????
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

    // ???????????????????????
    void PlayJumpSound()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.PlayerJump);
    }

    // ???????????????????????????
    void PlayJumpParticle()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }
    }

    // ?????????????
    public void SetJumpSpeedMultiplier(float multiplier)
    {
        jumpSpeedMultiplier = Mathf.Clamp(multiplier, 0.5f, 3f);
        Debug.Log($"PlayerMovement: ?????????????={jumpSpeedMultiplier}x");
    }

    // ????????????
    public float GetJumpSpeedMultiplier()
    {
        return jumpSpeedMultiplier;
    }
}