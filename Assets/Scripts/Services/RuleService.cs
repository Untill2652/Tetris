using UnityEngine;

namespace TetrisCourse
{
    public sealed class RuleService
    {
        private static readonly Vector2Int[] rotationOffsets =
        {
            Vector2Int.zero,
            Vector2Int.left,
            Vector2Int.right
        };

        public bool CanPlace(BoardModel board, ActivePiece piece, Vector2Int position, int rotation)
        {
            Vector2Int[] shape = TetrominoData.GetShape(piece.Type, rotation);

            for (int i = 0; i < shape.Length; i++)
            {
                Vector2Int cell = position + shape[i];

                if (!board.IsInside(cell) || board.IsFilled(cell))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryMove(BoardModel board, ActivePiece piece, Vector2Int delta)
        {
            Vector2Int nextPosition = piece.Position + delta;

            if (!CanPlace(board, piece, nextPosition, piece.Rotation))
            {
                return false;
            }

            piece.Position = nextPosition;
            return true;
        }

        public bool TryRotate(BoardModel board, ActivePiece piece)
        {
            int nextRotation = (piece.Rotation + 1) % 4;

            for (int i = 0; i < rotationOffsets.Length; i++)
            {
                Vector2Int candidatePosition = piece.Position + rotationOffsets[i];

                if (!CanPlace(board, piece, candidatePosition, nextRotation))
                {
                    continue;
                }

                piece.Position = candidatePosition;
                piece.Rotation = nextRotation;
                return true;
            }

            return false;
        }

        public void LockPiece(BoardModel board, ActivePiece piece)
        {
            Vector2Int[] cells = piece.GetCells();

            for (int i = 0; i < cells.Length; i++)
            {
                board.SetCell(cells[i], piece.Type);
            }
        }
    }
}
