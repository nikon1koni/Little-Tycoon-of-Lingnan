// BuildingDataConfig.cs - ??????????????????
using UnityEngine;
using System.Collections.Generic;

public class BuildingDataConfig : MonoBehaviour
{
    [Header("????????????????")]
    public List<BuildingData> allBuildingData = new List<BuildingData>();

    [Header("????UI????")]
    public UpgradeUIController upgradeUIController;

    private bool isUpgradeMode = false;
    private Player upgradeModePlayer = null;

    public static BuildingDataConfig Instance { get; private set; }

    public bool IsUpgradeModeActive()
    {
        return isUpgradeMode;
    }

    public Player GetUpgradeModePlayer()
    {
        return upgradeModePlayer;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public BuildingData GetBuildingByType(BoardTile.BuildingType type)
    {
        foreach (var building in allBuildingData)
        {
            if (building.buildingType == type)
            {
                return building;
            }
        }
        return null;
    }

    public BuildingData GetNextLevel(BuildingData current)
    {
        return current?.nextLevelBuilding;
    }

    public bool CanUpgrade(BuildingData current)
    {
        return current != null && !current.isFinalLevel && current.nextLevelBuilding != null;
    }

    public List<BuildingData> GetBuildingsByScale(int tileScale)
    {
        List<BuildingData> result = new List<BuildingData>();
        
        foreach (var building in allBuildingData)
        {
            if (building.minTileScale <= tileScale && building.maxTileScale >= tileScale)
            {
                result.Add(building);
            }
        }
        
        return result;
    }

    public void EnterUpgradeMode(Player player)
    {
        if (player == null) return;
        
        isUpgradeMode = true;
        upgradeModePlayer = player;
        
        if (upgradeUIController != null)
        {
            upgradeUIController.EnterUpgradeMode(player);
        }
        
        Debug.Log($"??????????: {player.playerName}");
    }

    public void ExitUpgradeMode()
    {
        isUpgradeMode = false;
        upgradeModePlayer = null;
        
        if (upgradeUIController != null)
        {
            upgradeUIController.ExitUpgradeMode();
        }
        
        Debug.Log("?????????");
    }

    public void OnTileClickedInUpgradeMode(BoardTile tile)
    {
        if (!isUpgradeMode || upgradeUIController == null) return;
        
        upgradeUIController.OnTileClicked(tile);
    }

    public List<BoardTile> GetPlayerUpgradeableBuildings(Player player)
    {
        List<BoardTile> upgradeableBuildings = new List<BoardTile>();
        
        if (player == null || BoardManager.Instance == null) 
            return upgradeableBuildings;
        
        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            if (tile != null && 
                tile.ownerPlayer == player && 
                tile.currentBuildingData != null && 
                CanUpgrade(tile.currentBuildingData))
            {
                upgradeableBuildings.Add(tile);
            }
        }
        
        return upgradeableBuildings;
    }
}
