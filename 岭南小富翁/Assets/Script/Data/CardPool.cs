﻿using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCardPool", menuName = "Game/Card Pool")]
public class CardPool : ScriptableObject
{
    [System.Serializable]
    public class RarityWeight
    {
        public ItemData.ItemRarity rarity;
        [Min(0f)] public float weight = 1f;
    }

    [System.Serializable]
    public class RarityGold
    {
        public ItemData.ItemRarity rarity;
        [Min(0)] public int gold = 0;
    }

    [Header("可获得的卡牌")]
    [Tooltip("卡池里所有可能抽到的卡牌，稀有度取自每张卡自身的 rarity 字段")]
    public List<ItemData> cards = new List<ItemData>();

    [Header("稀有度权重")]
    [Tooltip("先按权重滚定稀有度，再在该稀有度的卡里等概率抽一张")]
    public List<RarityWeight> rarityWeights = new List<RarityWeight>();

    [Header("手牌已满补偿金币")]
    [Tooltip("手牌已满无法获得卡牌时，按该卡稀有度补偿的金币数量")]
    public List<RarityGold> fullHandCompensation = new List<RarityGold>();

    /// <summary> 查询某稀有度在手牌已满时的金币补偿；未配置返回 0 </summary>
    public int GetCompensationGold(ItemData.ItemRarity rarity)
    {
        if (fullHandCompensation != null)
        {
            foreach (var rg in fullHandCompensation)
            {
                if (rg.rarity == rarity) return Mathf.Max(0, rg.gold);
            }
        }
        return 0;
    }

    /// <summary> 抽一张卡：先按权重滚稀有度，该稀有度无卡则顺延到最近有卡稀有度，再等概率抽一张；卡池为空返回 null </summary>
    public ItemData DrawCard()
    {
        if (cards == null || cards.Count == 0) return null;

        ItemData.ItemRarity rolled = RollRarity();
        if (TryFindNearestRarityWithCards(rolled, out ItemData.ItemRarity finalRarity))
        {
            return PickRandomOfRarity(finalRarity);
        }

        return cards[Random.Range(0, cards.Count)];
    }

    /// <summary> 按权重滚一个稀有度；未配置权重时退化为按卡池中实际存在的稀有度等概率 </summary>
    private ItemData.ItemRarity RollRarity()
    {
        float total = 0f;
        if (rarityWeights != null)
        {
            foreach (var rw in rarityWeights)
            {
                if (rw.weight > 0f) total += rw.weight;
            }
        }

        if (total <= 0f)
        {
            return cards[Random.Range(0, cards.Count)].rarity;
        }

        float r = Random.value * total;
        foreach (var rw in rarityWeights)
        {
            if (rw.weight <= 0f) continue;
            r -= rw.weight;
            if (r <= 0f) return rw.rarity;
        }
        return rarityWeights[rarityWeights.Count - 1].rarity;
    }

    private bool HasCardOfRarity(ItemData.ItemRarity rarity)
    {
        foreach (var c in cards)
        {
            if (c != null && c.rarity == rarity) return true;
        }
        return false;
    }

    private ItemData PickRandomOfRarity(ItemData.ItemRarity rarity)
    {
        List<ItemData> matched = new List<ItemData>();
        foreach (var c in cards)
        {
            if (c != null && c.rarity == rarity) matched.Add(c);
        }
        if (matched.Count == 0) return null;
        return matched[Random.Range(0, matched.Count)];
    }

    /// <summary> 从起始稀有度向两侧按数值距离由近及远查找有卡的稀有度（同距离优先低稀有度） </summary>
    private bool TryFindNearestRarityWithCards(ItemData.ItemRarity start, out ItemData.ItemRarity result)
    {
        result = start;
        if (HasCardOfRarity(start)) return true;

        int startIdx = (int)start;
        int maxIdx = System.Enum.GetValues(typeof(ItemData.ItemRarity)).Length - 1;
        for (int dist = 1; dist <= maxIdx; dist++)
        {
            int lower = startIdx - dist;
            if (lower >= 0 && HasCardOfRarity((ItemData.ItemRarity)lower))
            {
                result = (ItemData.ItemRarity)lower;
                return true;
            }
            int higher = startIdx + dist;
            if (higher <= maxIdx && HasCardOfRarity((ItemData.ItemRarity)higher))
            {
                result = (ItemData.ItemRarity)higher;
                return true;
            }
        }
        return false;
    }
}
