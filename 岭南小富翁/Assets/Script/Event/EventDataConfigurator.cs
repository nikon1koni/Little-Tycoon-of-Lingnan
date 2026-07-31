using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public class EventDataConfigurator : EditorWindow
{
    private string eventDataFolderPath = "Assets/Building/Event Data";

    [MenuItem("Tools/Event Data Configurator")]
    public static void ShowWindow()
    {
        GetWindow<EventDataConfigurator>("?????????");
    }

    void OnGUI()
    {
        GUILayout.Label("????????????? - ???????????????", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("???????????"))
        {
            ConfigureAllEvents();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("??????????????"))
        {
            ConfigureDanJiaFishermanEvent();
        }

        if (GUILayout.Button("????????????"))
        {
            ConfigureHerbalTeaEvent();
        }

        if (GUILayout.Button("??????????????"))
        {
            ConfigureCompetitorEvent();
        }

        if (GUILayout.Button("??????????????"))
        {
            ConfigureGovernmentEvent();
        }

        if (GUILayout.Button("???????????????"))
        {
            ConfigureAncestralHallEvent();
        }

        if (GUILayout.Button("?????????????"))
        {
            ConfigureTeaStandEvent();
        }

        if (GUILayout.Button("?????????????"))
        {
            ConfigureTyphoonEvent();
        }

        if (GUILayout.Button("???????????????"))
        {
            ConfigureOldBrandGuildEvent();
        }

        GUILayout.Space(20);
        GUILayout.Label("????????????????", EditorStyles.miniLabel);
    }

    void ConfigureAllEvents()
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
        Debug.Log("??????????????");
        EditorUtility.DisplayDialog("???????", "????????????8?????", "???");
    }

    void ConfigureDanJiaFishermanEvent()
    {
        EventData eventData = LoadOrCreateEventData("????????");
        if (eventData == null) return;

        eventData.eventTitle = "[????????]???????";
        eventData.eventDescription = "???????????????????????????????????????????????????????????????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "??20?? -> 2????50??",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.Loan,
                loanAmount = 20,
                loanRepayMultiplier = 2.5f,
                loanRepayRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "??30?? -> 2????80??",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.Loan,
                loanAmount = 30,
                loanRepayMultiplier = 2.67f,
                loanRepayRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "?????? -> ????????????????",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.NextRollMultiplier,
                nextRollMultiplier = 0.5f
            }
        };

        SaveEventData(eventData, "????????");
    }

    void ConfigureHerbalTeaEvent()
    {
        EventData eventData = LoadOrCreateEventData("?????");
        if (eventData == null) return;

        eventData.eventTitle = "[?????]??????";
        eventData.eventDescription = "?????????????????????k??????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "????5????? -> ????????10%????50???",
                optionCostAmount = 5,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "????10????? -> ????????20%????100???",
                optionCostAmount = 10,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "??????? -> ??????",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "?????");
    }

    void ConfigureCompetitorEvent()
    {
        EventData eventData = LoadOrCreateEventData("????????");
        if (eventData == null) return;

        eventData.eventTitle = "[????????]???????";
        eventData.eventDescription = "?????????????????????????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "????????????????50%????1???",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.IncomeReduction,
                incomeReductionPercent = 0.5f,
                incomeReductionRounds = 1
            },
            new EventData.EventOption()
            {
                optionText = "????20????????? -> ??????",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "????????");
    }

    void ConfigureGovernmentEvent()
    {
        EventData eventData = LoadOrCreateEventData("???????");
        if (eventData == null) return;

        eventData.eventTitle = "[???????]???????";
        eventData.eventDescription = "?????????????????????????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "??????? -> ??????????120%?????",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.PropertyCompensation,
                propertyCompensationPercent = 1.2f
            },
            new EventData.EventOption()
            {
                optionText = "????30????? -> ??????",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "???????");
    }

    void ConfigureAncestralHallEvent()
    {
        EventData eventData = LoadOrCreateEventData("????????");
        if (eventData == null) return;

        eventData.eventTitle = "[????????]???????";
        eventData.eventDescription = "???????????????????????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "????30?????? -> ??????50%????2???",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.TaxReduction,
                taxReductionPercent = 0.5f,
                taxReductionRounds = 2
            },
            new EventData.EventOption()
            {
                optionText = "????? -> ??????????1???",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.ImmuneToNegativeEvents,
                roundsImmuneToNegativeEvents = 1
            }
        };

        SaveEventData(eventData, "????????");
    }

    void ConfigureTeaStandEvent()
    {
        EventData eventData = LoadOrCreateEventData("??????");
        if (eventData == null) return;

        eventData.eventTitle = "[??????]???????";
        eventData.eventDescription = "?????????????????????????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "????5?????? -> ????????????????",
                optionCostAmount = 5,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.NextRollMultiplier,
                nextRollMultiplier = 2f
            },
            new EventData.EventOption()
            {
                optionText = "???????? -> ??????",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "??????");
    }

    void ConfigureTyphoonEvent()
    {
        EventData eventData = LoadOrCreateEventData("??????");
        if (eventData == null) return;

        eventData.eventTitle = "[??????]??????";
        eventData.eventDescription = "????????????????????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "????20????? -> 1??????????",
                optionCostAmount = 20,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.BuildingDowngrade,
                buildingDowngradeCount = 1
            },
            new EventData.EventOption()
            {
                optionText = "??????? -> 2??????????",
                optionCostAmount = 0,
                optionRewardAmount = 0,
                effectType = EventData.EventEffectType.BuildingDowngrade,
                buildingDowngradeCount = 2
            }
        };

        SaveEventData(eventData, "??????");
    }

    void ConfigureOldBrandGuildEvent()
    {
        EventData eventData = LoadOrCreateEventData("????????");
        if (eventData == null) return;

        eventData.eventTitle = "[????????]???????";
        eventData.eventDescription = "????????????????????????????????";

        eventData.options = new EventData.EventOption[]
        {
            new EventData.EventOption()
            {
                optionText = "????30????? -> ????????(3???+40%)",
                optionCostAmount = 30,
                optionRewardAmount = 0,
                optionIncomeBoost = 0.4f,
                optionBuffDurationRounds = 3,
                effectType = EventData.EventEffectType.None
            },
            new EventData.EventOption()
            {
                optionText = "??????? -> ???15??????",
                optionCostAmount = 0,
                optionRewardAmount = 15,
                effectType = EventData.EventEffectType.None
            }
        };

        SaveEventData(eventData, "????????");
    }

    EventData LoadOrCreateEventData(string eventName)
    {
        string path = Path.Combine(eventDataFolderPath, $"{eventName}.asset");
        EventData eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);

        if (eventData == null)
        {
            eventData = ScriptableObject.CreateInstance<EventData>();
            AssetDatabase.CreateAsset(eventData, path);
            Debug.Log($"???????????: {path}");
        }

        return eventData;
    }

    void SaveEventData(EventData eventData, string eventName)
    {
        EditorUtility.SetDirty(eventData);
        Debug.Log($"?????????: {eventName}");
    }
}
#endif