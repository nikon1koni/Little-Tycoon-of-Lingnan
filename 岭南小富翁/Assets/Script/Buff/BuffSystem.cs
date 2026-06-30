using System.Collections.Generic;
using UnityEngine;

public class BuffSystem : MonoBehaviour
{
    public static BuffSystem Instance { get; private set; }

    [System.Serializable]
    public class Buff
    {
        public string buffId;
        public string sourceName;
        public BuildingData.BuffEffect effectType;
        public float value;
        public float duration;
        public float remainingTime;
        public bool isPermanent;
        public int remainingRounds;
        public bool useRoundTimer;
        public object source;
        public string customDescription;

        public Buff(string id, string sourceName, BuildingData.BuffEffect type, float val, float dur = 0f, int rounds = 0, object srcObj = null, string desc = "")
        {
            buffId = id;
            this.sourceName = sourceName;
            effectType = type;
            value = val;
            duration = dur;
            remainingTime = dur;
            isPermanent = dur <= 0f && rounds <= 0;
            remainingRounds = rounds;
            useRoundTimer = rounds > 0;
            source = srcObj;
            customDescription = desc;
        }
    }

    private Dictionary<Player, List<Buff>> playerBuffs = new Dictionary<Player, List<Buff>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateTimeBasedBuffs();
    }

    public void OnRoundChanged()
    {
        UpdateRoundBasedBuffs();
    }

    private void UpdateTimeBasedBuffs()
    {
        foreach (var kvp in new Dictionary<Player, List<Buff>>(playerBuffs))
        {
            Player player = kvp.Key;
            List<Buff> buffs = kvp.Value;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = buffs[i];
                if (!buff.isPermanent && !buff.useRoundTimer)
                {
                    buff.remainingTime -= Time.deltaTime;
                    if (buff.remainingTime <= 0f)
                    {
                        RemoveBuff(player, buff);
                    }
                }
            }
        }
    }

    private void UpdateRoundBasedBuffs()
    {
        foreach (var kvp in new Dictionary<Player, List<Buff>>(playerBuffs))
        {
            Player player = kvp.Key;
            List<Buff> buffs = kvp.Value;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = buffs[i];
                if (!buff.isPermanent && buff.useRoundTimer)
                {
                    buff.remainingRounds--;
                    if (buff.remainingRounds <= 0)
                    {
                        // Debuff - 
                        if (buff.effectType == BuildingData.BuffEffect.Bankrupt)
                        {
                            HandleBankruptBuffExpired(player);
                        }
                        RemoveBuff(player, buff);
                    }
                }
            }
        }
    }
    
    // Debuff
    private void HandleBankruptBuffExpired(Player player)
    {
        Debug.Log($"{player.playerName} 破产Debuff到期，游戏结束");
        
        player.isBankrupt = true;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{player.playerName} 破产了！", 3f);
        }
        
        // 
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckGameOverAfterBankrupt();
        }
    }

    public void AddBuff(Player player, Buff buff)
    {
        if (!playerBuffs.ContainsKey(player))
        {
            playerBuffs[player] = new List<Buff>();
        }

        playerBuffs[player].Add(buff);
        Debug.Log($"{player.playerName} 获得 Buff: {BuildingData.GetBuffEffectName(buff.effectType)} +{buff.value * 100}% (来源: {buff.sourceName})");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"获得 {BuildingData.GetBuffEffectName(buff.effectType)} 效果!", 2f);
        }
        
        //  Buff 
        UpdateBuffDisplay();
    }

    public void RemoveBuff(Player player, Buff buff)
    {
        if (playerBuffs.ContainsKey(player) && playerBuffs[player].Remove(buff))
        {
            Debug.Log($"{player.playerName} 移除 Buff 效果: {BuildingData.GetBuffEffectName(buff.effectType)} (来源: {buff.sourceName})");
            
            UpdateBuffDisplay();
        }
    }
    
    private void UpdateBuffDisplay()
    {
        if (BuffDisplayManager.Instance != null)
        {
            BuffDisplayManager.Instance.UpdateBuffDisplay();
        }
    }

    public void RemoveAllBuffsFromSource(Player player, object source)
    {
        if (!playerBuffs.ContainsKey(player)) return;

        List<Buff> buffs = playerBuffs[player];
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            if (buffs[i].source == source)
            {
                buffs.RemoveAt(i);
            }
        }
    }

    public void RemoveAllBuffs(Player player)
    {
        if (playerBuffs.ContainsKey(player))
        {
            playerBuffs[player].Clear();
        }
    }

    public float GetTotalBuffValue(Player player, BuildingData.BuffEffect effectType)
    {
        float total = 0f;

        if (playerBuffs.ContainsKey(player))
        {
            foreach (Buff buff in playerBuffs[player])
            {
                if (buff.effectType == effectType)
                {
                    total += buff.value;
                }
            }
        }

        return total;
    }

    public bool HasDiceBoost(Player player)
    {
        return GetTotalBuffValue(player, BuildingData.BuffEffect.DiceBoost) > 0f;
    }

    public int GetDiceBoostValue(Player player)
    {
        return Mathf.RoundToInt(GetTotalBuffValue(player, BuildingData.BuffEffect.DiceBoost));
    }

    public float GetIncomeMultiplier(Player player)
    {
        float multiplier = 1f;
        multiplier += GetTotalBuffValue(player, BuildingData.BuffEffect.IncomeMultiplier);
        multiplier += GetTotalBuffValue(player, BuildingData.BuffEffect.AllIncomeBoost);
        return Mathf.Max(0.1f, multiplier);
    }

    public float GetMoveSpeedMultiplier(Player player)
    {
        float multiplier = 1f;
        multiplier += GetTotalBuffValue(player, BuildingData.BuffEffect.MoveSpeedBoost);
        return Mathf.Max(0.1f, multiplier);
    }

    public float GetLuckBoost(Player player)
    {
        return GetTotalBuffValue(player, BuildingData.BuffEffect.LuckBoost);
    }

    public float GetDefenseBoost(Player player)
    {
        return GetTotalBuffValue(player, BuildingData.BuffEffect.DefenseBoost);
    }

    public List<Buff> GetPlayerBuffs(Player player)
    {
        if (playerBuffs.ContainsKey(player))
        {
            return new List<Buff>(playerBuffs[player]);
        }
        return new List<Buff>();
    }

    public List<Buff> CreateBuildingBuffs(BuildingData data, int level, BoardTile sourceTile)
    {
        List<Buff> buffs = new List<Buff>();
        List<BuildingData.BuildingBuffConfig> configs = data.GetBuffConfigs();
        
        foreach (var config in configs)
        {
            string buffId = $"building_{sourceTile.GetInstanceID()}_{config.effectType}";
            float buffValue = data.GetBuffValue(level, config);
            
            buffs.Add(new Buff(
                buffId,
                data.buildingName,
                config.effectType,
                buffValue,
                config.isPermanent ? 0f : config.duration,
                config.isPermanent ? 0 : config.durationRounds,
                sourceTile
            ));
        }
        
        return buffs;
    }

    // Buff
    public Buff CreateBuildingBuff(BuildingData data, int level, BoardTile sourceTile)
    {
        string buffId = $"building_{sourceTile.GetInstanceID()}_{data.buildingName}";
        float buffValue = data.GetBuffValue(level);
        float duration = data.buffDuration;

        return new Buff(
            buffId,
            data.buildingName,
            data.buffEffect,
            buffValue,
            duration,
            0,
            sourceTile
        );
    }

    public Buff CreateEventBuff(EventData data, int optionIndex, object source)
    {
        string buffId = $"event_{data.GetInstanceID()}_{optionIndex}";
        float boost = 0f;
        int rounds = 0;

        if (optionIndex >= 0 && optionIndex < data.options.Length)
        {
            EventData.EventOption option = data.options[optionIndex];
            boost = option.optionIncomeBoost > 0 ? option.optionIncomeBoost : data.incomeBoost;
            rounds = option.optionBuffDurationRounds > 0 ? option.optionBuffDurationRounds : data.buffDurationRounds;
        }
        else
        {
            boost = data.incomeBoost;
            rounds = data.buffDurationRounds;
        }

        return new Buff(
            buffId,
            data.eventTitle,
            BuildingData.BuffEffect.IncomeMultiplier,
            boost,
            0f,
            rounds,
            source
        );
    }
}
