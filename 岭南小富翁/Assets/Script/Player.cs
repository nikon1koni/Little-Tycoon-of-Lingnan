using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("玩家信息")]
    public string playerName = "玩家1";
    public int playerID = 1;
    public Color playerColor = Color.red;

    [Header("资产信息")]
    public int cash = 1500;  // 初始现金
    public List<BoardTile> ownedProperties = new List<BoardTile>();  // 拥有的地产

    [Header("状态信息")]
    public bool isInJail = false;
    public int jailTurnsRemaining = 0;
    public bool isBankrupt = false;

    [Header("位置信息")]
    [HideInInspector] public BoardTile currentTile;  // 当前所在格子
    [HideInInspector] public int currentTileIndex = 0;  // 当前格子索引

    // 引用组件
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning($"Player {playerName} 缺少 PlayerMovement 组件！");
        }

        // 设置玩家颜色
        SetPlayerColor();
    }

    void SetPlayerColor()
    {
        // 设置玩家棋子的颜色
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }
    }

    // 支付金钱
    public bool PayCash(int amount)
    {
        if (cash >= amount)
        {
            cash -= amount;
            Debug.Log($"{playerName} 支付了 {amount} 元，剩余现金: {cash}");
            return true;
        }
        else
        {
            Debug.LogWarning($"{playerName} 资金不足！需要 {amount} 元，但只有 {cash} 元");
            return false;
        }
    }

    // 获得金钱
    public void ReceiveCash(int amount)
    {
        cash += amount;
        Debug.Log($"{playerName} 获得了 {amount} 元，现在有: {cash} 元");
    }

    // 购买地产
    public bool BuyProperty(BoardTile property)
    {
        if (property == null) return false;

        if (property.tileType != BoardTile.TileType.Property &&
            property.tileType != BoardTile.TileType.Railroad &&
            property.tileType != BoardTile.TileType.Utility)
        {
            Debug.LogWarning($"无法购买 {property.tileName}，这不是可购买的地产类型！");
            return false;
        }

        if (property.ownerPlayer != null)
        {
            Debug.LogWarning($"{property.tileName} 已经有主人了！");
            return false;
        }

        if (PayCash(property.propertyPrice))
        {
            property.ownerPlayer = this;
            ownedProperties.Add(property);
            Debug.Log($"{playerName} 成功购买了 {property.tileName}！");
            return true;
        }

        return false;
    }

    // 支付租金
    public bool PayRent(int rentAmount, GameObject owner)
    {
        if (PayCash(rentAmount))
        {
            // 找到房主，把钱给他
            Player ownerPlayer = owner.GetComponent<Player>();
            if (ownerPlayer != null)
            {
                ownerPlayer.ReceiveCash(rentAmount);
            }
            return true;
        }
        return false;
    }

    // 移动到指定格子
    public void MoveToTile(BoardTile tile, bool teleport = false)
    {
        if (tile == null) return;

        if (playerMovement != null && !teleport)
        {
            // 计算步数
            int steps = GetStepsToTile(tile);
            if (steps > 0)
            {
                playerMovement.MoveSteps(steps);
            }
        }
        else
        {
            // 瞬移（如监狱、机会卡效果）
            transform.position = tile.transform.position + Vector3.up * 0.5f;
            currentTile = tile;
            currentTileIndex = BoardManager.Instance?.allTiles.IndexOf(tile) ?? 0;

            // 触发格子事件
            tile.OnLanded(this);
        }
    }

    // 计算到目标格子的步数
    private int GetStepsToTile(BoardTile targetTile)
    {
        if (BoardManager.Instance == null || currentTile == null || targetTile == null)
            return 0;

        List<BoardTile> allTiles = BoardManager.Instance.allTiles;
        int currentIndex = allTiles.IndexOf(currentTile);
        int targetIndex = allTiles.IndexOf(targetTile);

        if (currentIndex == -1 || targetIndex == -1)
            return 0;

        if (targetIndex <= currentIndex)
        {
            // 需要经过起点
            return (allTiles.Count - currentIndex) + targetIndex;
        }
        else
        {
            return targetIndex - currentIndex;
        }
    }

    // 检查是否破产
    public bool CheckBankruptcy()
    {
        if (cash < 0)
        {
            isBankrupt = true;
            Debug.Log($"{playerName} 破产了！");
            return true;
        }
        return false;
    }
}