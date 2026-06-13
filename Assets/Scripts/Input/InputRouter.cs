using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisCourse
{
    public sealed class InputRouter : MonoBehaviour
    {
        private const float InitialRepeatDelay = 0.18f;
        private const float RepeatInterval = 0.08f;
        private const float SoftDropInterval = 0.04f;

        private float leftTimer;
        private float rightTimer;
        private float softDropTimer;

        public event Action<InputCommand> CommandSent;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            HandleRepeat(IsPressed(keyboard.aKey, keyboard.leftArrowKey), WasPressed(keyboard.aKey, keyboard.leftArrowKey), ref leftTimer, InputCommand.MoveLeft, RepeatInterval);
            HandleRepeat(IsPressed(keyboard.dKey, keyboard.rightArrowKey), WasPressed(keyboard.dKey, keyboard.rightArrowKey), ref rightTimer, InputCommand.MoveRight, RepeatInterval);
            HandleRepeat(IsPressed(keyboard.sKey, keyboard.downArrowKey), WasPressed(keyboard.sKey, keyboard.downArrowKey), ref softDropTimer, InputCommand.SoftDrop, SoftDropInterval);

            if (WasPressed(keyboard.wKey, keyboard.upArrowKey) || keyboard.xKey.wasPressedThisFrame)
            {
                SendRotate();
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                SendHardDrop();
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                SendHold();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SendPause();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                SendRestart();
            }
        }

        public void SendMoveLeft() => Dispatch(InputCommand.MoveLeft);
        public void SendMoveRight() => Dispatch(InputCommand.MoveRight);
        public void SendRotate() => Dispatch(InputCommand.Rotate);
        public void SendHardDrop() => Dispatch(InputCommand.HardDrop);
        public void SendHold() => Dispatch(InputCommand.Hold);
        public void SendPause() => Dispatch(InputCommand.Pause);
        public void SendRestart() => Dispatch(InputCommand.Restart);

        private void HandleRepeat(bool pressed, bool pressedThisFrame, ref float timer, InputCommand command, float repeatInterval)
        {
            if (pressedThisFrame)
            {
                Dispatch(command);
                timer = InitialRepeatDelay;
                return;
            }

            if (!pressed)
            {
                timer = 0f;
                return;
            }

            timer -= Time.unscaledDeltaTime;
            if (timer > 0f)
            {
                return;
            }

            Dispatch(command);
            timer = repeatInterval;
        }

        private bool IsPressed(params UnityEngine.InputSystem.Controls.KeyControl[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].isPressed)
                {
                    return true;
                }
            }

            return false;
        }

        private bool WasPressed(params UnityEngine.InputSystem.Controls.KeyControl[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private void Dispatch(InputCommand command)
        {
            CommandSent?.Invoke(command);
        }
    }
}
