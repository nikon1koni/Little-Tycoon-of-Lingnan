using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public GameObject gridTilePrefab; // 拖入 GridTile 预制体
    public int rows = 3; // 菱形行数（根据你的地图调整）
    public float tileSize = 1f; // 格子大小（与平面缩放对应）
    public Vector2 offset = new Vector2(0, 0); // 格子间的偏移

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            // 计算当前行的格子数量（菱形：第n行有 2n+1 个？或根据你的地图调整）
            int tileCount = row + 1; // 示例：第一行1个，第二行2个？需匹配你的地图！
            for (int col = 0; col < tileCount; col++)
            {
                // 实例化格子
                GameObject tile = Instantiate(gridTilePrefab, transform);
                // 计算位置（菱形的坐标逻辑：x和z偏移，让格子呈菱形排列）
                float x = (col - tileCount / 2f) * tileSize + offset.x;
                float z = (row - rows / 2f) * tileSize + offset.y;
                tile.transform.position = new Vector3(x, 0, z);
                // （可选）设置格子的旋转，让菱形更自然（如 45度？需测试）
                tile.transform.rotation = Quaternion.Euler(0, 45, 0);
            }
        }
    }
}