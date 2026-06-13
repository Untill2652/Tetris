using System;
using UnityEngine;

namespace TetrisCourse
{
    public sealed class GameSessionController : MonoBehaviour
    {
        private BoardModel board;
        private PieceService pieceService;
        private RuleService ruleService;
        private ScoreService scoreService;
        private StorageService storageService;
        private AppStateMachine stateMachine;
        private UIManager uiManager;

        private float fallTimer;
        private bool initialized;

        public BoardModel Board => board;
        public PieceService PieceService => pieceService;
        public ScoreService ScoreService => scoreService;

        public void Initialize(
            BoardModel boardModel,
            PieceService pieces,
            RuleService rules,
            ScoreService score,
            StorageService storage,
            AppStateMachine states,
            UIManager ui)
        {
            board = boardModel;
            pieceService = pieces;
            ruleService = rules;
            scoreService = score;
            storageService = storage;
            stateMachine = states;
            uiManager = ui;
            initialized = true;
        }

        public void HandleCommand(InputCommand command)
        {
            if (!initialized)
            {
                return;
            }

            if (command == InputCommand.Restart)
            {
                StartNewGame();
                return;
            }

            if (command == InputCommand.Pause)
            {
                TogglePause();
                return;
            }

            if (stateMachine.CurrentState != AppState.Playing || pieceService.ActivePiece == null)
            {
                return;
            }

            switch (command)
            {
                case InputCommand.MoveLeft:
                    ruleService.TryMove(board, pieceService.ActivePiece, Vector2Int.left);
                    break;
                case InputCommand.MoveRight:
                    ruleService.TryMove(board, pieceService.ActivePiece, Vector2Int.right);
                    break;
                case InputCommand.Rotate:
                    ruleService.TryRotate(board, pieceService.ActivePiece);
                    break;
                case InputCommand.SoftDrop:
                    StepDown();
                    break;
                case InputCommand.HardDrop:
                    HardDrop();
                    break;
                case InputCommand.Hold:
                    Hold();
                    break;
            }

            UpdateView();
        }

        public void StartNewGame()
        {
            board.Clear();
            scoreService.Reset();
            pieceService.Reset();
            fallTimer = 0f;
            stateMachine.ChangeState(AppState.Playing);
            SpawnNextOrGameOver();
            UpdateView();
        }

        public void ResumeGame()
        {
            if (stateMachine.CurrentState == AppState.Paused)
            {
                stateMachine.ChangeState(AppState.Playing);
            }
        }

        public void PauseGame()
        {
            if (stateMachine.CurrentState == AppState.Playing)
            {
                stateMachine.ChangeState(AppState.Paused);
            }
        }

        public void ReturnToMainMenu()
        {
            stateMachine.ChangeState(AppState.MainMenu);
        }

        private void Update()
        {
            if (!initialized || stateMachine.CurrentState != AppState.Playing || pieceService.ActivePiece == null)
            {
                return;
            }

            scoreService.Tick(Time.deltaTime);
            fallTimer += Time.deltaTime;

            if (fallTimer >= scoreService.FallDelay)
            {
                fallTimer = 0f;
                StepDown();
                UpdateView();
            }

            uiManager.UpdateHud(scoreService);
        }

        private void TogglePause()
        {
            if (stateMachine.CurrentState == AppState.Playing)
            {
                stateMachine.ChangeState(AppState.Paused);
            }
            else if (stateMachine.CurrentState == AppState.Paused)
            {
                stateMachine.ChangeState(AppState.Playing);
            }
        }

        private void StepDown()
        {
            if (!ruleService.TryMove(board, pieceService.ActivePiece, Vector2Int.down))
            {
                LockCurrentPiece();
            }
        }

        private void HardDrop()
        {
            while (ruleService.TryMove(board, pieceService.ActivePiece, Vector2Int.down))
            {
            }

            LockCurrentPiece();
        }

        private void Hold()
        {
            if (!pieceService.TryHold())
            {
                return;
            }

            if (!ruleService.CanPlace(board, pieceService.ActivePiece, pieceService.ActivePiece.Position, pieceService.ActivePiece.Rotation))
            {
                GameOver();
            }
        }

        private void LockCurrentPiece()
        {
            ruleService.LockPiece(board, pieceService.ActivePiece);
            int clearedLines = board.ClearCompletedLines();
            scoreService.AddClearedLines(clearedLines);
            SpawnNextOrGameOver();
        }

        private void SpawnNextOrGameOver()
        {
            ActivePiece piece = pieceService.SpawnNext();

            if (!ruleService.CanPlace(board, piece, piece.Position, piece.Rotation))
            {
                GameOver();
            }
        }

        private void GameOver()
        {
            stateMachine.ChangeState(AppState.GameOver);

            HighScoreEntry entry = new HighScoreEntry
            {
                score = scoreService.Score,
                lines = scoreService.Lines,
                level = scoreService.Level,
                durationSec = Mathf.RoundToInt(scoreService.DurationSec),
                dateIso = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            bool topScore = storageService.AddHighScore(entry);
            uiManager.ShowGameOver(scoreService, topScore);
            uiManager.RefreshRecords(storageService.LoadScores());
        }

        private void UpdateView()
        {
            uiManager.RenderGame(board, pieceService);
            uiManager.UpdateHud(scoreService);
        }
    }
}
