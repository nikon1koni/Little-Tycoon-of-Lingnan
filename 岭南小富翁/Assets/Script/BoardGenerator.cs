// BoardGenerator.cs - 棋盘生成器（三角形布局）
using UnityEngine;
using System.Collections.Generic;

public class BoardGenerator : MonoBehaviour
{
    [Header("棋盘设置")]
    public GameObject gridTilePrefab; // 棋盘格 GridTile 预制体

    public int rows = 3; // 行数，每行生成递增数量的棋盘格子

    public float tileSize = 1f; // 格子大小，用于平移定位相邻格子

    public Vector2 offset = new Vector2(0, 0); // 整体偏移量

    private List<GameObject> generatedTiles = new List<GameObject>();

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            // 计算当前行的格子数量，第n行有 n+1 个棋盘格子的三角形布局
            int tileCount = row + 1; // 表示第一行1个、第二行2个...的三角形棋盘

            for (int col = 0; col < tileCount; col++)
            {
                // 实例化格子
                var tileObj = Instantiate(gridTilePrefab, transform);
                var boardTile = tileObj.GetComponent<BoardTile>();
                if (boardTile != null)
                {
                    boardTile.tileID = generatedTiles.Count;
                    boardTile.tileName = $"Tile_{row}_{col}";
                }

                // 计算位置：基于行数和列索引计算x,z偏移，使格子居中排列
                float x = (col - tileCount * 0.5f) * tileSize + offset.x;
                float z = -row * tileSize * 0.866f + offset.y;

                tileObj.transform.localPosition = new Vector3(x, 0, z);

                // 设置旋转：使每个格子旋转45度形成菱形排列
                tileObj.transform.localRotation = Quaternion.Euler(0, 45, 0);

                generatedTiles.Add(tileObj);
            }
        }

        Debug.Log($"棋盘生成完成: {generatedTiles.Count}个地块");
    }
}
