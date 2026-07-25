using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{   
    public static GameInput Instance { get; private set; }

    public event EventHandler OnMenuButtonPressed;

    private InputActions inputActions;

    private void Awake()
    {
        Instance = this;
        inputActions = new InputActions();
        inputActions.Enable();

        inputActions.Player.Menu.performed += Menu_Performed;
    }

    private void OnDestroy()
    {
        inputActions.Disable();
    }

    private void Menu_Performed(InputAction.CallbackContext context)
    {
        OnMenuButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsUpActionPressed()
    {
        return inputActions.Player.LanderUp.IsPressed();
    }

    public bool IsLeftActionPressed()
    {
        return inputActions.Player.LanderLeft.IsPressed();
    }

    public bool IsRightActionPressed()
    {
        return inputActions.Player.LanderRight.IsPressed();
    }

    public Vector2 GetMovementInputVector2()
    {
        return inputActions.Player.Movement.ReadValue<Vector2>();
    }
}
