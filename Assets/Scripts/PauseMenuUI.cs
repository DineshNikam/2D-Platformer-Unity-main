using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    public void Resume()
    {
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.ResumeGame();
    }

    public void Restart()
    {
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.RestartGame();
    }

    public void GoToMenu()
    {
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.GoToMainMenu();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }
    }

    public void Quit()
    {
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.ShowQuitConfirmation();
    }
}
