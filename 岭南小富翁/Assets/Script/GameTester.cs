// GameTester.cs - ���ڲ���
using UnityEngine;

public class GameTester : MonoBehaviour
{
    void Update()
    {
        // ���ٲ��Կ�ݼ�
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TestMovePlayer(Random.Range(1, 7));
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // ������Ϸ
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // ֱ����������1
            JumpToTile(1);
        }
    }

    void JumpToTile(int tileID)
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.players.Count == 0) return;

        Player player = GameManager.Instance.players[0];
        BoardTile tile = BoardManager.Instance?.GetTileByID(tileID);

        if (tile != null && player != null)
        {
            player.MoveToTile(tile, true);
            Debug.Log($"��ת��: {tile.tileName}");
        }
    }
}