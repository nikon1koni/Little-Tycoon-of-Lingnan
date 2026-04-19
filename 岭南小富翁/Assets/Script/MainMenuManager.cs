using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private string gameSceneName = "New"; // 要加载的游戏场景名称

    [Header("按钮引用（可选，若不拖拽则自动查找）")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // 如果未手动拖拽按钮，自动根据名称查找
        if (startButton == null)
            startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (quitButton == null)
            quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();

        // 绑定监听事件
        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);
        else
            Debug.LogError("未找到开始按钮！请确保按钮命名为 StartButton 或手动拖拽赋值。");

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);
        else
            Debug.LogError("未找到退出按钮！请确保按钮命名为 QuitButton 或手动拖拽赋值。");
    }

    /// <summary>
    /// 开始游戏：加载指定场景
    /// </summary>
    public void OnStartGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError("请填写游戏场景名称！");
    }

    /// <summary>
    /// 退出游戏：结束运行
    /// </summary>
    public void OnQuitGame()
    {
        Debug.Log("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}