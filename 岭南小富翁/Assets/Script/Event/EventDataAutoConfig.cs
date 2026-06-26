using UnityEngine;
using UnityEditor;

public static class EventDataAutoConfig
{
    [MenuItem("Tools/Configure All Event Data NOW")]
    public static void ConfigureAllEvents()
    {
        Debug.Log("开始配置所有事件数据...");

        ConfigureDanJiaFishermanEvent();
        ConfigureHerbalTeaEvent();
        ConfigureCompetitorEvent();
        ConfigureGovernmentEvent();
        ConfigureAncestralHallEvent();
        ConfigureTeaStandEvent();
        ConfigureTyphoonEvent();
        ConfigureOldBrandGuildEvent();

        AssetDatabase.SaveAssets();
        Debug.Log("=== 所有8个事件配置完成 ===");
        EditorUtility.DisplayDialog("配置完成", "已成功配置所有8个事件", "确定");
    }

    private static void ConfigureDanJiaFishermanEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/蛋家渔民.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 蛋家渔民.asset");
            return;
        }

        eventData.eventTitle = "[蛋家渔民]借鱼出海";
        eventData.eventDescription = "蛋家人世代以捕鱼为生，如今遇到风浪急需周转。他们愿意用未来的渔获作为抵押向你借钱。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "借20铜钱 -> 2回合后还50铜钱",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.Loan,
                loanAmount = 20,
                loanRepayMultiplier = 2.5f,
                loanRepayRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "借30铜钱 -> 2回合后还80铜钱",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.Loan,
                loanAmount = 30,
                loanRepayMultiplier = 2.67f,
                loanRepayRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "婉言拒绝 -> 下一次骰子步数减半",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.NextRollMultiplier,
                nextRollMultiplier = 0.5f
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 蛋家渔民");
    }

    private static void ConfigureHerbalTeaEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/功夫茶.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 功夫茶.asset");
            return;
        }

        eventData.eventTitle = "[功夫茶]品茶论道";
        eventData.eventDescription = "一位老茶师邀请你品尝功夫茶，据说能增进财运。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "花费5铜钱品茶 -> 收入提升10%持续50回合",
                optionCostAmount = 5,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "花费10铜钱品茶 -> 收入提升20%持续100回合",
                optionCostAmount = 10,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "拒绝邀请 -> 无效果",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 功夫茶");
    }

    private static void ConfigureCompetitorEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/竞争对手.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 竞争对手.asset");
            return;
        }

        eventData.eventTitle = "[竞争对手]商业竞争";
        eventData.eventDescription = "附近出现了竞争对手，正在抢夺你的客源。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "硬扛竞争：收入减少50%持续1回合",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.IncomeReduction,
                incomeReductionPercent = 0.5f,
                incomeReductionRounds = 1
            },
            new EventData.EventOption()
            {
                optionText = "花费20铜钱送礼疏通 -> 化解竞争",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 竞争对手");
    }

    private static void ConfigureGovernmentEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/官府新政.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 官府新政.asset");
            return;
        }

        eventData.eventTitle = "[官府新政]新税政策";
        eventData.eventDescription = "官府颁布了新的税收政策，可能影响你的资产。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "配合新政 -> 获得房产价值120%的补偿",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.PropertyCompensation,
                propertyCompensationPercent = 1.2f
            },
            new EventData.EventOption()
            {
                optionText = "花费30铜钱打点 -> 免除影响",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 官府新政");
    }

    private static void ConfigureAncestralHallEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/祠堂祈福.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 祠堂祈福.asset");
            return;
        }

        eventData.eventTitle = "[祠堂祈福]祖先庇佑";
        eventData.eventDescription = "宗族祠堂举办祈福仪式，你可以选择参加。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "花费30铜钱祭拜 -> 税务减少50%持续2回合",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.TaxReduction,
                taxReductionPercent = 0.5f,
                taxReductionRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "简单祭拜 -> 免疫负面事件1回合",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.ImmuneToNegativeEvents,
                roundsImmuneToNegativeEvents = 1
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 祠堂祈福");
    }

    private static void ConfigureTeaStandEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/茶摊经营.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 茶摊经营.asset");
            return;
        }

        eventData.eventTitle = "[茶摊经营]茶摊奇遇";
        eventData.eventDescription = "路边茶摊来了一位神秘客人，可能带来好运。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "花费5铜钱招待 -> 下一次骰子步数翻倍",
                optionCostAmount = 5,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.NextRollMultiplier,
                nextRollMultiplier = 2f
            },
            new EventData.EventOption()
            {
                optionText = "不予理会 -> 无效果",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 茶摊经营");
    }

    private static void ConfigureTyphoonEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/台风灾害.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 台风灾害.asset");
            return;
        }

        eventData.eventTitle = "[台风灾害]狂风来袭";
        eventData.eventDescription = "台风即将来袭，你的建筑面临损毁风险。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "花费20铜钱加固 -> 1栋建筑降级",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.BuildingDowngrade,
                buildingDowngradeCount = 1
            },
            new EventData.EventOption()
            {
                optionText = "不做准备 -> 2栋建筑降级",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.BuildingDowngrade,
                buildingDowngradeCount = 2
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 台风灾害");
    }

    private static void ConfigureOldBrandGuildEvent()
    {
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Building/Event Data/老字号商会.asset");
        if (eventData == null)
        {
            Debug.LogError("找不到事件数据: 老字号商会.asset");
            return;
        }

        eventData.eventTitle = "[老字号商会]商会邀请";
        eventData.eventDescription = "城中老字号商会邀请你加入，可获得丰厚回报。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "花费30铜钱入会 -> 收入提升(3回合+40%)",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                optionIncomeBoost = 0.4f,
                optionBuffDurationRounds = 3,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "婉拒邀请 -> 获得15铜钱补偿",
                optionCostAmount = 0,
                optionRewardAmount = 15,
                effectType = EventData.EventEffectType.None
            }
        };

        EditorUtility.SetDirty(eventData);
        Debug.Log("已配置事件: 老字号商会");
    }
}