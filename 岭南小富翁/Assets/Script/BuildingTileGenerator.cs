using System.Collections.Generic;
using UnityEngine;

public class BuildingTileGenerator : MonoBehaviour
{
    [Header("��������")]
    public int buildingTileCount = 5; // Ҫ���ɵĽ����ؿ�����
    public List<Vector2> spawnPositions = new List<Vector2>(); // ����λ��
    public GameObject buildingTilePrefab; // �����ؿ�Ԥ����

    [Header("�ؿ�����")]
    [Range(1, 4)] public int minTileScale = 1;
    [Range(1, 4)] public int maxTileScale = 4;
    [Range(50, 1000)] public int minPrice = 100;
    [Range(50, 1000)] public int maxPrice = 500;

    [Header("��������")]
    public bool enableTileLinking = true; // �Ƿ����õؿ����
    [Range(1, 5)] public int maxLinkedTilesPerProperty = 3; // ÿ�����Եؿ�����������

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

    // ���ɽ����ؿ�
    void GenerateBuildingTiles()
    {
        Debug.Log($"��ʼ���� {buildingTileCount} �������ؿ�");

        if (buildingTilePrefab == null)
        {
            Debug.LogError("�����ؿ�Ԥ����δ����");
            return;
        }

        // ��ȡ�������Եؿ�
        FindAllPropertyTiles();

        // ���ɽ����ؿ�
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

        Debug.Log($"�ɹ����� {generatedBuildingTiles.Count} �������ؿ�");
    }

    // ��ȡ����λ��
    Vector3 GetSpawnPosition(int index)
    {
        if (spawnPositions != null && index < spawnPositions.Count)
        {
            Vector2 pos = spawnPositions[index];
            return new Vector3(pos.x, 0, pos.y);
        }
        else
        {
            // �������λ��
            float x = Random.Range(-10f, 10f);
            float z = Random.Range(-10f, 10f);
            return new Vector3(x, 0, z);
        }
    }

    // ���ý����ؿ�����
    void SetupBuildingTile(BoardTile tile, int index)
    {
        tile.tileName = $"�����ؿ�_{index}";
        tile.tileID = 1000 + index; // ��һ����ID�����ͻ
        tile.tileScale = Random.Range(minTileScale, maxTileScale + 1);
        tile.propertyPrice = Random.Range(minPrice, maxPrice + 1);
        tile.tileType = BoardTile.TileType.Buildable;
        tile.isBuildable = true;

        // �������
        tile.rentPrice = tile.propertyPrice / 10;

        Debug.Log($"���ɽ����ؿ�: {tile.tileName}, ��ģ: {tile.tileScale}, �۸�: {tile.propertyPrice}");
    }

    // �����������Եؿ�
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

        Debug.Log($"�ҵ� {allPropertyTiles.Count} �����Եؿ�");
    }

    // �������ؿ���������Եؿ�
    void LinkTilesToProperties()
    {
        if (allPropertyTiles.Count == 0 || generatedBuildingTiles.Count == 0)
        {
            Debug.LogWarning("û���㹻�����Եؿ�����ؿ���й���");
            return;
        }

        int linksCreated = 0;

        // Ϊÿ�����Եؿ��������һЩ�����ؿ�
        foreach (BoardTile propertyTile in allPropertyTiles)
        {
            int linksToCreate = Random.Range(1, Mathf.Min(maxLinkedTilesPerProperty, generatedBuildingTiles.Count) + 1);

            List<BoardTile> availableBuildingTiles = new List<BoardTile>(generatedBuildingTiles);

            for (int i = 0; i < linksToCreate; i++)
            {
                if (availableBuildingTiles.Count == 0) break;

                int randomIndex = Random.Range(0, availableBuildingTiles.Count);
                BoardTile buildingTile = availableBuildingTiles[randomIndex];

                // ��������
                propertyTile.AddLinkedBuildingTile(buildingTile);
                Debug.Log($"����: ���Եؿ� {propertyTile.tileName} -> �����ؿ� {buildingTile.tileName}");

                availableBuildingTiles.RemoveAt(randomIndex);
                linksCreated++;
            }
        }

        Debug.Log($"�ɹ����� {linksCreated} ���ؿ����");
    }

    // �ֶ����������ؿ�
    public void LinkTilesManually(BoardTile propertyTile, BoardTile buildingTile)
    {
        if (propertyTile == null || buildingTile == null)
        {
            Debug.LogError("�޷��������ؿ�Ϊ��");
            return;
        }

        propertyTile.AddLinkedBuildingTile(buildingTile);
        Debug.Log($"�ֶ�����: {propertyTile.tileName} -> {buildingTile.tileName}");
    }

    // ��ȡ�������ɵĽ����ؿ�
    public List<BoardTile> GetGeneratedBuildingTiles()
    {
        return generatedBuildingTiles;
    }

    // ��ȡ�������Եؿ�
    public List<BoardTile> GetAllPropertyTiles()
    {
        return allPropertyTiles;
    }
}