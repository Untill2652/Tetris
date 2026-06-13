using UnityEngine;

namespace TetrisCourse
{
    public struct CellData
    {
        public bool Filled;
        public TetrominoType Type;

        public CellData(bool filled, TetrominoType type)
        {
            Filled = filled;
            Type = type;
        }
    }

    public sealed class BoardModel
    {
        public const int Width = 10;
        public const int Height = 20;

        private readonly CellData[,] cells = new CellData[Width, Height];

        public CellData GetCell(int x, int y)
        {
            return cells[x, y];
        }

        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height;
        }

        public bool IsFilled(Vector2Int position)
        {
            return IsInside(position) && cells[position.x, position.y].Filled;
        }

        public void SetCell(Vector2Int position, TetrominoType type)
        {
            if (!IsInside(position))
            {
                return;
            }

            cells[position.x, position.y] = new CellData(true, type);
        }

        public void Clear()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cells[x, y] = new CellData(false, TetrominoType.None);
                }
            }
        }

        public int ClearCompletedLines()
        {
            int cleared = 0;

            for (int y = 0; y < Height; y++)
            {
                if (!IsLineFull(y))
                {
                    continue;
                }

                RemoveLine(y);
                cleared++;
                y--;
            }

            return cleared;
        }

        private bool IsLineFull(int y)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!cells[x, y].Filled)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveLine(int line)
        {
            // Все строки выше очищенной опускаются на одну клетку вниз.
            for (int y = line; y < Height - 1; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    cells[x, y] = cells[x, y + 1];
                }
            }

            for (int x = 0; x < Width; x++)
            {
                cells[x, Height - 1] = new CellData(false, TetrominoType.None);
            }
        }
    }
}
