using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public GameObject mobileControls;

    public bool fadeToBlack, fadeFromBlack;
    public Image blackScreen;
    public float fadeSpeed = 2f;

    //player reference

    public PlayerController playerController;

    [Header("Diagnostics")]
    [Tooltip("Logs EnableMobileControls / DisableMobileControls calls (helps trace Player.Start on device).")]
    [SerializeField] bool logBootstrap = true;


    private void Awake()
    {
        instance = this;
    }

    void LogBootstrap(string message)
    {
        if (!logBootstrap) return;
        Debug.Log($"[UIManager:{name}] {message}", this);
    }

    public void DisableMobileControls()
    {
        LogBootstrap($"{nameof(DisableMobileControls)} enter; mobileControls={(mobileControls != null ? "assigned" : "NULL")}");
        if (mobileControls == null)
        {
            Debug.LogError($"[UIManager:{name}] {nameof(DisableMobileControls)}: mobileControls is NULL.", this);
            return;
        }
        mobileControls.SetActive(false);
        LogBootstrap($"{nameof(DisableMobileControls)} done.");
    }

    public void EnableMobileControls()
    {
        LogBootstrap($"{nameof(EnableMobileControls)} enter; mobileControls={(mobileControls != null ? "assigned" : "NULL")}");
        if (mobileControls == null)
        {
            Debug.LogError($"[UIManager:{name}] {nameof(EnableMobileControls)}: mobileControls is NULL.", this);
            return;
        }
        mobileControls.SetActive(true);
        LogBootstrap($"{nameof(EnableMobileControls)} done.");
    }

    private void Update()
    {
        UpdateFade();
    }

    private void UpdateFade()
    {
        if (fadeToBlack)
        {
            FadeToBlack();
        }
        else if (fadeFromBlack)
        {
            FadeFromBlack();
        }
    }

    private void FadeToBlack()
    {
        FadeScreen(1f);

        if (blackScreen.color.a >= 1f)
        {
            fadeToBlack = false;
        }
    }

    private void FadeFromBlack()
    {
        FadeScreen(0f);

        if (blackScreen.color.a <= 0f)
        {
            if(playerController.controlmode == Controls.mobile)
            {
                EnableMobileControls();
            }
            fadeFromBlack = false;
        }
    }

    private void FadeScreen(float targetAlpha)
    {
        Color currentColor = blackScreen.color;
        float newAlpha = Mathf.MoveTowards(currentColor.a, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        blackScreen.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
    }
}
