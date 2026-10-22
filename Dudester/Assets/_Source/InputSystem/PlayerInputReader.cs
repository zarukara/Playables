using UnityEngine;
using UnityEngine.InputSystem;

namespace InputSystem
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public Vector2 ReadMovement()
        {
            Vector2 value = Vector2.zero;
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                value.x += (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0)
                    - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
                value.y += (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0)
                    - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
            }
            if (Gamepad.current != null) value += Gamepad.current.leftStick.ReadValue();
            return Vector2.ClampMagnitude(value, 1);
        }
    }

}
