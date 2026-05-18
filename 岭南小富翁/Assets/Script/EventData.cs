using UnityEngine;

[CreateAssetMenu(fileName = "NewEvent", menuName = "Game/Event Data")]
public class EventData : ScriptableObject
{
    [Header("事件基本信息")]
    public string eventTitle;
    public Sprite eventImage;
    [TextArea(3, 10)]
    public string eventDescription;

    [Header("事件选项")]
    public EventOption[] options;

    [System.Serializable]
    public class EventOption
    {
        public string optionText;
        public UnityEngine.Events.UnityEvent onOptionSelected;
    }
}
