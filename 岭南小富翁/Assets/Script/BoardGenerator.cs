// BoardGenerator.cs - 棋盘生成器
using UnityEngine;
using System.Collections.Generic;

public class BoardGenerator : MonoBehaviour
{
    [Header("配置")]
    public GameObject gridTilePrefab; // 网格瓦片预制体

    public int rows = 3; // 棋盘行数（金字塔结构）

    public float tileSize = 1f; // 瓦片尺寸

    public Vector2 offset = new Vector2(0, 0); // 偏移量

    private List<GameObject> generatedTiles = new List<GameObject>();

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            // 第n行有n+1个瓦片（金字塔结构）
            int tileCount = row + 1; // 第0行1个,第1行2个...以此类推

            for (int col = 0; col < tileCount; col++)
            {
                // 实例化瓦片
                var tileObj = Instantiate(gridTilePrefab, transform);
                var boardTile = tileObj.GetComponent<BoardTile>();
                if (boardTile != null)
                {
                    boardTile.tileID = generatedTiles.Count;
                    boardTile.tileName = $"Tile_{row}_{col}";
                }

                // 计算六边形布局的x,z坐标
                float x = (col - tileCount * 0.5f) * tileSize + offset.x;
                float z = -row * tileSize * 0.866f + offset.y;

                tileObj.transform.localPosition = new Vector3(x, 0, z);

                // 旋转45度以匹配六边形布局
                tileObj.transform.localRotation = Quaternion.Euler(0, 45, 0);

                generatedTiles.Add(tileObj);
            }
        }

        Debug.Log($"生成瓦片数量: {generatedTiles.Count} 个");
    }
}
