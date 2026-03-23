using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BoardTile : MonoBehaviour
{
    [Header("地块信息")]
    public int tileID;
    public string tileName = "未命名";
    public TileType tileType = TileType.Property;

    [Header("地产信息 (如果适用)")]
    public int propertyPrice = 100;
    public int baseRent = 20;
    public Color propertyColor = Color.white;
    [HideInInspector] public Player ownerPlayer;  // 改为存储Player而非GameObject

    [Header("事件")]
    public UnityEvent onPlayerLanded;

    [Header("建筑 (可建造地块)")]
    public bool isBuildable = false;
    public GameObject currentBuilding = null;  // 当前建筑
    public List<GameObject> availableBuildings = new List<GameObject>(); // 可选的建筑预制体

    public enum TileType
    {
        Start,
        Property,
        Railroad,
        Utility,
        Event,
        CommunityChest,
        Tax,
        Jail,
        GoToJail,
        FreeParking,
        Normal,
        Buildable,         //可建筑的地块，类似地产
        BuildingSite       //已放置建筑的地块
    }

    void Start()
    {
        //UpdateTileVisual(); //根据类型着色
    }

    void UpdateTileVisual()
    {
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            switch (tileType)
            {
                case TileType.Start:
                    renderer.material.color = Color.green;
                    break;
                case TileType.Property:
                    renderer.material.color = propertyColor;
                    break;
                case TileType.Event:
                    renderer.material.color = Color.yellow;
                    break;
                case TileType.CommunityChest:
                    renderer.material.color = new Color(0.5f, 0.8f, 1f); // 淡蓝色
                    break;
                case TileType.Tax:
                    renderer.material.color = Color.red;
                    break;
                case TileType.Jail:
                    renderer.material.color = new Color(0.3f, 0.3f, 0.3f); // 深灰色
                    break;
                case TileType.GoToJail:
                    renderer.material.color = new Color(0.5f, 0f, 0f); // 暗红色
                    break;
                case TileType.FreeParking:
                    renderer.material.color = Color.white;
                    break;
                case TileType.Normal:
                    renderer.material.color = Color.gray;  // 设为灰色以示普通
                    break;
                default:
                    renderer.material.color = Color.white;
                    break;
            }
        }
    }

    // 玩家落地时调用
    public void OnLanded(Player player)
    {
        if (player == null)
        {
            Debug.LogError("Player为null");
            return;
        }

        Debug.Log($"玩家 {player.playerName} 落在 {tileName} 上 (类型: {tileType})");

        // UnityEvent可在Inspector面板绑定
        onPlayerLanded?.Invoke();

        // 根据类型执行不同逻辑
        switch (tileType)
        {
            case TileType.Property:
            case TileType.Railroad:
            case TileType.Utility:
                HandlePropertyLanding(player);
                break;

            case TileType.Event:
                DrawChanceCard(player);
                break;

            case TileType.CommunityChest:
                DrawCommunityChestCard(player);
                break;

            case TileType.Tax:
                HandleTaxTile(player);
                break;

            case TileType.GoToJail:
                SendToJail(player);
                break;

            case TileType.Start:
                HandleStartTile(player);
                break;

            case TileType.FreeParking:
                HandleFreeParking(player);
                break;
            case TileType.Normal:  //普通地块不执行任何操作
                Debug.Log($"{player.playerName} 停留在普通地块 [{tileName}]");
                break;
            case TileType.Buildable:  //可建筑地块
                HandleBuildableTileLanding(player);
                break;
        }
    }
    void HandleBuildableTileLanding(Player player)
    {
        Debug.Log($"{player.playerName} 停在可建筑地块 [{tileName}]");

        if (ownerPlayer == null)
        {
            // 1. 无主地块 -> 购买/建筑
            Debug.Log($"可建筑地块 [{tileName}] 可供购买，价格: {propertyPrice} 元");

            // 延迟显示选择UI以避免视觉冲突
            StartCoroutine(DelayedBuildingSelection(player));
        }
        else if (ownerPlayer == player)
        {
            Debug.Log($"玩家 {player.playerName} 拥有此可建筑地块 [{tileName}]");
        }
        else
        {
            Debug.Log($"{player.playerName} 踩在 {ownerPlayer.playerName} 拥有的建筑地块 [{tileName}]");
        }
    }

    // 延迟显示建筑选择
    IEnumerator DelayedBuildingSelection(Player player)
    {
        // 等待一小段时间让骰子动画等完成
        yield return new WaitForSeconds(0.5f);

        // 显示建筑选择UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBuildingSelectionUI(this, player);
        }
    }
    void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // 无主财产
            Debug.Log($"此片地产 [{tileName}] 可供购买。价格: {propertyPrice} 元");

            // 调用UI显示购买面板
            // UIManager.Instance.ShowPurchasePanel(this, player);
        }
        else if (ownerPlayer == player)
        {
            // 玩家自己的地产
            Debug.Log($"玩家 {player.playerName} 拥有自己的地产 [{tileName}]");
        }

    }

    void DrawChanceCard(Player player)
    {
        Debug.Log($"{player.playerName} 抽取机会卡");
        // 实现抽机会卡逻辑
        // ChanceCardManager.Instance.DrawChanceCard(player);
    }

    void DrawCommunityChestCard(Player player)
    {
        Debug.Log($"{player.playerName} 抽取公益金卡");
        // 实现抽卡逻辑
        // ChanceCardManager.Instance.DrawCommunityChestCard(player);
    }

    void HandleTaxTile(Player player)
    {
        int taxAmount = 200; // 示例税额
        Debug.Log($"{player.playerName} 需要缴税 {taxAmount} 元");

        if (player.PayCash(taxAmount))
        {
            Debug.Log("缴税成功");
        }
        else
        {
            Debug.LogWarning("缴税失败，资金不足");
        }
    }

    void SendToJail(Player player)
    {
        Debug.Log($"{player.playerName} 被送进监狱");
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        // 移动到监狱地块
        BoardTile jailTile = FindJailTile();
        if (jailTile != null)
        {
            player.MoveToTile(jailTile, true);
        }
    }

    BoardTile FindJailTile()
    {
        if (BoardManager.Instance == null) return null;

        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            if (tile.tileType == TileType.Jail)
            {
                return tile;
            }
        }
        return null;
    }

    void HandleStartTile(Player player)
    {
        int salary = 200; // 固定薪水
        Debug.Log($"{player.playerName} 落在起点，获得薪水 {salary} 元");
        player.ReceiveCash(salary);

        // === 新增关键逻辑：落在起点时，也触发购买流程 ===
        // 这里我们让GameManager统一处理购买逻辑，以保持一致性
        if (GameManager.Instance != null)
        {
            // 延迟一帧调用，确保其他逻辑先完成
            GameManager.Instance.StartCoroutine(DelayedStartTilePurchase(player));
        }
    }

    // === 新增：延迟触发起点购买 ===
    IEnumerator DelayedStartTilePurchase(Player player)
    {
        yield return new WaitForSeconds(0.5f); // 短暂延迟，让玩家看到薪水信息

        // 切换到购买状态
        GameManager.Instance.currentState = GameManager.GameState.BuildingSelection;
        GameManager.Instance.isPlayerTurn = false;

        // 显示购买界面
        if (UIManager.Instance != null)
        {
            // 创建虚拟的起点商店Tile
            BoardTile startShopTile = CreateStartShopTile();
            UIManager.Instance.ShowBuildingSelectionUI(startShopTile, player);
        }
    }

    // === 新增：创建起点商店Tile ===
    BoardTile CreateStartShopTile()
    {
        GameObject tempObj = new GameObject("StartShopTile_FromBoardTile");
        BoardTile tile = tempObj.AddComponent<BoardTile>();
        tile.tileName = "起点商店";
        tile.tileType = TileType.Buildable;
        tile.propertyPrice = 100;
        tile.isBuildable = true;
        return tile;
    }

    void HandleFreeParking(Player player)
    {
        Debug.Log($"{player.playerName} 免费停车场，休息一回合");
        // 实现免费停车场累积奖金逻辑
    }
}