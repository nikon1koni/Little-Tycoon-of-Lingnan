using UnityEngine;
using System.Collections.Generic;

public class BuildingTileGenerator : MonoBehaviour
{
    [Header("建筑格子预制体")]
    public GameObject buildingTilePrefab;

    [Header("生成设置")]
    public int minBuildingTiles = 5;  // 最少建筑格子数量
    public int maxBuildingTiles = 15; // 最多建筑格子数量

    [Header("规模分布")]
    [Range(0, 1)] public float smallScaleProbability = 0.5f;   // 小建筑格子概率
    [Range(0, 1)] public float mediumScaleProbability = 0.3f;  // 中建筑格子概率
    [Range(0, 1)] public float largeScaleProbability = 0.2f;   // 大建筑格子概率

    [Header("位置生成")]
    public Vector2 mapBounds = new Vector2(20, 20); // 地图边界
    public float minDistanceBetweenTiles = 2f;      // 格子最小间距
    public float minDistanceFromRoad = 1f;         // 距离道路的最小距离

    private List<BoardTile> generatedBuildingTiles = new List<BoardTile>();

    void Start()
    {
        GenerateBuildingTiles();
    }

    public void GenerateBuildingTiles()
    {
        // 清除现有建筑格子
        ClearExistingTiles();

        // 随机生成数量
        int tileCount = Random.Range(minBuildingTiles, maxBuildingTiles + 1);

        for (int i = 0; i < tileCount; i++)
        {
            GenerateSingleBuildingTile(i);
        }

        Debug.Log($"生成 {tileCount} 个建筑格子");
    }

    private void GenerateSingleBuildingTile(int index)
    {
        // 生成随机位置（不依赖道路）
        Vector3 position = GenerateRandomPosition();

        // 确保位置不与其他格子重叠
        int attempts = 0;
        while (IsPositionOccupied(position) && attempts < 100)
        {
            position = GenerateRandomPosition();
            attempts++;
        }

        if (attempts >= 100)
        {
            Debug.LogWarning($"无法为建筑格子 {index} 找到合适位置");
            return;
        }

        // 实例化建筑格子
        GameObject tileObj = Instantiate(buildingTilePrefab, position, Quaternion.identity, transform);
        tileObj.name = $"BuildingTile_{index}";

        // 获取BoardTile组件
        BoardTile tile = tileObj.GetComponent<BoardTile>();
        if (tile == null)
        {
            Debug.LogError("建筑格子预制体缺少BoardTile组件");
            Destroy(tileObj);
            return;
        }

        // 随机分配规模
        int scale = DetermineTileScale();
        tile.tileScale = scale;
        tile.tileName = $"{GetScaleName(scale)}建筑地";
        tile.tileType = BoardTile.TileType.Buildable;
        tile.isBuildable = true;

        // 根据规模设置价格
        tile.propertyPrice = GetPriceByScale(scale);

        // 设置地块颜色（根据规模）
        SetTileColorByScale(tile, scale);

        // 添加到列表
        generatedBuildingTiles.Add(tile);

        // 注册到BoardManager
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.allTiles.Add(tile);
        }
    }

    private Vector3 GenerateRandomPosition()
    {
        float x = Random.Range(-mapBounds.x / 2, mapBounds.x / 2);
        float z = Random.Range(-mapBounds.y / 2, mapBounds.y / 2);

        return new Vector3(x, 0, z);
    }

    private bool IsPositionOccupied(Vector3 position)
    {
        foreach (BoardTile tile in generatedBuildingTiles)
        {
            if (Vector3.Distance(tile.transform.position, position) < minDistanceBetweenTiles)
            {
                return true;
            }
        }
        return false;
    }

    private int DetermineTileScale()
    {
        float randomValue = Random.Range(0f, 1f);

        if (randomValue < smallScaleProbability)
            return 1;
        else if (randomValue < smallScaleProbability + mediumScaleProbability)
            return 2;
        else
            return 3;
    }

    private string GetScaleName(int scale)
    {
        switch (scale)
        {
            case 1: return "小型";
            case 2: return "中型";
            case 3: return "大型";
            default: return "未知";
        }
    }

    private int GetPriceByScale(int scale)
    {
        switch (scale)
        {
            case 1: return Random.Range(100, 300);
            case 2: return Random.Range(300, 600);
            case 3: return Random.Range(600, 1000);
            default: return 100;
        }
    }

    private void SetTileColorByScale(BoardTile tile, int scale)
    {
        MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            switch (scale)
            {
                case 1: // 小 - 浅蓝色
                    renderer.material.color = new Color(0.7f, 0.9f, 1f);
                    break;
                case 2: // 中 - 浅绿色
                    renderer.material.color = new Color(0.7f, 1f, 0.7f);
                    break;
                case 3: // 大 - 浅黄色
                    renderer.material.color = new Color(1f, 1f, 0.7f);
                    break;
            }
        }
    }

    private void ClearExistingTiles()
    {
        foreach (BoardTile tile in generatedBuildingTiles)
        {
            if (tile != null)
            {
                if (BoardManager.Instance != null)
                {
                    BoardManager.Instance.allTiles.Remove(tile);
                }
                Destroy(tile.gameObject);
            }
        }
        generatedBuildingTiles.Clear();
    }
}