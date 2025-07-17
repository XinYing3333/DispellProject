using UnityEngine;


    public enum InputMode
    {
        Mouse,
        Gamepad
    }

    public static class InputDetector
    {
        public static InputMode CurrentInputMode { get; private set; } = InputMode.Mouse;

        public static void UpdateInputMode()
        {
            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
                CurrentInputMode = InputMode.Mouse;

            if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
                CurrentInputMode = InputMode.Gamepad;
        }
    }
    