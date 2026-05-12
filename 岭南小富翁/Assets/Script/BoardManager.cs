using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("Tile References")]
    public List<BoardTile> allTiles = new List<BoardTile>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeBoard();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeBoard()
    {
        // 自动获取所有格子（确保这些格子是BoardManager的子对象）
        allTiles.Clear();
        BoardTile[] tiles = GetComponentsInChildren<BoardTile>();
        allTiles.AddRange(tiles);

        // 按TileID排序
        allTiles.Sort((a, b) => a.tileID.CompareTo(b.tileID));

        Debug.Log($"棋盘初始化完成，共有 {allTiles.Count} 个格子");
    }

    public BoardTile GetTileAfterSteps(BoardTile currentTile, int steps)
    {
        if (allTiles.Count == 0)
        {
            Debug.LogError("棋盘没有格子！");
            return null;
        }

        int currentIndex = allTiles.IndexOf(currentTile);
        if (currentIndex == -1)
        {
            Debug.LogError("当前格子不在棋盘列表中！");
            return allTiles[0]; // 默认返回起点
        }

        int targetIndex = (currentIndex + steps) % allTiles.Count;
        return allTiles[targetIndex];
    }

    public BoardTile GetTileByID(int id)
    {
        foreach (BoardTile tile in allTiles)
        {
            if (tile.tileID == id) return tile;
        }

        Debug.LogWarning($"未找到ID为 {id} 的格子");
        return allTiles.Count > 0 ? allTiles[0] : null;
    }
}
