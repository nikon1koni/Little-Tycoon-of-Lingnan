// GameTester.cs - 游戏测试器（开发调试用）
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTester : MonoBehaviour
{
    void Update()
    {
        // 快速测试快捷键
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            GoToTile(0);
        }
    }

    void RestartGame()
    {
        // 重启游戏
        SceneManager.LoadScene("New");
        Debug.Log("游戏已重启");
    }

    void GoToTile(int tileIndex)
    {
        // 直接跳转到地块1
        var tiles = FindObjectsOfType<BoardTile>();
        if (tiles.Length > tileIndex)
        {
            var tile = tiles[tileIndex];
            Debug.Log($"跳转至: {tile.tileName}");
        }
        else
        {
            Debug.LogWarning($"未找到索引为{tileIndex}的地块");
        }
    }
}
