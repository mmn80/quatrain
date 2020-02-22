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
        public Move(byte moveType, byte x, byte y, byte z)
        {
            this.moveType = moveType; this.x = x; this.y = y; this.z = z;
        }

        public byte moveType;
        public byte x, y, z;

        public bool Apply(ref Game game)
        {
            if (moveType == 0)
                return game.DoAddStone(x, y);
            else if (moveType == 1)
                return game.DoRemoveStone(x, y, z);
            else if (moveType == 2)
                return game.RandomMove(true);
            else
                return false;
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

        void GenNextMove(Move move, ref float bestNext, ref Game best,
            ref int tries, ref float total)
        {
            var next = new Game(ref this);
            next.AiMove = move;

            if (next.AiMove.Apply(ref next))
            {
                Game nextBest;
                var val = next.Eval(out nextBest);
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
                                    GenNextMove(new Move(0, x, y, 0),
                                        ref bestNext, ref best, ref tries, ref total);
                    }
                    else if (GameMode == GameMode.Remove)
                    {
                        for (byte rx = 0; rx < 4; rx++)
                            for (byte ry = 0; ry < 4; ry++)
                                for (byte rz = 0; rz < 4; rz++)
                                    if (CanRemoveStone(rx, ry, rz))
                                        GenNextMove(new Move(1, rx, ry, rz),
                                            ref bestNext, ref best, ref tries, ref total);
                    }
                }
                else
                    for (int i = 0; i < Width; i++)
                        GenNextMove(new Move(2, 0, 0, 0),
                            ref bestNext, ref best, ref tries, ref total);
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
            AiMove.moveType = 2;
            AiMove.x = AiMove.y = AiMove.z = 0;

            var attempts = 0;
            if (GameMode == GameMode.Add)
            {
                AiMove.moveType = 0;
                do
                {
                    AiMove.x = (byte)UnityEngine.Random.Range(0, 4);
                    AiMove.y = (byte)UnityEngine.Random.Range(0, 4);
                    if (DoAddStone(AiMove.x, AiMove.y))
                        return true;
                }
                while (!onlyValidMoves || attempts++ < 20);
                return false;
            }
            if (GameMode == GameMode.Remove)
            {
                AiMove.moveType = 1;
                do
                {
                    AiMove.x = (byte)UnityEngine.Random.Range(0, 4);
                    AiMove.y = (byte)UnityEngine.Random.Range(0, 4);
                    AiMove.z = (byte)UnityEngine.Random.Range(0, 4);
                    if (DoRemoveStone(AiMove.x, AiMove.y, AiMove.z))
                        return true;
                }
                while (!onlyValidMoves || attempts++ < 20);
                return false;
            }
            return false;
        }
    }
}