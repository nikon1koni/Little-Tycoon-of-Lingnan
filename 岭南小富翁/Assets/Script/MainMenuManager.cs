using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private string gameSceneName = "New";

    [Header("按钮配置")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("音效配置")]
    [SerializeField] private SFXConfig sfxConfig;

    private void Start()
    {
        EnsureSFXManagerExists();

        if (startButton == null)
            startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (quitButton == null)
            quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);
        else
            Debug.LogError("未找到开始按钮，请确保按钮名称为StartButton或手动拖拽");

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);
        else
            Debug.LogError("未找到退出按钮，请确保按钮名称为QuitButton或手动拖拽");
    }

    public void OnStartGame()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);

        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError("未设置游戏场景名称");
    }

    public void OnQuitGame()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);

        Debug.Log("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void EnsureSFXManagerExists()
    {
        if (SFXManager.Instance != null) return;

        GameObject sfxObj = new GameObject("SFXManager");
        SFXManager sfxManager = sfxObj.AddComponent<SFXManager>();

        if (sfxConfig != null)
        {
            sfxManager.config = sfxConfig;
            sfxManager.ReloadClips();
        }
        else
        {
            sfxConfig = Resources.Load<SFXConfig>("SFXConfig");
            if (sfxConfig != null)
            {
                sfxManager.config = sfxConfig;
                sfxManager.ReloadClips();
            }
        }
    }
}
