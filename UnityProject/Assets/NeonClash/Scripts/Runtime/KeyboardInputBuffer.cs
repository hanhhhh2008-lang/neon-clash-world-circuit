using UnityEngine;

namespace NeonClash
{
    /// <summary>Maps the original web keyboard scheme into fixed-tick commands.</summary>
    public sealed class KeyboardInputBuffer
    {
        private readonly bool playerTwo;
        private readonly bool touchEnabled;
        private FighterButtons pressed;
        private FighterButtons held;
        private int move;
        private bool controllerJumpHeld;

        public KeyboardInputBuffer(bool playerTwo, bool touchEnabled = false)
        {
            this.playerTwo = playerTwo;
            this.touchEnabled = touchEnabled;
        }

        public void CaptureFrame()
        {
            if (playerTwo) CapturePlayerTwo();
            else
            {
                CapturePlayerOne();
                CaptureController();
                if (touchEnabled) CaptureTouches();
            }
        }

        public FighterCommand ConsumeTick()
        {
            FighterCommand command = new FighterCommand();
            command.Move = move;
            command.Buttons = held | pressed;
            pressed = FighterButtons.None;
            return command;
        }

        private void CapturePlayerOne()
        {
            move = (Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
            held = FighterButtons.None;
            if (Input.GetKey(KeyCode.S)) held |= FighterButtons.Crouch;
            if (Input.GetKey(KeyCode.Space)) held |= FighterButtons.Guard;
            if (Input.GetKeyDown(KeyCode.W)) pressed |= FighterButtons.Jump;
            if (Input.GetKeyDown(KeyCode.T)) pressed |= FighterButtons.LightPunch;
            if (Input.GetKeyDown(KeyCode.Y)) pressed |= FighterButtons.HeavyPunch;
            if (Input.GetKeyDown(KeyCode.U)) pressed |= FighterButtons.LightKick;
            if (Input.GetKeyDown(KeyCode.K)) pressed |= FighterButtons.HeavyKick;
            if (Input.GetKeyDown(KeyCode.L)) pressed |= FighterButtons.Special;
            if (Input.GetKeyDown(KeyCode.O)) pressed |= FighterButtons.Impact;
        }

        private void CapturePlayerTwo()
        {
            move = (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0) + (Input.GetKey(KeyCode.RightArrow) ? 1 : 0);
            held = FighterButtons.None;
            if (Input.GetKey(KeyCode.DownArrow)) held |= FighterButtons.Crouch;
            if (Input.GetKey(KeyCode.RightShift)) held |= FighterButtons.Guard;
            if (Input.GetKeyDown(KeyCode.UpArrow)) pressed |= FighterButtons.Jump;
            if (Input.GetKeyDown(KeyCode.Keypad1)) pressed |= FighterButtons.LightPunch;
            if (Input.GetKeyDown(KeyCode.Keypad2)) pressed |= FighterButtons.HeavyPunch;
            if (Input.GetKeyDown(KeyCode.Keypad3)) pressed |= FighterButtons.LightKick;
            if (Input.GetKeyDown(KeyCode.Keypad4)) pressed |= FighterButtons.HeavyKick;
            if (Input.GetKeyDown(KeyCode.Keypad5)) pressed |= FighterButtons.Special;
            if (Input.GetKeyDown(KeyCode.Keypad6)) pressed |= FighterButtons.Impact;
        }

        private void CaptureController()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            if (horizontal < -0.35f) move = -1;
            else if (horizontal > 0.35f) move = 1;
            bool jumpHeld = vertical > 0.65f;
            if (jumpHeld && !controllerJumpHeld) pressed |= FighterButtons.Jump;
            controllerJumpHeld = jumpHeld;
            if (vertical < -0.65f) held |= FighterButtons.Crouch;
            if (Input.GetKey(KeyCode.JoystickButton4)) held |= FighterButtons.Guard;
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) pressed |= FighterButtons.LightPunch;
            if (Input.GetKeyDown(KeyCode.JoystickButton1)) pressed |= FighterButtons.HeavyPunch;
            if (Input.GetKeyDown(KeyCode.JoystickButton2)) pressed |= FighterButtons.LightKick;
            if (Input.GetKeyDown(KeyCode.JoystickButton3)) pressed |= FighterButtons.HeavyKick;
            if (Input.GetKeyDown(KeyCode.JoystickButton5)) pressed |= FighterButtons.Special;
            if (Input.GetKeyDown(KeyCode.JoystickButton7)) pressed |= FighterButtons.Impact;
        }

        private void CaptureTouches()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
                float x = touch.position.x / Screen.width;
                float y = touch.position.y / Screen.height;
                bool began = touch.phase == TouchPhase.Began;
                if (x < 0.3f)
                {
                    if (y > 0.68f && began) pressed |= FighterButtons.Jump;
                    else if (y < 0.28f) held |= FighterButtons.Crouch;
                    else move = x < 0.15f ? -1 : 1;
                }
                else if (x < 0.48f)
                {
                    held |= FighterButtons.Guard;
                }
                else if (began)
                {
                    int column = Mathf.Clamp(Mathf.FloorToInt((x - 0.52f) / 0.16f), 0, 2);
                    bool upper = y > 0.5f;
                    FighterButtons[,] map =
                    {
                        { FighterButtons.LightPunch, FighterButtons.HeavyPunch, FighterButtons.Special },
                        { FighterButtons.LightKick, FighterButtons.HeavyKick, FighterButtons.Impact }
                    };
                    pressed |= map[upper ? 0 : 1, column];
                }
            }
        }
    }
}
