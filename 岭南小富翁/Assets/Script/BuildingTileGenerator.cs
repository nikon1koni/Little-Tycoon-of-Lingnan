using System.Collections.Generic;
using UnityEngine;

public class BuildingTileGenerator : MonoBehaviour
{
    [Header("生成设置")]
    public int buildingTileCount = 5; // 要生成的建筑地块数量
    public List<Vector2> spawnPositions = new List<Vector2>(); // 生成位置
    public GameObject buildingTilePrefab; // 建筑地块预制体

    [Header("地块属性")]
    [Range(1, 4)] public int minTileScale = 1;
    [Range(1, 4)] public int maxTileScale = 4;
    [Range(50, 1000)] public int minPrice = 100;
    [Range(50, 1000)] public int maxPrice = 500;

    [Header("关联设置")]
    public bool enableTileLinking = true; // 是否启用地块关联
    [Range(1, 5)] public int maxLinkedTilesPerProperty = 3; // 每个属性地块最大关联数量

    private List<BoardTile> generatedBuildingTiles = new List<BoardTile>();
    private List<BoardTile> allPropertyTiles = new List<BoardTile>();

    void Start()
    {
        GenerateBuildingTiles();

        if (enableTileLinking)
        {
            LinkTilesToProperties();
        }
    }

    // 生成建筑地块
    void GenerateBuildingTiles()
    {
        Debug.Log($"开始生成 {buildingTileCount} 个建筑地块");

        if (buildingTilePrefab == null)
        {
            Debug.LogError("建筑地块预制体未设置");
            return;
        }

        // 获取所有属性地块
        FindAllPropertyTiles();

        // 生成建筑地块
        for (int i = 0; i < buildingTileCount; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition(i);

            GameObject tileObj = Instantiate(buildingTilePrefab, spawnPosition, Quaternion.identity);
            tileObj.name = $"BuildingTile_{i}";

            BoardTile tile = tileObj.GetComponent<BoardTile>();
            if (tile != null)
            {
                SetupBuildingTile(tile, i);
                generatedBuildingTiles.Add(tile);
            }
        }

        Debug.Log($"成功生成 {generatedBuildingTiles.Count} 个建筑地块");
    }

    // 获取生成位置
    Vector3 GetSpawnPosition(int index)
    {
        if (spawnPositions != null && index < spawnPositions.Count)
        {
            Vector2 pos = spawnPositions[index];
            return new Vector3(pos.x, 0, pos.y);
        }
        else
        {
            // 随机生成位置
            float x = Random.Range(-10f, 10f);
            float z = Random.Range(-10f, 10f);
            return new Vector3(x, 0, z);
        }
    }

    // 设置建筑地块属性
    void SetupBuildingTile(BoardTile tile, int index)
    {
        tile.tileName = $"建筑地块_{index}";
        tile.tileID = 1000 + index; // 给一个高ID避免冲突
        tile.tileScale = Random.Range(minTileScale, maxTileScale + 1);
        tile.propertyPrice = Random.Range(minPrice, maxPrice + 1);
        tile.tileType = BoardTile.TileType.Buildable;
        tile.isBuildable = true;

        // 设置租金
        tile.rentPrice = tile.propertyPrice / 10;

        Debug.Log($"生成建筑地块: {tile.tileName}, 规模: {tile.tileScale}, 价格: {tile.propertyPrice}");
    }

    // 查找所有属性地块
    void FindAllPropertyTiles()
    {
        BoardTile[] allTiles = FindObjectsOfType<BoardTile>();
        allPropertyTiles.Clear();

        foreach (BoardTile tile in allTiles)
        {
            if (tile.tileType == BoardTile.TileType.Property)
            {
                allPropertyTiles.Add(tile);
            }
        }

        Debug.Log($"找到 {allPropertyTiles.Count} 个属性地块");
    }

    // 将建筑地块关联到属性地块
    void LinkTilesToProperties()
    {
        if (allPropertyTiles.Count == 0 || generatedBuildingTiles.Count == 0)
        {
            Debug.LogWarning("没有足够的属性地块或建筑地块进行关联");
            return;
        }

        int linksCreated = 0;

        // 为每个属性地块随机关联一些建筑地块
        foreach (BoardTile propertyTile in allPropertyTiles)
        {
            int linksToCreate = Random.Range(1, Mathf.Min(maxLinkedTilesPerProperty, generatedBuildingTiles.Count) + 1);

            List<BoardTile> availableBuildingTiles = new List<BoardTile>(generatedBuildingTiles);

            for (int i = 0; i < linksToCreate; i++)
            {
                if (availableBuildingTiles.Count == 0) break;

                int randomIndex = Random.Range(0, availableBuildingTiles.Count);
                BoardTile buildingTile = availableBuildingTiles[randomIndex];

                // 建立关联
                propertyTile.AddLinkedBuildingTile(buildingTile);
                Debug.Log($"关联: 属性地块 {propertyTile.tileName} -> 建筑地块 {buildingTile.tileName}");

                availableBuildingTiles.RemoveAt(randomIndex);
                linksCreated++;
            }
        }

        Debug.Log($"成功创建 {linksCreated} 个地块关联");
    }

    // 手动关联两个地块
    public void LinkTilesManually(BoardTile propertyTile, BoardTile buildingTile)
    {
        if (propertyTile == null || buildingTile == null)
        {
            Debug.LogError("无法关联，地块为空");
            return;
        }

        propertyTile.AddLinkedBuildingTile(buildingTile);
        Debug.Log($"手动关联: {propertyTile.tileName} -> {buildingTile.tileName}");
    }

    // 获取所有生成的建筑地块
    public List<BoardTile> GetGeneratedBuildingTiles()
    {
        return generatedBuildingTiles;
    }

    // 获取所有属性地块
    public List<BoardTile> GetAllPropertyTiles()
    {
        return allPropertyTiles;
    }
}