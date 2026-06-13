using System;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisCourse
{
    public sealed class PieceService
    {
        private readonly Queue<TetrominoType> bag = new Queue<TetrominoType>();
        private readonly System.Random random = new System.Random();

        public ActivePiece ActivePiece { get; private set; }
        public TetrominoType NextPiece { get; private set; }
        public TetrominoType HoldPiece { get; private set; } = TetrominoType.None;
        public bool CanHold { get; private set; }

        public void Reset()
        {
            bag.Clear();
            HoldPiece = TetrominoType.None;
            CanHold = true;
            NextPiece = DrawFromBag();
            ActivePiece = null;
        }

        public ActivePiece SpawnNext()
        {
            ActivePiece = CreatePiece(NextPiece);
            NextPiece = DrawFromBag();
            CanHold = true;
            return ActivePiece;
        }

        public bool TryHold()
        {
            if (!CanHold || ActivePiece == null)
            {
                return false;
            }

            TetrominoType currentType = ActivePiece.Type;

            if (HoldPiece == TetrominoType.None)
            {
                HoldPiece = currentType;
                SpawnNext();
            }
            else
            {
                ActivePiece = CreatePiece(HoldPiece);
                HoldPiece = currentType;
            }

            CanHold = false;
            return true;
        }

        private ActivePiece CreatePiece(TetrominoType type)
        {
            return new ActivePiece(type, new Vector2Int(BoardModel.Width / 2, BoardModel.Height - 2));
        }

        private TetrominoType DrawFromBag()
        {
            if (bag.Count == 0)
            {
                RefillBag();
            }

            return bag.Dequeue();
        }

        private void RefillBag()
        {
            List<TetrominoType> types = new List<TetrominoType>(TetrominoData.AllTypes);

            // 7-bag: каждая из 7 фигур появляется ровно один раз до перемешивания следующего набора.
            for (int i = types.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                TetrominoType temp = types[i];
                types[i] = types[j];
                types[j] = temp;
            }

            foreach (TetrominoType type in types)
            {
                bag.Enqueue(type);
            }
        }
    }
}
