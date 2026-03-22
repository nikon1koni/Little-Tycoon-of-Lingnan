using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BoardTile : MonoBehaviour
{
    [Header("Tile Information")]
    public int tileID;
    public string tileName = "未命名格子";
    public TileType tileType = TileType.Property;

    [Header("Property Info (If applicable)")]
    public int propertyPrice = 100;
    public int baseRent = 20;
    public Color propertyColor = Color.white;
    [HideInInspector] public Player ownerPlayer;  // 改为存储Player引用而非GameObject

    [Header("Events")]
    public UnityEvent onPlayerLanded;

    [Header("建筑属性 (如果是可建筑格子)")]
    public bool isBuildable = false;
    public GameObject currentBuilding = null;  // 当前建筑对象
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
        Buildable,         //可建筑的格子（非道路）
        BuildingSite       //已放置建筑的地块

    }

    void Start()
    {
        //UpdateTileVisual(); //自动更新颜色
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
                    renderer.material.color = new Color(0.5f, 0f, 0f); // 深红色
                    break;
                case TileType.FreeParking:
                    renderer.material.color = Color.white;
                    break;
                case TileType.Normal:
                    renderer.material.color = Color.gray;  // 设置为灰色，表示普通格子
                    break;
                default:
                    renderer.material.color = Color.white;
                    break;
            }
        }
    }

    // 当玩家落在格子上时调用
    public void OnLanded(Player player)
    {
        if (player == null)
        {
            Debug.LogError("传入的Player为null！");
            return;
        }

        Debug.Log($"玩家 {player.playerName} 落在了 {tileName} 上 (类型: {tileType})");

        // 触发UnityEvent事件（可以在Inspector中配置）
        onPlayerLanded?.Invoke();

        // 根据格子类型执行不同逻辑
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
            case TileType.Normal:  //普通格子不执行任何功能
                Debug.Log($"{player.playerName} 经过普通格子 [{tileName}]，无特殊事件");
                break;
            case TileType.Buildable:  //可建筑格子
                HandleBuildableTileLanding(player);
                break;

        }
    }
    void HandleBuildableTileLanding(Player player)
    {
        Debug.Log($"{player.playerName} 抵达了可建造地块 [{tileName}]");

        if (ownerPlayer == null)
        {
            // 情况1：无主地块 -> 触发购买/建造流程
            Debug.Log($"可建造地块 [{tileName}] 无主，购买价格: {propertyPrice} 元");

            // 延迟显示建筑选择UI，给玩家一个反应时间
            StartCoroutine(DelayedBuildingSelection(player));
        }
        else if (ownerPlayer == player)
        {
            Debug.Log($"这是 {player.playerName} 自己拥有的可建造地块 [{tileName}]。");
        }
        else
        {
            Debug.Log($"{player.playerName} 经过了 {ownerPlayer.playerName} 拥有的建筑地块 [{tileName}]。");
        }
    }

    // 延迟显示建筑选择
    IEnumerator DelayedBuildingSelection(Player player)
    {
        // 等待一小段时间让动画完成
        yield return new WaitForSeconds(0.5f);

        // 显示建筑选择界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBuildingSelectionUI(this, player);
        }
    }
    void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // 地产无主
            Debug.Log($"这片地产 [{tileName}] 可以购买。价格: {propertyPrice} 元");

            // 触发UI购买界面
            // UIManager.Instance.ShowPurchasePanel(this, player);
        }
        else if (ownerPlayer == player)
        {
            // 自己的地产
            Debug.Log($"这是 {player.playerName} 自己的地产 [{tileName}]。");
        }

    }

    void DrawChanceCard(Player player)
    {
        Debug.Log($"{player.playerName} 抽取机会卡！");
        // 实现机会卡逻辑
        // ChanceCardManager.Instance.DrawChanceCard(player);
    }

    void DrawCommunityChestCard(Player player)
    {
        Debug.Log($"{player.playerName} 抽取公益金卡！");
        // 实现公益金卡逻辑
        // ChanceCardManager.Instance.DrawCommunityChestCard(player);
    }

    void HandleTaxTile(Player player)
    {
        int taxAmount = 200; // 可以设为变量
        Debug.Log($"{player.playerName} 需要缴税 {taxAmount} 元");

        if (player.PayCash(taxAmount))
        {
            Debug.Log("缴税成功！");
        }
        else
        {
            Debug.LogWarning("缴税失败！");
        }
    }

    void SendToJail(Player player)
    {
        Debug.Log($"{player.playerName} 被送进了监狱！");
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        // 找到监狱格子
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
        int salary = 200; // 经过起点的薪水
        Debug.Log($"{player.playerName} 经过起点，获得薪水 {salary} 元！");
        player.ReceiveCash(salary);
    }

    void HandleFreeParking(Player player)
    {
        Debug.Log($"{player.playerName} 停在免费停车场，休息一下！");
        // 可以在这里实现免费停车场累积奖金逻辑
    }
}