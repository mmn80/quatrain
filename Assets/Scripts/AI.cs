using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace Quatrene
{
    public enum PlayerType { Human, Vegas, Carlos }

    public struct GameAiJob : IJobParallelFor
    {
        public byte depth, width;
        public Game game;
        public byte player;
        public NativeArray<Move> moves;
        public NativeArray<int> tries;
        public NativeArray<double> results;

        public void Execute(int i)
        {
            var stats = new AiStats(0);
            var next = new Game(ref game);
            if (next.ApplyMove(moves[i]))
                results[i] = next.EvalVegas(depth, width, player, ref stats);
            else
                results[i] = -10000;
            tries[i] = stats.Tries;
        }
    }

    public struct Move
    {
        [ReadOnly]
        private readonly byte m;

        public Move(byte _moveType, byte _x, byte _y, byte _z)
        {
            m = (byte)((byte)(_moveType << 6) | 
                (byte)(_z << 4) | (byte)(_y << 2) | _x);
        }

        public byte x
        {
            get => (byte)((byte)(m << 6) >> 6);
        }
        public byte y
        {
            get => (byte)((byte)(m << 4) >> 6);
        }
        public byte z
        {
            get => (byte)((byte)(m << 2) >> 6);
        }
        public byte moveType
        {
            get => (byte)(m >> 6);
        }

        public override string ToString() =>
            (moveType == 0 ? $"add {x} {y}" : $"remove {x} {y} {z}");
    }

    public struct AiValue
    {
        public double Score;
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

        static System.Random Seed = new System.Random();
        static byte Rnd4() => (byte)Seed.Next(4);
        static double SmallNoise() => (Seed.Next(100) - 50) * 0.00000001f;

        double EvalCurrent(byte player) => SmallNoise() +
            (double)(GetScore(player) - GetScore((byte)(player == 0 ? 1 : 0)));

        public bool ApplyMove(Move move)
        {
            if (move.moveType == 0)
                return AddStone(move.x, move.y);
            else if (move.moveType == 1)
                return RemoveStone(move.x, move.y, move.z);
            else
                return false;
        }

        public double EvalVegas(byte depth, byte width, byte player, ref AiStats stats)
        {
            double score = -10000;
            if (aiDepth >= depth || GameMode == GameMode.GameOver)
                score = EvalCurrent(player);
            else
            {
                byte tries = 0;
                double total = 0;

                var moves = GetValidMoves().ToArray();

                byte i = 0;
                if (aiDepth > 1)
                    i = (byte)Seed.Next(moves.Length);
                UInt64 usedMoves = 0;
                byte usedMovesNo = 0;
                while (true)
                {
                    if (aiDepth > 1)
                        while ((usedMoves & ((UInt64)1 << i)) != 0)
                            i = (byte)Seed.Next(moves.Length);

                    var move = moves[i];
                    double scoreNext = -10000;
                    var next = new Game(ref this);
                    if (next.ApplyMove(move))
                        scoreNext = next.EvalVegas(depth, width, player, ref stats);
                    if (scoreNext > -1000)
                    {
                        total += scoreNext;
                        tries++;
                        stats.Tries++;
                        if (aiDepth == 0)
                            stats.Moves.Add(new AiValue() {
                                Move = move, Score = scoreNext });
                    }

                    if (aiDepth > 1)
                    {
                        if (++usedMovesNo >= width)
                            break;
                    }
                    else if (++i >= moves.Length)
                        break;
                }

                if (tries == 0)
                    score = EvalCurrent(player);
                else
                    score = total / tries;
            }
            return score;
        }

        IEnumerable<Move> GetValidMoves()
        {
            if (GameMode == GameMode.Add)
            {
                for (byte x = 0; x < 4; x++)
                    for (byte y = 0; y < 4; y++)
                        if (CanAddStone(x, y))
                            yield return new Move(0, x, y, 0);
            }
            else if (GameMode == GameMode.Remove)
            {
                for (byte x = 0; x < 4; x++)
                    for (byte y = 0; y < 4; y++)
                        for (byte z = 0; z < 4; z++)
                            if (CanRemoveStone(x, y, z))
                                yield return new Move(1, x, y, z);
            }
        }

        public void MakeAiMove(PlayerType player, byte depth = 6, byte width = 4)
        {
            if (GameMode == GameMode.Lobby || GameMode == GameMode.GameOver)
                return;

            Seed = new System.Random();
            aiTimer = new Stopwatch();
            AiStats = new AiStats(0);

            AiMode = true;
            aiTimer.Start();

            var movesArr = GetValidMoves().ToArray();
            var results = new NativeArray<double>(64, Allocator.Persistent);
            var tries = new NativeArray<int>(64, Allocator.Persistent);
            var moves = new NativeArray<Move>(movesArr, Allocator.Persistent);

            var job = new GameAiJob();
            job.game = this;
            job.player = GetPlayer();
            job.depth = depth;
            job.width = width;
            job.moves = moves;
            job.results = results;
            job.tries = tries;

            var handle = job.Schedule(movesArr.Length, 2);

            handle.Complete();

            var best = new AiValue()
            {
                Score = -10000,
                Move = new Move(2, 0, 0, 0)
            };
            double total = 0;

            AiStats.Tries = 0;
            for (int i = 0; i < movesArr.Length; i++)
            {
                var result = results[i];
                var val = new AiValue()
                {
                    Score = result,
                    Move = movesArr[i]
                };
                AiStats.Moves.Add(val);
                AiStats.Tries += tries[i];
                if (best.Score < result)
                    best = val;
                total += result;
            }

            moves.Dispose();
            results.Dispose();
            tries.Dispose();

            aiTimer.Stop();
            AiMode = false;

            ApplyMove(best.Move);

            if (ShowAiDebugInfo)
                MainControl.ShowAiDebugInfo();
        }

        public static Stopwatch aiTimer;
    }
}