using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    [Header("Girdi Değerleri")]
    public Vector2 Move;
    public Vector2 Look;
    public bool Jump;

    [Header("Kürsör Ayarları")]
    public bool LockCursor = true;

    // Player Input bileşeninin "Send Messages" modu bu metotları otomatik çağırır:
    private void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    private void OnLook(InputValue value)
    {
        Look = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        Jump = value.isPressed;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (LockCursor)
        {
            Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !hasFocus;
        }
    }
}