using UnityEngine;
using UnityEngine.EventSystems;

public class MobileButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerController player;
    public enum ButtonType { Left, Right, Jump }
    public ButtonType buttonType;

    private void Start()
    {
        if (player == null)
            player = Object.FindFirstObjectByType<PlayerController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player == null) return;
        switch (buttonType)
        {
            case ButtonType.Left: player.MobileMove(-1f); break;
            case ButtonType.Right: player.MobileMove(1f); break;
            case ButtonType.Jump: player.MobileJump(); break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (player == null) return;
        switch (buttonType)
        {
            case ButtonType.Left:
            case ButtonType.Right:
                player.MobileMove(0f);
                break;
        }
    }
}
