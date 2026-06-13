using UnityEngine;

namespace TetrisCourse
{
    public sealed class ActivePiece
    {
        public TetrominoType Type;
        public Vector2Int Position;
        public int Rotation;

        public ActivePiece(TetrominoType type, Vector2Int position)
        {
            Type = type;
            Position = position;
            Rotation = 0;
        }

        public Vector2Int[] GetCells()
        {
            Vector2Int[] shape = TetrominoData.GetShape(Type, Rotation);
            Vector2Int[] cells = new Vector2Int[shape.Length];

            for (int i = 0; i < shape.Length; i++)
            {
                cells[i] = Position + shape[i];
            }

            return cells;
        }
    }
}
