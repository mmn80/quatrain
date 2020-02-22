﻿using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;

namespace Quatrene
{
    public struct GameAi : IJob
    {
        public byte depth, width;
        public Game game;
        public int tries;
        public NativeList<AiValue> moves;
        public NativeArray<AiValue> result;

        public void Execute()
        {
            var tempStats = new AiStats(0);
            game.Eval(depth, width, game.GetPlayer(), ref tempStats);
            tries = tempStats.Tries;
            moves.Clear();
            foreach (var m in tempStats.Moves)
                moves.Add(m);
            result[0] = game.aiValue;
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

        public override string ToString() =>
            (moveType == 0 ? $"add {x} {y}" : $"remove {x} {y} {z}");
    }

    public struct AiValue
    {
        public float Score;
        public Move Move;
    }

    public struct AiStats
    {
        public AiStats(int tries)
        {
            Tries = tries;
            Moves = new List<AiValue>();
        }

        public int Tries;
        public List<AiValue> Moves;
    }

    public partial struct Game
    {
        public static AiStats AiStats;

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

        static System.Random Seed = new System.Random();

        static byte Rnd4() => //(byte)UnityEngine.Random.Range(0, 4);
            (byte)Seed.Next(4);

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
                    aiValue.Move.x = Rnd4();
                    aiValue.Move.y = Rnd4();
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
                    aiValue.Move.x = Rnd4();
                    aiValue.Move.y = Rnd4();
                    aiValue.Move.z = Rnd4();
                    if (ApplyAiMove())
                        return true;
                }
                while (!onlyValidMoves || attempts++ < 20);
                aiValue.Move.moveType = 3;
            }
            return aiValue.Move.moveType < 2;
        }

        void TryMove(Move move, ref AiStats stats, ref float total, ref byte tries,
            byte depth, byte width, byte player)
        {
            var next = new Game(ref this);
            next.aiValue.Move = move;
            if (next.ApplyAiMove())
            {
                var nextVal = next.aiValue;
                next.Eval(depth, width, player, ref stats);
                nextVal.Score = next.aiValue.Score;
                if (AiDepth == 0)
                    stats.Moves.Add(nextVal);
                if (nextVal.Score > aiValue.Score)
                    aiValue = nextVal;
                total += nextVal.Score;
                tries++;
                stats.Tries++;
            }
        }

        public void Eval(byte depth, byte width, byte player, ref AiStats stats)
        {
            aiValue = new AiValue()
            {
                Score = -10000,
                Move = new Move(3, 0, 0, 0)
            };
            if (AiDepth >= depth || GameMode == GameMode.GameOver)
                aiValue.Score = GetAiScore(player);
            else
            {
                byte tries = 0;
                float total = 0;
                if (AiDepth <= 1)
                {
                    if (GameMode == GameMode.Add)
                    {
                        for (byte x = 0; x < 4; x++)
                            for (byte y = 0; y < 4; y++)
                                if (CanAddStone(x, y))
                                    TryMove(new Move(0, x, y, 0),
                                        ref stats, ref total, ref tries,
                                        depth, width, player);
                    }
                    else if (GameMode == GameMode.Remove)
                    {
                        for (byte rx = 0; rx < 4; rx++)
                            for (byte ry = 0; ry < 4; ry++)
                                for (byte rz = 0; rz < 4; rz++)
                                    if (CanRemoveStone(rx, ry, rz))
                                        TryMove(new Move(1, rx, ry, rz),
                                            ref stats, ref total, ref tries,
                                            depth, width, player);
                    }
                }
                else
                    for (int i = 0; i < width; i++)
                        TryMove(new Move(2, 0, 0, 0),
                            ref stats, ref total, ref tries, depth, width, player);
                if (tries == 0)
                    aiValue.Score = GetAiScore(player);
                else
                    aiValue.Score = total / tries;
            }
        }

        public void MakeAiMove(byte depth = 6, byte width = 4)
        {
            if (GameMode == GameMode.Lobby || GameMode == GameMode.GameOver)
                return;

            Seed = new System.Random();
            aiTimer = new Stopwatch();
            aiTimer.Start();
            AiMode = true;

            AiStats = new AiStats(0);
            Eval(depth, width, GetPlayer(), ref AiStats);

            //MakeECSJob(depth, width);

            aiTimer.Stop();

            AiMode = false;
            ApplyAiMove();

            if (ShowAiDebugInfo)
                MainControl.ShowAiDebugInfo();
        }

        void MakeECSJob(byte depth, byte width)
        {
            var moves = new NativeList<AiValue>(Allocator.Persistent);
            var result = new NativeArray<AiValue>(1, Allocator.Persistent);

            var job = new GameAi();
            job.game = this;
            job.depth = depth;
            job.width = width;
            job.result = result;
            job.moves = moves;

            var handle = job.Schedule();
            handle.Complete();

            aiValue = job.result[0];

            foreach (var m in moves.ToArray())
                AiStats.Moves.Add(m);
            AiStats.Tries = job.tries;

            moves.Dispose();
            result.Dispose();
        }

        public static Stopwatch aiTimer;
    }
}