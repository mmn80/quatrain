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
            var rnd = new System.Random(i);
            if (playerType == PlayerType.Neumann)
            {
                var maxTries = ((int)Math.Pow(2.1d, ai_level - 1)) * 100 * 64;
                byte maxDepth = 2;
                var lastTotalTries = totalTries;
                while (true)
                {
                    results[i] = next.EvalMinimax(maxTries, maxDepth, player,
                        double.MinValue, double.MaxValue,
                        rnd, ref totalTries);
                    maxDepth++;
                    if (totalTries >= maxTries)
                        break;
                    if (lastTotalTries == totalTries)
                        break;
                    lastTotalTries = totalTries;
                }
            }
            else if (playerType == PlayerType.Carlos)
            {
                var maxTries = ((int)Math.Pow(2.1d, ai_level - 1)) * 100 * 64;
                results[i] = next.EvalMonteCarlo(maxTries, player,
                    rnd, out totalTries);
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
        static double AddSmallNoise(System.Random Seed, double number) =>
            number - 0.000000000005d + Seed.NextDouble() * 0.00000000001d;
        static double AddFixedSmallNoise(System.Random Seed, double number) =>
            number - 0.0005d + Seed.NextDouble() * 0.001d;

        double EvalCurrent(System.Random Seed, byte player) =>
            AddSmallNoise(Seed,
            (double)(GetScore(player) - GetScore((byte)(player == 0 ? 1 : 0))));

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

        struct GameComparer : IComparer<Game>
        {
            public GameComparer(byte player, bool maximizing)
            {
                this.player = player;
                this.other = (byte)(player == 0 ? 1 : 0);
                this.maximizing = maximizing;
            }

            byte player, other;
            bool maximizing;

            public int Compare(Game x, Game y)
            {
                var s0 = x.GetScore(player) - x.GetScore(other);
                var s1 = y.GetScore(player) - y.GetScore(other);
                if (s0 == s1)
                    return 0;
                if (maximizing)
                    return s0 > s1 ? -1 : 1;
                else
                    return s0 > s1 ? 1 : -1;
            }
        }

        public double EvalMinimax(int maxTries, byte maxDepth, byte player,
            double alpha, double beta,
            System.Random Seed, ref int totalTries)
        {
            if (depth >= maxDepth || GameMode == GameMode.GameOver ||
                    (totalTries > maxTries && depth >= maxDepth - 1))
                return EvalCurrent(Seed, player);
            else
            {
                var myMove = player == GetPlayer();
                double best = myMove ? double.MinValue : double.MaxValue;
                foreach (var move in GetValidMoves())
                {
                    var next = new Game(ref this);
                    next.ApplyMove(move);
                    var scoreNext = next.EvalMinimax(maxTries, maxDepth, player,
                        alpha, beta, Seed, ref totalTries);
                    scoreNext = AddFixedSmallNoise(Seed, scoreNext);
                    totalTries++;
                    if (myMove)
                    {
                        best = Math.Max(best, scoreNext);
                        alpha = Math.Max(alpha, best);
                        if (alpha >= beta)
                            break;
                    }
                    else
                    {
                        best = Math.Min(best, scoreNext);
                        beta = Math.Min(beta, best);
                        if (alpha >= beta)
                            break;
                    }
                }
                return best;
            }
        }

        public double EvalMonteCarlo(int maxTries, byte player,
            System.Random Seed, out int tries)
        {
            tries = 0;
            int wins = 0, losses = 0, draws = 0, lastTries = -1;
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