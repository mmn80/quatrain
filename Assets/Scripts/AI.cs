using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;

namespace Quatrene
{
    public struct Move
    {
        public Move(byte moveType, byte x, byte y, byte z)
        {
            this.moveType = moveType; this.x = x; this.y = y; this.z = z;
        }

        public byte moveType;
        public byte x, y, z;

        public override string ToString() =>
            (moveType == 0 ? $"add {x} {y}" : $"remove {x} {y} {z}");
    }

    public struct GameAi : IJob
    {
        public Game game;
        public NativeArray<AiValue> result;

        public void Execute()
        {
            game.MakeAiMove();
            result[0] = game.aiValue;
        }
    }

    public struct AiValue
    {
        public float Score;
        public Move Move;
    }

    public partial struct Game
    {
        public static int Tries;
        public static List<AiValue> Moves = new List<AiValue>();

        public float GetAiScore(byte player) =>
            GetScore(player) - GetScore((byte)(player == 0 ? 1 : 0));

        public bool ApplyAiMove()
        {
            if (aiValue.Move.moveType == 0)
                return AddStone(aiValue.Move.x, aiValue.Move.y);
            else if (aiValue.Move.moveType == 1)
                return RemoveStone(aiValue.Move.x, aiValue.Move.y, aiValue.Move.z);
            else if (aiValue.Move.moveType == 2)
                return RandomMove(true);
            else if (aiValue.Move.moveType == 3)
                return true;
            else
                return false;
        }

        public bool RandomMove(bool onlyValidMoves = true)
        {
            aiValue.Move.moveType = 3;
            aiValue.Move.x = aiValue.Move.y = aiValue.Move.z = 0;

            var attempts = 0;
            if (GameMode == GameMode.Add)
            {
                aiValue.Move.moveType = 0;
                do
                {
                    aiValue.Move.x = (byte)UnityEngine.Random.Range(0, 4);
                    aiValue.Move.y = (byte)UnityEngine.Random.Range(0, 4);
                    if (ApplyAiMove())
                        return true;
                }
                while (!onlyValidMoves || attempts++ < 20);
                aiValue.Move.moveType = 3;
            }
            else if (GameMode == GameMode.Remove)
            {
                aiValue.Move.moveType = 1;
                do
                {
                    aiValue.Move.x = (byte)UnityEngine.Random.Range(0, 4);
                    aiValue.Move.y = (byte)UnityEngine.Random.Range(0, 4);
                    aiValue.Move.z = (byte)UnityEngine.Random.Range(0, 4);
                    if (ApplyAiMove())
                        return true;
                }
                while (!onlyValidMoves || attempts++ < 20);
                aiValue.Move.moveType = 3;
            }
            return aiValue.Move.moveType < 2;
        }

        void TryMove(Move move, ref int tries, ref float total,
            byte depth, byte width, byte player)
        {
            var next = new Game(ref this);
            next.aiValue.Move = move;
            if (next.ApplyAiMove())
            {
                var nextVal = next.aiValue;
                next.Eval(depth, width, player);
                nextVal.Score = next.aiValue.Score;
                if (AiDepth == 0)
                    Moves.Add(nextVal);
                if (nextVal.Score > aiValue.Score || tries == 0)
                    aiValue = nextVal;
                total += nextVal.Score;
                tries++;
            }
        }

        public int Eval(byte depth, byte width, byte player)
        {
            aiValue = new AiValue()
            {
                Score = -10000,
                Move = new Move(3, 0, 0, 0)
            };
            var tries = 0;
            if (AiDepth >= depth || GameMode == GameMode.GameOver)
                aiValue.Score = GetAiScore(player);
            else
            {
                float total = 0;
                if (AiDepth <= 1)
                {
                    if (GameMode == GameMode.Add)
                    {
                        for (byte x = 0; x < 4; x++)
                            for (byte y = 0; y < 4; y++)
                                if (CanAddStone(x, y))
                                    TryMove(new Move(0, x, y, 0),
                                        ref tries, ref total,
                                        depth, width, player);
                    }
                    else if (GameMode == GameMode.Remove)
                    {
                        for (byte rx = 0; rx < 4; rx++)
                            for (byte ry = 0; ry < 4; ry++)
                                for (byte rz = 0; rz < 4; rz++)
                                    if (CanRemoveStone(rx, ry, rz))
                                        TryMove(new Move(1, rx, ry, rz),
                                            ref tries, ref total,
                                            depth, width, player);
                    }
                }
                else
                    for (int i = 0; i < width; i++)
                        TryMove(new Move(2, 0, 0, 0),
                            ref tries, ref total, depth, width, player);
                if (tries == 0)
                    aiValue.Score = GetAiScore(player);
                else
                    aiValue.Score = total / width;
            }
            return tries;
        }

        public void MakeAiMove(byte depth = 6, byte width = 4)
        {
            if (GameMode == GameMode.Lobby || GameMode == GameMode.GameOver)
                return;

            aiTimer = new Stopwatch();
            aiTimer.Start();

            Moves.Clear();

            AiMode = true;
            Tries = Eval(depth, width, GetPlayer());
            AiMode = false;

            ApplyAiMove();

            aiTimer.Stop();

            if (ShowAiDebugInfo)
                MainControl.ShowAiDebugInfo();
        }

        public static Stopwatch aiTimer;
    }
}