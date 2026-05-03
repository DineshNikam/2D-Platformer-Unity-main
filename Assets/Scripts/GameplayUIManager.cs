using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject hudPanel;
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject quitPanel;

    [Header("HUD Elements")]
    public Text scoreText;
    public Image[] heartImages;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;
    public Image powerUpIcon;
    public Image powerUpFillBar;

    [Header("Audio")]
    public AudioClip buttonClickSFX;

    public void PlayButtonClick()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(buttonClickSFX);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Try to bind buttons automatically
        BindButton(hudPanel, "Pause", ShowPauseMenu);
        BindButton(pausePanel, "Resume", ResumeGame);
        BindButton(pausePanel, "Restart", RestartGame);
        BindButton(pausePanel, "Settings", ShowSettings);
        BindButton(pausePanel, "Menu", GoToMainMenu);
        BindButton(pausePanel, "Quit", ShowQuitConfirmation);
        
        BindButton(settingsPanel, "Close", ShowPauseMenu);
        BindButton(settingsPanel, "Back", ShowPauseMenu);

        BindButton(quitPanel, "Yes", QuitGame);
        BindButton(quitPanel, "No", ShowPauseMenu);
        BindButton(quitPanel, "Close", ShowPauseMenu);

        // Auto-subscribe to game logic events
        var brain = Object.FindFirstObjectByType<PlayerBrain>();
        if (brain != null)
        {
            if (brain.Health != null)
            {
                UpdateHP(Mathf.CeilToInt(brain.Health.CurrentHp), Mathf.CeilToInt(brain.Health.MaxHp));
                brain.Health.Damaged += (amt, remaining) => UpdateHP(Mathf.CeilToInt(remaining), Mathf.CeilToInt(brain.Health.MaxHp));
            }
            brain.OnDied += () => ShowPauseMenu(true); // Show pause/game over panel on death, disabling resume
        }

        if (ScoreManager.Instance != null)
        {
            UpdateScore(Mathf.FloorToInt(ScoreManager.Instance.CurrentScore));
            ScoreManager.Instance.OnScoreChanged += (score) => UpdateScore(Mathf.FloorToInt(score));
        }

        ShowHUD();
        ApplyMobileControlVisibility();
    }

    private void ApplyMobileControlVisibility()
    {
        if (hudPanel == null) return;
        var pc = Object.FindFirstObjectByType<PlayerController>();
        bool showMobile = (pc == null) || (pc.controlmode == Controls.mobile);

        string[] mobileNames = { "Mobile_Left", "Mobile_Right", "Mobile_Jump" };
        foreach (var btnName in mobileNames)
        {
            var t = hudPanel.transform.Find(btnName);
            if (t != null) t.gameObject.SetActive(showMobile);
        }
    }

    private void BindButton(GameObject panel, string nameHint, UnityEngine.Events.UnityAction action)
    {
        if (panel == null) return;
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        bool found = false;
        foreach (var b in buttons)
        {
            // Check GameObject name
            if (b.gameObject.name.IndexOf(nameHint, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => { PlayButtonClick(); action(); });
                found = true;
            }
            // Check child text
            Text txt = b.GetComponentInChildren<Text>(true);
            if (txt != null && txt.text.IndexOf(nameHint, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => { PlayButtonClick(); action(); });
                found = true;
            }
        }
        
        if (!found)
            Debug.LogWarning($"Could not find button with hint '{nameHint}' in panel {panel.name}");
    }

    public void ShowHUD()
    {
        CloseAllPanels();
        if (hudPanel) hudPanel.SetActive(true);
        Time.timeScale = 1f;
    }

    public void ShowPauseMenu()
    {
        ShowPauseMenu(false);
    }

    public void ShowPauseMenu(bool isDeath)
    {
        CloseAllPanels();
        if (pausePanel)
        {
            pausePanel.SetActive(true);
            
            // Disable Resume button if this is a death state
            Transform resumeBtn = pausePanel.transform.Find("bg/Btn_Resume");
            if (resumeBtn != null)
            {
                Button btn = resumeBtn.GetComponent<Button>();
                if (btn != null) btn.interactable = !isDeath;
            }
        }
        Time.timeScale = 0f;
    }

    public void ShowSettings()
    {
        CloseAllPanels();
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void ShowQuitConfirmation()
    {
        CloseAllPanels();
        if (quitPanel) quitPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        ShowHUD();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Quit Game Called");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    private void CloseAllPanels()
    {
        if (hudPanel) hudPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (quitPanel) quitPanel.SetActive(false);
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        if (heartImages == null || heartImages.Length == 0) return;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < maxHP)
            {
                heartImages[i].gameObject.SetActive(true);
                heartImages[i].sprite = (i < currentHP) ? fullHeartSprite : emptyHeartSprite;
            }
            else
            {
                heartImages[i].gameObject.SetActive(false);
            }
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = $"SCORE: {score:D5}";
    }
}
