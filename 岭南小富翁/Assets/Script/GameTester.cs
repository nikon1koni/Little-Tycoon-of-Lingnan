// GameTester.cs - 用于测试
using UnityEngine;

public class GameTester : MonoBehaviour
{
    void Update()
    {
        // 快速测试快捷键
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TestMovePlayer(Random.Range(1, 7));
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // 重置游戏
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 直接跳到格子1
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
            Debug.Log($"跳转到: {tile.tileName}");
        }
    }
}