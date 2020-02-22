using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;

namespace Quatrene
{
    public struct MyJob : IJob
    {
        public float a;
        public float b;
        public NativeArray<float> result;

        public void Execute()
        {
            result[0] = a + b;
        }
    }

    public struct Move
    {
        public byte moveType;
        public byte x, y, z;

        public bool Apply(ref Game game)
        {
            if (moveType == 0)
                return game.DoAddStone(x, y);
            else
                return game.DoRemoveStone(x, y, z);
        }

        public override string ToString() =>
            (moveType == 0 ? $"add {x} {y}" : $"remove {x} {y} {z}");
    }

    public partial struct Game
    {
        public static int Width, Depth;
        public static byte Player;
        public static int Tries;
        public static List<Game> Moves = new List<Game>();

        void GenNextMove(bool random, ref float bestNext, ref Game best,
            ref int tries, ref float total)
        {
            var next = new Game(ref this);
            var ok = (random ?
                next.RandomGen(true, out next.AiMove) :
                next.AllGen(true, out next.AiMove));
            if (ok)
            {
                Game nn;
                var val = next.Eval(out nn);
                if (AiDepth == 0)
                    Moves.Add(next);
                if (val > bestNext || tries == 0)
                {
                    bestNext = val;
                    best = next;
                }
                total += val;
                tries++;
                Tries++;
            }
        }

        bool RandomGen(bool onlyValid, out Move move) =>
            RandomMoveExt(onlyValid, out move);

        bool AllGen(bool onlyValid, out Move move)
        {
            move.moveType = allMove.moveType;
            move.x = allMove.x;
            move.y = allMove.y;
            move.z = allMove.z;
            return move.Apply(ref this);
        }

        static Move allMove;

        public float Eval(out Game best)
        {
            AiScore = 0;
            best = new Game();
            
            if (AiDepth >= Depth || GameMode == GameMode.GameOver)
                AiScore = GetAiScore();
            else
            {
                float total = 0, bestNext = -10000;
                var tries = 0;
                if (AiDepth <= 1)
                {
                    if (GameMode == GameMode.Add)
                    {
                        for (byte x = 0; x < 4; x++)
                            for (byte y = 0; y < 4; y++)
                                if (CanAddStone(x, y))
                                {
                                    allMove.moveType = 0;
                                    allMove.x = x;
                                    allMove.y = y;
                                    allMove.z = 0;
                                    GenNextMove(false, ref bestNext, ref best, ref tries, ref total);
                                }
                    }
                    else if (GameMode == GameMode.Remove)
                    {
                        for (byte rx = 0; rx < 4; rx++)
                            for (byte ry = 0; ry < 4; ry++)
                                for (byte rz = 0; rz < 4; rz++)
                                    if (CanRemoveStone(rx, ry, rz))
                                    {
                                        allMove.moveType = 1;
                                        allMove.x = rx;
                                        allMove.y = ry;
                                        allMove.z = rz;
                                        GenNextMove(false, ref bestNext, ref best, ref tries, ref total);
                                    }
                    }
                }
                else
                    for (int i = 0; i < Width; i++)
                        GenNextMove(true, ref bestNext, ref best, ref tries, ref total);
                if (tries == 0)
                    AiScore = GetAiScore();
                else
                    AiScore = total / Width;
            }
            return AiScore;
        }

        public void AIMove(int depth = 6, int width = 4)
        {
            if (GameMode == GameMode.Lobby || GameMode == GameMode.GameOver)
                return;

            aiTimer = new Stopwatch();
            aiTimer.Start();

            Tries = 0;
            Moves.Clear();
            Width = width;
            Depth = depth;
            Player = GetPlayer();

            Game.AiMode = true;
            Game best;
            var score = Eval(out best);
            Game.AiMode = false;

            if (best.AiDepth > 0)
                best.AiMove.Apply(ref this);

            aiTimer.Stop();

            if (ShowAiDebugInfo)
                MainControl.ShowAiDebugInfo();
        }

        public static Stopwatch aiTimer;

        public float GetAiScore() =>
            GetScore(Player) - GetScore((byte)(Player == 0 ? 1 : 0));

        public bool RandomMove(bool onlyValidMoves = true)
        {
            Move move;
            return RandomMoveExt(onlyValidMoves, out move);
        }

        public bool RandomMoveExt(bool onlyValidMoves, out Move move)
        {
            move.moveType = move.x = move.y = move.z = 0;
            var attempts = 0;
            if (GameMode == GameMode.Add)
            {
                do
                {
                    move.x = (byte)UnityEngine.Random.Range(0, 4);
                    move.y = (byte)UnityEngine.Random.Range(0, 4);
                    if (DoAddStone(move.x, move.y))
                        return true;
                }
                while (!onlyValidMoves || attempts++ < 20);
                return false;
            }
            if (GameMode == GameMode.Remove)
            {
                move.moveType = 1;
                do
                {
                    move.x = (byte)UnityEngine.Random.Range(0, 4);
                    move.y = (byte)UnityEngine.Random.Range(0, 4);
                    move.z = (byte)UnityEngine.Random.Range(0, 4);
                    if (DoRemoveStone(move.x, move.y, move.z))
                        return true;
                }
                while (!onlyValidMoves || attempts++ < 20);
                return false;
            }
            return false;
        }
    }
}