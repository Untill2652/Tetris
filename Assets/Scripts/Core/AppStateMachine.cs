using System;

namespace TetrisCourse
{
    public enum AppState
    {
        MainMenu,
        Tutorial,
        Playing,
        Paused,
        Settings,
        Records,
        GameOver
    }

    public sealed class AppStateMachine
    {
        public AppState CurrentState { get; private set; } = AppState.MainMenu;
        public event Action<AppState> StateChanged;

        public void ChangeState(AppState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            StateChanged?.Invoke(CurrentState);
        }
    }
}
