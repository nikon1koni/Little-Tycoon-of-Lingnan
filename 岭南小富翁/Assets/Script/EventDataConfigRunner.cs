using UnityEngine;
using UnityEditor;
using System.IO;

public class EventDataConfigRunner : MonoBehaviour
{
    [ContextMenu("Configure All Event Data")]
    public void ConfigureAllEventData()
    {
        ConfigureDanJiaFishermanEvent();
        ConfigureHerbalTeaEvent();
        ConfigureCompetitorEvent();
        ConfigureGovernmentEvent();
        ConfigureAncestralHallEvent();
        ConfigureTeaStandEvent();
        ConfigureTyphoonEvent();
        ConfigureOldBrandGuildEvent();

        AssetDatabase.SaveAssets();
        Debug.Log("所有事件数据配置完成！");
    }

    void ConfigureDanJiaFishermanEvent()
    {
        EventData eventData = LoadEventData("疍家渔民求助");
        if (eventData == null) return;

        eventData.eventTitle = "[疍家渔民求助]";
        eventData.eventDescription = "一队疍民遭遇风浪，货物受损，请求借款维修资金，承诺加倍奉还。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "借出20铜钱 -> 2回合后返还50铜钱",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.Loan,
                loanAmount = 20,
                loanRepayMultiplier = 2.5f,
                loanRepayRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "借出30铜钱 -> 2回合后返还80铜钱",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.Loan,
                loanAmount = 30,
                loanRepayMultiplier = 2.67f,
                loanRepayRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "爱莫能助 -> 下一次步数减半",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.NextRollMultiplier,
                nextRollMultiplier = 0.5f
            }
        };

        SaveEventData(eventData, "疍家渔民求助");
    }

    void ConfigureHerbalTeaEvent()
    {
        EventData eventData = LoadEventData("百年凉茶秘方");
        if (eventData == null) return;

        eventData.eventTitle = "[百年凉茶秘方]";
        eventData.eventDescription = "你在旧书店发现一张发黄的凉茶配方，可能价值连城，也可能一文不值。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "花5铜钱买下研究 -> 10%概率获得50铜钱",
                optionCostAmount = 5,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "花10铜钱买下研究 -> 20%概率获得100铜钱",
                optionCostAmount = 10,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "无视 -> 无事发生",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "百年凉茶秘方");
    }

    void ConfigureCompetitorEvent()
    {
        EventData eventData = LoadEventData("同行恶性竞争");
        if (eventData == null) return;

        eventData.eventTitle = "[同行恶性竞争]";
        eventData.eventDescription = "隔壁街的老店开始降价抢生意，你的客源被分流。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "被动应对: 所有产业下一回合收入减半",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.IncomeReduction,
                incomeReductionPercent = 0.5f,
                incomeReductionRounds = 1
            },
            new EventData.EventOption()
            {
                optionText = "支付20铜钱进行营销反击 -> 消除减收",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "同行恶性竞争");
    }

    void ConfigureGovernmentEvent()
    {
        EventData eventData = LoadEventData("官府基建征用");
        if (eventData == null) return;

        eventData.eventTitle = "[官府基建征用]";
        eventData.eventDescription = "官府要修路，需要征用你的一块地，会给予补偿。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "同意征用 -> 失去产业，获得120%补偿",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.PropertyCompensation,
                propertyCompensationPercent = 1.2f
            },
            new EventData.EventOption()
            {
                optionText = "支付30铜钱打点费 -> 保留产业",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "官府基建征用");
    }

    void ConfigureAncestralHallEvent()
    {
        EventData eventData = LoadEventData("祠堂祈福");
        if (eventData == null) return;

        eventData.eventTitle = "[祠堂祈福]";
        eventData.eventDescription = "镇上的老祠堂香火旺盛，据说祈福很灵验。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "支付30铜钱 -> 2回合内税收减半",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.TaxReduction,
                taxReductionPercent = 0.5f,
                taxReductionRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "诚心上香 -> 1回合内免疫负面事件",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.ImmuneToNegativeEvents,
                roundsImmuneToNegativeEvents = 1
            }
        };

        SaveEventData(eventData, "祠堂祈福");
    }

    void ConfigureTeaStandEvent()
    {
        EventData eventData = LoadEventData("神秘的功夫茶摊");
        if (eventData == null) return;

        eventData.eventTitle = "[神秘的功夫茶摊]";
        eventData.eventDescription = "一位老师傅请你喝一杯茶，茶香沁人心脾，但他似乎有话要说。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "支付5铜钱 -> 下一次骰子步数翻倍",
                optionCostAmount = 5,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.NextRollMultiplier,
                nextRollMultiplier = 2f
            },
            new EventData.EventOption()
            {
                optionText = "喝完道谢 -> 无事发生",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "神秘的功夫茶摊");
    }

    void ConfigureTyphoonEvent()
    {
        EventData eventData = LoadEventData("台风过境");
        if (eventData == null) return;

        eventData.eventTitle = "[台风过境]";
        eventData.eventDescription = "狂风暴雨席卷小镇，部分产业受损。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "支付20铜钱加固防护 -> 随机1处产业降1级",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.BuildingDowngrade,
                buildingDowngradeCount = 1
            },
            new EventData.EventOption()
            {
                optionText = "听天由命 -> 随机2处产业降1级",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.BuildingDowngrade,
                buildingDowngradeCount = 2
            }
        };

        SaveEventData(eventData, "台风过境");
    }

    void ConfigureOldBrandGuildEvent()
    {
        EventData eventData = LoadEventData("老字号商会邀约");
        if (eventData == null) return;

        eventData.eventTitle = "[老字号商会邀约]";
        eventData.eventDescription = "商会会长看中你的潜力，邀请你加入。加入需缴纳一笔会费，但从此商路亨通。";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "缴纳30铜钱入会费 -> 获得[商誉](3回合内收入+40%)",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                optionIncomeBoost = 0.4f,
                optionBuffDurationRounds = 3,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "婉言谢绝 -> 获得15铜钱",
                optionCostAmount = 0,
                optionRewardAmount = 15,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "老字号商会邀约");
    }

    EventData LoadEventData(string eventName)
    {
        string path = "Assets/Building/Event Data/" + eventName + ".asset";
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
        return eventData;
    }

    void SaveEventData(EventData eventData, string eventName)
    {
        EditorUtility.SetDirty(eventData);
        Debug.Log("配置完成: " + eventName);
    }
}
