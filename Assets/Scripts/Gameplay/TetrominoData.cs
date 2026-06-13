using UnityEngine;

namespace TetrisCourse
{
    public enum TetrominoType
    {
        None,
        I,
        O,
        T,
        S,
        Z,
        J,
        L
    }

    public static class TetrominoData
    {
        private static readonly TetrominoType[] allTypes =
        {
            TetrominoType.I,
            TetrominoType.O,
            TetrominoType.T,
            TetrominoType.S,
            TetrominoType.Z,
            TetrominoType.J,
            TetrominoType.L
        };

        public static TetrominoType[] AllTypes => allTypes;

        public static Vector2Int[] GetShape(TetrominoType type, int rotation)
        {
            Vector2Int[] baseShape = GetBaseShape(type);
            Vector2Int[] result = new Vector2Int[baseShape.Length];
            int normalizedRotation = ((rotation % 4) + 4) % 4;

            for (int i = 0; i < baseShape.Length; i++)
            {
                result[i] = Rotate(baseShape[i], normalizedRotation, type);
            }

            return result;
        }

        public static Color GetColor(TetrominoType type, bool contrastMode)
        {
            if (contrastMode)
            {
                switch (type)
                {
                    case TetrominoType.I: return new Color(0.0f, 0.95f, 1.0f);
                    case TetrominoType.O: return new Color(1.0f, 0.95f, 0.0f);
                    case TetrominoType.T: return new Color(1.0f, 0.2f, 1.0f);
                    case TetrominoType.S: return new Color(0.15f, 1.0f, 0.15f);
                    case TetrominoType.Z: return new Color(1.0f, 0.1f, 0.1f);
                    case TetrominoType.J: return new Color(0.25f, 0.45f, 1.0f);
                    case TetrominoType.L: return new Color(1.0f, 0.55f, 0.0f);
                    default: return Color.white;
                }
            }

            switch (type)
            {
                case TetrominoType.I: return new Color(0.20f, 0.80f, 0.92f);
                case TetrominoType.O: return new Color(0.96f, 0.82f, 0.22f);
                case TetrominoType.T: return new Color(0.66f, 0.42f, 0.86f);
                case TetrominoType.S: return new Color(0.37f, 0.78f, 0.36f);
                case TetrominoType.Z: return new Color(0.90f, 0.28f, 0.26f);
                case TetrominoType.J: return new Color(0.26f, 0.45f, 0.86f);
                case TetrominoType.L: return new Color(0.95f, 0.55f, 0.20f);
                default: return Color.white;
            }
        }

        private static Vector2Int[] GetBaseShape(TetrominoType type)
        {
            switch (type)
            {
                case TetrominoType.I:
                    return new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
                case TetrominoType.O:
                    return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
                case TetrominoType.T:
                    return new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
                case TetrominoType.S:
                    return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) };
                case TetrominoType.Z:
                    return new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
                case TetrominoType.J:
                    return new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) };
                case TetrominoType.L:
                    return new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
                default:
                    return new Vector2Int[0];
            }
        }

        private static Vector2Int Rotate(Vector2Int point, int rotation, TetrominoType type)
        {
            if (type == TetrominoType.O)
            {
                return point;
            }

            for (int i = 0; i < rotation; i++)
            {
                point = new Vector2Int(point.y, -point.x);
            }

            return point;
        }
    }
}
