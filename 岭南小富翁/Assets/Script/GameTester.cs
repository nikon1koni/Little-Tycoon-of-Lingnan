// GameTester.cs - 游戏测试工具
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTester : MonoBehaviour
{
    void Update()
    {
        // 调试快捷键
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
        // 重新加载场景
        SceneManager.LoadScene("New");
        Debug.Log("游戏已重启");
    }

    void GoToTile(int tileIndex)
    {
        // 跳转到指定格子
        var tiles = FindObjectsOfType<BoardTile>();
        if (tiles.Length > tileIndex)
        {
            var tile = tiles[tileIndex];
            Debug.Log($"格子: {tile.tileName}");
        }
        else
        {
            Debug.LogWarning($"索引{tileIndex}的格子不存在");
        }
    }
}
