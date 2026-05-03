using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Events : MonoBehaviour
{
    private GameObject _settingsPanel;

    private void Start()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "Settings Canvas")
            {
                _settingsPanel = root;
                break;
            }
        }

        var menuCanvas = GameObject.Find("Canvas")?.transform;
        if (menuCanvas != null)
        {
            var settingsBtn = menuCanvas.Find("Settings")?.GetComponent<Button>();
            if (settingsBtn != null)
                settingsBtn.onClick.AddListener(OpenSettings);
        }

        if (_settingsPanel != null)
        {
            var close = _settingsPanel.transform.Find("bg/bg_Settings/Btn_Close");
            if (close != null)
            {
                var b = close.GetComponent<Button>();
                if (b != null) b.onClick.AddListener(CloseSettings);
            }
        }
    }

    public void OpenSettings()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }

    public void Menu()
    {
        SceneManager.LoadScene(0);
    }

    public void Level()
    {
        SceneManager.LoadScene(1);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
