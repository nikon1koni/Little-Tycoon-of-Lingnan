// BuildingDataConfig.cs - 建筑数据配置
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BuildingDataConfig : MonoBehaviour
{
    [Header("所有建筑数据")]
    public List<BuildingData> allBuildingData = new List<BuildingData>();

    [Header("升级UI控制器")]
    public UpgradeUIController upgradeUIController;
    
    [Header("卖出UI控制器")]
    public SellBuildingUIController sellBuildingUIController;
    
    [Header("建筑选择面板控制器")]
    public BuildingSelectionPanelController buildingSelectionPanelController;
    
    [Header("卖出建筑配置")]
    [Tooltip("卖出建筑返还金额的比例 (0.0-1.0，默认0.5即50%)")]
    [Range(0.0f, 1.0f)]
    public float sellPriceRatio = 0.5f;

    private bool isUpgradeMode = false;
    private bool isSellMode = false;
    private Player upgradeModePlayer = null;
    private Player sellModePlayer = null;
    private List<BoardTile> upgradeModeTiles = new List<BoardTile>();
    private List<BoardTile> sellModeTiles = new List<BoardTile>();

    public static BuildingDataConfig Instance { get; private set; }

    public bool IsUpgradeModeActive()
    {
        return isUpgradeMode;
    }

    public bool IsSellModeActive()
    {
        return isSellMode;
    }

    public Player GetUpgradeModePlayer()
    {
        return upgradeModePlayer;
    }
    
    public Player GetSellModePlayer()
    {
        return sellModePlayer;
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
        upgradeModeTiles.Clear();
        
        // 为玩家的所有建筑添加点击事件
        if (BoardManager.Instance != null)
        {
            foreach (BoardTile tile in BoardManager.Instance.allTiles)
            {
                if (tile != null && tile.ownerPlayer == player && tile.currentBuildingData != null)
                {
                    AddUpgradeTileClickHandler(tile);
                    upgradeModeTiles.Add(tile);
                }
            }
        }
        
        if (upgradeUIController != null)
        {
            upgradeUIController.EnterUpgradeMode(player);
        }
        
        Debug.Log($"进入升级模式: {player.playerName}");
    }

    private void AddUpgradeTileClickHandler(BoardTile tile)
    {
        // 移除旧的EventTrigger
        EventTrigger oldTrigger = tile.GetComponent<EventTrigger>();
        if (oldTrigger != null)
        {
            Destroy(oldTrigger);
        }
        
        EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => OnTileClickedInUpgradeMode(tile));

        trigger.triggers.Add(entry);
    }

    private void RemoveUpgradeTileClickHandlers()
    {
        foreach (BoardTile tile in upgradeModeTiles)
        {
            if (tile != null)
            {
                EventTrigger trigger = tile.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    Destroy(trigger);
                }
            }
        }
        upgradeModeTiles.Clear();
    }

    public void ExitUpgradeMode()
    {
        if (!isUpgradeMode) return; // 防止重复调用
        
        isUpgradeMode = false;
        upgradeModePlayer = null;
        
        // 移除点击事件
        RemoveUpgradeTileClickHandlers();
        
        if (upgradeUIController != null)
        {
            upgradeUIController.ExitUpgradeMode();
        }
        
        // 通知建筑选择面板控制器返回建筑选择面板
        if (buildingSelectionPanelController != null)
        {
            buildingSelectionPanelController.OnExitUpgradeMode();
        }
        
        Debug.Log("退出升级模式");
    }

    public void OnTileClickedInUpgradeMode(BoardTile tile)
    {
        if (!isUpgradeMode || upgradeUIController == null) return;
        
        Debug.Log($"升级模式下点击了地块: {tile.tileName}");
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
    
    public void EnterSellMode(Player player)
    {
        if (player == null) return;
        
        isSellMode = true;
        sellModePlayer = player;
        sellModeTiles.Clear();
        
        if (BoardManager.Instance != null)
        {
            foreach (BoardTile tile in BoardManager.Instance.allTiles)
            {
                if (tile != null && tile.ownerPlayer == player && tile.currentBuildingData != null)
                {
                    AddSellTileClickHandler(tile);
                    sellModeTiles.Add(tile);
                }
            }
        }
        
        if (sellBuildingUIController != null)
        {
            sellBuildingUIController.EnterSellMode(player);
        }
        
        Debug.Log($"进入卖出模式: {player.playerName}");
    }
    
    private void AddSellTileClickHandler(BoardTile tile)
    {
        EventTrigger oldTrigger = tile.GetComponent<EventTrigger>();
        if (oldTrigger != null)
        {
            Destroy(oldTrigger);
        }
        
        EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => OnTileClickedInSellMode(tile));
        
        trigger.triggers.Add(entry);
    }
    
    private void RemoveSellTileClickHandlers()
    {
        foreach (BoardTile tile in sellModeTiles)
        {
            if (tile != null)
            {
                EventTrigger trigger = tile.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    Destroy(trigger);
                }
            }
        }
        sellModeTiles.Clear();
    }
    
    public void ExitSellMode()
    {
        if (!isSellMode) return;
        
        isSellMode = false;
        sellModePlayer = null;
        
        RemoveSellTileClickHandlers();
        
        if (sellBuildingUIController != null)
        {
            sellBuildingUIController.ExitSellMode();
        }
        
        if (buildingSelectionPanelController != null)
        {
            buildingSelectionPanelController.OnExitSellMode();
        }
        
        Debug.Log("退出卖出模式");
    }
    
    public void OnTileClickedInSellMode(BoardTile tile)
    {
        if (!isSellMode || sellBuildingUIController == null) return;
        
        Debug.Log($"卖出模式下点击了地块: {tile.tileName}");
        sellBuildingUIController.OnTileClicked(tile);
    }
    
    public List<BoardTile> GetPlayerSellableBuildings(Player player)
    {
        List<BoardTile> sellableBuildings = new List<BoardTile>();
        
        if (player == null || BoardManager.Instance == null)
            return sellableBuildings;
        
        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            if (tile != null && 
                tile.ownerPlayer == player && 
                tile.currentBuildingData != null)
            {
                sellableBuildings.Add(tile);
            }
        }
        
        return sellableBuildings;
    }
    
    /// <summary>
    /// 设置卖出建筑返还金额的比例
    /// </summary>
    /// <param name="ratio">比例值 (0.0-1.0)</param>
    public void SetSellPriceRatio(float ratio)
    {
        sellPriceRatio = Mathf.Clamp01(ratio);
        Debug.Log($"卖出建筑比例已设置为: {sellPriceRatio * 100f}%");
    }
    
    /// <summary>
    /// 获取当前卖出建筑返还金额的比例
    /// </summary>
    public float GetSellPriceRatio()
    {
        return sellPriceRatio;
    }
}
