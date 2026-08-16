using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("点击开始后跳转到的场景（加载场景）")]
    [SerializeField] private string gameSceneName = "Loading";

    [Header("按钮引用")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("音效配置")]
    [SerializeField] private SFXConfig sfxConfig;

    private void Start()
    {
        StartCoroutine(StartWithABInit());
    }

    IEnumerator StartWithABInit()
    {
        yield return ResourceLoader.InitializeAsync();
        Debug.Log("[MainMenu] ResourceLoader初始化完成, UseAssetBundles=" + ResourceLoader.UseAssetBundles);
        yield return EnsureSFXManagerExistsAsync();

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

    IEnumerator EnsureSFXManagerExistsAsync()
    {
        if (SFXManager.Instance != null) yield break;

        GameObject sfxObj = new GameObject("SFXManager");
        SFXManager sfxManager = sfxObj.AddComponent<SFXManager>();

        if (sfxConfig != null)
        {
            sfxManager.config = sfxConfig;
            sfxManager.ReloadClips();
        }
        else
        {
            // 异步从AB包加载 SFXConfig
            Debug.Log("[MainMenu] 异步加载 SFXConfig (AB包)...");
            var op = ResourceLoader.LoadAsync<SFXConfig>("config_data", "SFXConfig");
            yield return op.WaitForCompletion();
            sfxConfig = op.Result;

            // 回退：异步从 Resources 加载
            if (sfxConfig == null)
            {
                Debug.LogWarning("[MainMenu] AB包未找到SFXConfig，回退Resources.LoadAsync...");
                ResourceRequest rr = Resources.LoadAsync<SFXConfig>("SFXConfig");
                yield return rr;
                sfxConfig = rr.asset as SFXConfig;
            }
            if (sfxConfig != null)
            {
                sfxManager.config = sfxConfig;
                sfxManager.ReloadClips();
                Debug.Log("[MainMenu] SFXConfig 异步加载完成");
            }
        }
    }
}
