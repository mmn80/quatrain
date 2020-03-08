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
            next.ApplyMove(moves[i]);
            if (playerType == PlayerType.Neumann)
                results[i] = next.EvalNeumann(ai_level, player, 0, ref totalTries);
            else if (playerType == PlayerType.Carlos)
                results[i] = next.EvalCarlos(ai_level, player, out totalTries);
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

        void ThrowErr(string err)
        {
            MainControl.ShowError(err);
            throw new Exception(err);   
        }

        public void ApplyMove(Move move)
        {
            if (move.moveType == 0)
            {
                if (!AddStone(move.x, move.y))
                    ThrowErr($"Invalid add move: {move}");
            }
            else if (move.moveType == 1)
            {
                if (!RemoveStone(move.x, move.y, move.z))
                    ThrowErr($"Invalid remove move: {move}");
            }
            else
                ThrowErr($"Invalid move type '{move.moveType}' in move {move}.");
        }

        static int[][] nmnCfg = new int[][] {
            new int[] { 0, 0,  5,  4 },
            new int[] { 0, 0,  8,  6 },
            new int[] { 0, 0, 12, 10 },
            new int[] { 0, 0,  0,  4, 4 },
            new int[] { 0, 0,  0,  7, 4 }
        };

        public double EvalNeumann(byte ai_level, byte player, byte fstLevelCredit,
            ref int totalTries)
        {
            var cfg = nmnCfg[ai_level - 1];
            if (depth >= cfg.Length + 1 || GameMode == GameMode.GameOver)
                return EvalCurrent(player);
            else
            {
                var width = cfg[depth - 1];
                var myMove = player == GetPlayer();
                double best = myMove ? double.MinValue : double.MaxValue;

                var moves = GetValidMoves().ToArray();
                if (depth == 1 && GameMode == GameMode.Add)
                    fstLevelCredit = (byte)(Math.Max(0, 16 - moves.Length));

                var maxWidth = width + (depth >= cfg.Length - 2 ?
                    Math.Max(0, Math.Floor(
                        Math.Pow(fstLevelCredit, 1.1d) - 1.0d)) : 0);
                byte i = 0;
                if (width != 0)
                    i = (byte)Seed.Next(moves.Length);
                UInt64 usedMoves = 0;
                byte usedMovesNo = 0;
                while (true)
                {
                    if (width != 0)
                    {
                        while ((usedMoves & ((UInt64)1 << i)) != 0)
                            i = (byte)Seed.Next(moves.Length);
                        usedMoves &= ((UInt64)1 << i);
                    }

                    var move = moves[i];
                    var next = new Game(ref this);
                    next.ApplyMove(move);
                    var scoreNext = next.EvalNeumann(ai_level, player,
                        fstLevelCredit, ref totalTries);
                    if (scoreNext <= -1000)
                        ThrowErr($"Invalid score: {scoreNext}");
                    if ((myMove && best < scoreNext) ||
                        (!myMove && best > scoreNext))
                        best = scoreNext;
                    totalTries++;
                    if (width != 0)
                    {
                        if (++usedMovesNo >= maxWidth)
                            break;
                    }
                    else if (++i >= moves.Length)
                        break;
                }

                return best + SmallNoise();
            }
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
                    g.ApplyMove(move);
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