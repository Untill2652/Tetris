using UnityEngine;

namespace TetrisCourse
{
    public sealed class ScoreService
    {
        public int Score { get; private set; }
        public int Lines { get; private set; }
        public int Level { get; private set; } = 1;
        public float FallDelay { get; private set; } = 0.8f;
        public float DurationSec { get; private set; }

        public void Reset()
        {
            Score = 0;
            Lines = 0;
            Level = 1;
            FallDelay = 0.8f;
            DurationSec = 0f;
        }

        public void Tick(float deltaTime)
        {
            DurationSec += deltaTime;
        }

        public void AddClearedLines(int clearedLines)
        {
            if (clearedLines <= 0)
            {
                return;
            }

            int baseScore = 0;
            switch (clearedLines)
            {
                case 1: baseScore = 100; break;
                case 2: baseScore = 300; break;
                case 3: baseScore = 500; break;
                default: baseScore = 800; break;
            }

            Score += baseScore * Level;
            Lines += clearedLines;
            Level = Lines / 5 + 1;
            FallDelay = Mathf.Max(0.20f, 0.8f - (Level - 1) * 0.06f);
        }
    }
}
