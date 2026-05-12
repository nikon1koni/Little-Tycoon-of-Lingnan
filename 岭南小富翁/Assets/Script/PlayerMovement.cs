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
        // 等待2帧，确保所有 Start() 方法都执行完毕
        yield return null;
        yield return null;

        // 强制修正Y坐标到正确高度（无论在哪个地块）
        if (player != null && player.currentTile != null)
        {
            Vector3 correctedPos = transform.position;
            correctedPos.y = player.currentTile.transform.position.y + heightOffset;
            transform.position = correctedPos;

            Debug.Log($"[PlayerMovement] 位置修正完成: Y={correctedPos.y:F3}, 当前地块: {player.currentTile.tileName}");
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

    // �ƶ�ָ������������ڣ�
    public void MoveSteps(int steps)
    {
        if (isMoving) return;
        if (BoardManager.Instance == null || BoardManager.Instance.allTiles.Count == 0) return;

        StartCoroutine(MoveWithJumpAnimation(steps));
    }

    // ��Ծ�ƶ�Э��
    IEnumerator MoveWithJumpAnimation(int steps)
    {
        isMoving = true;
        List<BoardTile> allTiles = BoardManager.Instance.allTiles;
        int stepsMoved = 0;

        // �ؼ��޸�1����֤��ǰλ��
        if (player.currentTile.tileID < 0)
        {
            Debug.LogError($"������ҵ�ǰλ�� {player.currentTile.tileName} �ǽ������� (ID: {player.currentTile.tileID})");
            // �Զ�����������Ŀ�Ѱ·����
            BoardTile correctedTile = FindNearestWalkableTile();
            if (correctedTile != null)
            {
                Debug.Log($"����λ�õ�: {correctedTile.tileName}");
                MoveToTileImmediate(correctedTile);
                player.currentTile = correctedTile;
                player.currentTileIndex = allTiles.IndexOf(correctedTile);
            }
        }

        Debug.Log($"�ƶ���ʼ: �� {player.currentTile.tileName} (ID: {player.currentTile.tileID}) ��ʼ��Ŀ�� {steps} ��");

        while (stepsMoved < steps)
        {
            // �ؼ��޸�2��ȷ������ȷ����㿪ʼ
            int currentTileIndex = allTiles.IndexOf(player.currentTile);
            if (currentTileIndex < 0)
            {
                Debug.LogError("�Ҳ�����ҵ�ǰλ�õ�����");
                break;
            }

            int startIndex = (currentTileIndex + 1) % allTiles.Count;
            int currentSearchIndex = startIndex;
            bool foundValidTile = false;

            // ������һ����Ѱ·����
            int searchCount = 0;
            do
            {
                BoardTile candidateTile = allTiles[currentSearchIndex];
                searchCount++;

                if (candidateTile.tileID >= 0) // �Ϸ���Ѱ·����
                {
                    Debug.Log($"��{stepsMoved + 1}��: �� {player.currentTile.tileName} �ƶ��� {candidateTile.tileName} (ID: {candidateTile.tileID})");

                    // �ƶ�����
                    yield return StartCoroutine(JumpToTile(candidateTile));

                    // ����λ��
                    player.currentTile = candidateTile;
                    player.currentTileIndex = currentSearchIndex;
                    stepsMoved++;
                    foundValidTile = true;

                    yield return new WaitForSeconds(0.05f);
                    break;
                }
                else
                {
                    Debug.Log($"�����˽�������: {candidateTile.tileName} (ID: {candidateTile.tileID})");
                }

                currentSearchIndex = (currentSearchIndex + 1) % allTiles.Count;

            } while (currentSearchIndex != startIndex && searchCount < allTiles.Count);

            if (!foundValidTile)
            {
                Debug.LogError("û���ҵ����ƶ���Ŀ����ӣ����������и���");
                break;
            }
        }

        Debug.Log($"�ƶ����: �ܹ��ƶ��� {stepsMoved} ��");

        isMoving = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerMoveComplete();
        }
    }

    // ��������Ŀ�Ѱ·����
    private BoardTile FindNearestWalkableTile()
    {
        List<BoardTile> allTiles = BoardManager.Instance.allTiles;
        int currentIndex = allTiles.IndexOf(player.currentTile);

        if (currentIndex < 0) return null;

        // ��ǰ����
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

    // ����1����������Ծ���л��ߣ�
    IEnumerator JumpToTile(BoardTile targetTile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = baseY;

        // ʹ��public��jumpDuration���� (Ӧ����0.5f)
        float duration = jumpDuration;  // ��Ҫ��ʹ�ñ���������Ӳ����
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // ���㵱ǰ�߶ȣ������ߣ�
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // ˮƽλ��
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);

            // ����λ�� = ˮƽλ�� + �߶�
            transform.position = new Vector3(
                horizontalPos.x,
                baseY + height,
                horizontalPos.z
            );

            yield return null;
        }

        // ȷ��λ�þ�ȷ
        transform.position = endPos;
        transform.localScale = originalScale;
    }

    // ����2��˲����Ծ���޶�����ֱ������
    IEnumerator TeleportJumpToTile(BoardTile targetTile)
    {
        // ��������
        Vector3 startPos = transform.position;
        Vector3 midPos = (startPos + targetTile.transform.position) / 2;
        midPos.y += jumpHeight;

        float halfDuration = jumpDuration / 2;

        // ǰ��Σ�������
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(startPos, midPos, t);

            // ��΢��ת
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // ���Σ�������
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.position = Vector3.Lerp(midPos, targetTile.transform.position, t);

            // ��΢��ת
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            yield return null;
        }

        // ������ת
        transform.rotation = Quaternion.identity;
    }

    // ����3������Ч�������Ƶ��ɣ�
    IEnumerator BounceJumpToTile(BoardTile targetTile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        endPos.y = baseY;

        int bounceCount = 3;  // ��������
        float bounceHeight = jumpHeight;

        for (int bounce = 0; bounce < bounceCount; bounce++)
        {
            float bounceDuration = jumpDuration / bounceCount;
            float elapsed = 0f;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;

                // ���Ҳ�ʵ�ֵ���
                float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;

                // ˮƽ�ƶ��ٶȲ�ͬ
                float horizontalT = (bounce + t) / bounceCount;
                Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, horizontalT);

                transform.position = new Vector3(
                    horizontalPos.x,
                    baseY + height,
                    horizontalPos.z
                );

                yield return null;
            }

            // ÿ�ε����߶ȼ���
            bounceHeight *= 0.6f;
        }

        transform.position = endPos;
    }

    // �����ƶ������ӣ��޶�����
    public void MoveToTileImmediate(BoardTile tile)
    {
        if (tile == null) return;

        Vector3 targetPos = tile.transform.position;
        targetPos.y = baseY;
        transform.position = targetPos;

        currentTile = tile;
    }

    // ֱ�Ӵ��͵�ָ������
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
        // ����Ч��������ʧ
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

        // �ƶ���Ŀ��λ��
        MoveToTileImmediate(targetTile);
        UpdatePlayerTileInfo(targetTile);

        // �ٳ���
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

    // ��Ծʱ������Ч����ѡ��
    void PlayJumpSound()
    {
        // �����AudioSource���
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    // ��Ծʱ����Ч������ѡ��
    void PlayJumpParticle()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }
    }
}