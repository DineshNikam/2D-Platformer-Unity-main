using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;


    [SerializeField] private TMP_Text coinText;

    [SerializeField] private PlayerController playerController;

    Health _playerHealth;

    private int coinCount = 0;
    private int gemCount = 0;
    private bool isGameOver = false;
    private Vector3 playerPosition;

    //Level Complete

    [SerializeField] GameObject levelCompletePanel;
    [SerializeField] TMP_Text leveCompletePanelTitle;
    [SerializeField] TMP_Text levelCompleteCoins;




   
    private int totalCoins = 0;
  



    private void Awake()
    {
        instance = this;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        UpdateGUI();
        if (UIManager.instance != null)
        {
            UIManager.instance.fadeFromBlack = true;
        }
        playerPosition = playerController.transform.position;

        FindTotalPickups();

        _playerHealth = playerController != null ? playerController.GetComponent<Health>() : null;
        if (_playerHealth != null)
            _playerHealth.Died += HandlePlayerHealthDied;
    }

    void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.Died -= HandlePlayerHealthDied;
    }

    void HandlePlayerHealthDied()
    {
        Death();
    }

    public void IncrementCoinCount()
    {
        coinCount++;
        UpdateGUI();
    }
    public void IncrementGemCount()
    {
        gemCount++;
        UpdateGUI();
    }

    private void UpdateGUI()
    {
        coinText.text = coinCount.ToString();
  
    }

    public void Death()
    {
        if (!isGameOver)
        {
            isGameOver = true;

            if (UIManager.instance != null)
            {
                // Disable Mobile Controls
                UIManager.instance.DisableMobileControls();
                // Initiate screen fade
                UIManager.instance.fadeToBlack = true;
            }

            // Disable player input instead of hiding the object immediately
            // This allows the death animation and game feel to play out
            if (playerController != null)
            {
                playerController.DisableInput();
            }

            // Start death coroutine to wait and then show the menu
            StartCoroutine(DeathCoroutine());

            // Log death message
            Debug.Log("Died");
        }
    }
 
    public void FindTotalPickups()
    {

        pickup[] pickups = Object.FindObjectsByType<pickup>(FindObjectsSortMode.None);

        foreach (pickup pickupObject in pickups)
        {
            if (pickupObject.pt == pickup.pickupType.coin)
            {
                totalCoins += 1;
            }
           
        }


      
    }
    public void LevelComplete()
    {
       


        levelCompletePanel.SetActive(true);
        leveCompletePanelTitle.text = "LEVEL COMPLETE";



        levelCompleteCoins.text = "COINS COLLECTED: "+ coinCount.ToString() +" / " + totalCoins.ToString();
 
    }
   
    public IEnumerator DeathCoroutine()
    {
        // Wait for player to see death animation/UI
        yield return new WaitForSeconds(1.5f);
        
        // Show Game Over / Pause menu after the delay
        if (GameplayUIManager.Instance != null)
        {
            GameplayUIManager.Instance.ShowPauseMenu(true);
        }

        // We no longer reload the scene automatically; 
        // the user interacts with the UI buttons.
    }

}
