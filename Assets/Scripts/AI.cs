using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace Quatrain
{
    public enum PlayerType { Human, Neumann, Carlos }

    public struct GameAiJob : IJobParallelFor
    {
        public PlayerType playerType;
        public byte ai_level;
        public Game game;
        public byte player;
        public NativeArray<Move> moves;
        public NativeArray<int> tries;
        public NativeArray<double> results;

        public void Execute(int i)
        {
            var totalTries = 0;
            var next = new Game(ref game);
            if (next.ApplyMove(moves[i]))
            {
                if (playerType == PlayerType.Neumann)
                    results[i] = next.EvalVegas(ai_level, player, ref totalTries);
                else if (playerType == PlayerType.Carlos)
                    results[i] = next.EvalCarlos(ai_level, player, out totalTries);
                else
                    results[i] = -10000;
            }
            else
                results[i] = -10000;
            tries[i] = totalTries;
        }
    }

    [Serializable]
    public struct Move
    {
        public byte m;

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

    public partial struct Game
    {
        public static System.Random Seed = new System.Random();
        static byte Rnd4() => (byte)Seed.Next(4);
        static double SmallNoise() => (Seed.Next(100) - 50) * 0.0000000000001d;

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

        static int[][] VegasConfig = new int[][] {
            new int[] { 0, 0,  9,  8, 5 },
            new int[] { 0, 0, 12,  8, 8 },
            new int[] { 0, 0,  0, 12, 9 },
            new int[] { 0, 0,  0, 10, 6, 4 },
            new int[] { 0, 0,  0, 12, 8, 5 }
        };

        public double EvalVegas(byte ai_level, byte player, ref int totalTries)
        {
            double score = -10000;
            var cfg = VegasConfig[ai_level - 1];
            if (this.depth >= cfg.Length || GameMode == GameMode.GameOver)
                score = EvalCurrent(player);
            else
            {
                var tried = false;
                var myMove = player == GetPlayer();
                double best = myMove ? double.MinValue : double.MaxValue;

                var moves = GetValidMoves().ToArray();

                byte i = 0;
                if (cfg[depth] != 0)
                    i = (byte)Seed.Next(moves.Length);
                UInt64 usedMoves = 0;
                byte usedMovesNo = 0;
                while (true)
                {
                    if (cfg[depth] != 0)
                        while ((usedMoves & ((UInt64)1 << i)) != 0)
                            i = (byte)Seed.Next(moves.Length);

                    var move = moves[i];
                    double scoreNext = -10000;
                    var next = new Game(ref this);
                    if (next.ApplyMove(move))
                        scoreNext = next.EvalVegas(ai_level, player, ref totalTries);
                    if (scoreNext > -1000)
                    {
                        if ((myMove && best < scoreNext) ||
                            (!myMove && best > scoreNext))
                            best = scoreNext;
                        tried = true;
                        totalTries++;
                    }

                    if (cfg[depth] != 0)
                    {
                        if (++usedMovesNo >= cfg[depth])
                            break;
                    }
                    else if (++i >= moves.Length)
                        break;
                }

                if (!tried)
                    score = EvalCurrent(player);
                else
                    score = best + SmallNoise();
            }
            return score;
        }

        public double EvalCarlos(byte ai_level, byte player, out int tries)
        {
            tries = 0;
            int wins = 0, losses = 0, draws = 0, lastTries = -1;
            var maxTries = ((int)Math.Pow(2.1d, ai_level - 1)) * 100 * 64;
            while (tries < maxTries && lastTries != tries)
            {
                lastTries = tries;
                var g = this;
                while (g.GameMode != GameMode.GameOver)
                {
                    var moves = g.GetValidMoves().ToArray();
                    var move = moves[(byte)Seed.Next(moves.Length)];
                    g = new Game(ref g);
                    if (!g.ApplyMove(move))
                        break;
                    tries++;
                }
                if (g.GameMode != GameMode.GameOver)
                    continue;
                var myScore = g.GetScore(player);
                var otherScore = g.GetScore((byte)(player == 0 ? 1 : 0));
                if (myScore > otherScore)
                    wins++;
                else if (otherScore > myScore)
                    losses++;
                else
                    draws++;
            }
            var games = wins + losses + draws;
            if (games == 0)
                return 0;
            return ((double)wins + draws / 2d) / games;
        }

        public IEnumerable<Move> GetValidMoves()
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
    }
}