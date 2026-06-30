using System.Collections.Generic;
using UnityEngine;

public class BuildingTileGenerator : MonoBehaviour
{
    [Header("生成参数")]
    public int buildingTileCount = 5;
    public List<Vector2> spawnPositions = new List<Vector2>();
    public GameObject buildingTilePrefab;

    [Header("地块参数")]
    [Range(1, 4)] public int minTileScale = 1;
    [Range(1, 4)] public int maxTileScale = 4;
    [Range(50, 1000)] public int minPrice = 100;
    [Range(50, 1000)] public int maxPrice = 500;

    [Header("链接系统")]
    public bool enableTileLinking = true;
    [Range(1, 5)] public int maxLinkedTilesPerProperty = 3;

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

    void GenerateBuildingTiles()
    {
        Debug.Log($"开始生成 {buildingTileCount} 个建筑地块");

        if (buildingTilePrefab == null)
        {
            Debug.LogError("建筑地块预制体未设置");
            return;
        }

        FindAllPropertyTiles();

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

    Vector3 GetSpawnPosition(int index)
    {
        if (spawnPositions != null && index < spawnPositions.Count)
        {
            Vector2 pos = spawnPositions[index];
            return new Vector3(pos.x, 0, pos.y);
        }
        else
        {
            float x = Random.Range(-10f, 10f);
            float z = Random.Range(-10f, 10f);
            return new Vector3(x, 0, z);
        }
    }

    void SetupBuildingTile(BoardTile tile, int index)
    {
        tile.tileName = $"建筑地块_{index}";
        tile.tileID = 1000 + index;
        tile.tileScale = Random.Range(minTileScale, maxTileScale + 1);
        tile.propertyPrice = Random.Range(minPrice, maxPrice + 1);
        tile.tileType = BoardTile.TileType.Buildable;
        tile.isBuildable = true;
        tile.rentPrice = tile.propertyPrice / 10;
    }

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

        Debug.Log($"找到 {allPropertyTiles.Count} 个地产地块");
    }

    void LinkTilesToProperties()
    {
        if (allPropertyTiles.Count == 0 || generatedBuildingTiles.Count == 0)
        {
            Debug.LogWarning("没有足够的地产或建筑地块进行链接");
            return;
        }

        int linksCreated = 0;

        foreach (BoardTile propertyTile in allPropertyTiles)
        {
            int linksToCreate = Random.Range(1, Mathf.Min(maxLinkedTilesPerProperty, generatedBuildingTiles.Count) + 1);

            List<BoardTile> availableBuildingTiles = new List<BoardTile>(generatedBuildingTiles);

            for (int i = 0; i < linksToCreate; i++)
            {
                if (availableBuildingTiles.Count == 0) break;

                int randomIndex = Random.Range(0, availableBuildingTiles.Count);
                BoardTile buildingTile = availableBuildingTiles[randomIndex];

                propertyTile.AddLinkedBuildingTile(buildingTile);
                Debug.Log($"链接: 地产地块 {propertyTile.tileName} -> 建筑地块 {buildingTile.tileName}");

                availableBuildingTiles.RemoveAt(randomIndex);
                linksCreated++;
            }
        }

        Debug.Log($"成功创建 {linksCreated} 个地块链接");
    }

    public void LinkTilesManually(BoardTile propertyTile, BoardTile buildingTile)
    {
        if (propertyTile == null || buildingTile == null)
        {
            Debug.LogError("无法链接空地块");
            return;
        }

        propertyTile.AddLinkedBuildingTile(buildingTile);
        Debug.Log($"手动链接: {propertyTile.tileName} -> {buildingTile.tileName}");
    }

    public List<BoardTile> GetGeneratedBuildingTiles()
    {
        return generatedBuildingTiles;
    }

    public List<BoardTile> GetAllPropertyTiles()
    {
        return allPropertyTiles;
    }
}
