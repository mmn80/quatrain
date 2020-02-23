using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace Quatrene
{
    public struct GameAiJob : IJobParallelFor
    {
        public byte depth, width;
        public Game game;
        public byte player;
        public NativeArray<Move> move;
        public NativeArray<int> tries;
        public NativeArray<AiValue> result;

        public void Execute(int i)
        {
            var tempStats = new AiStats(0);
            var next = new Game(ref game);
            next.aiValue.Move = move[i];
            if (next.ApplyAiMove())
            {
                var nextVal = next.aiValue;
                next.Eval(depth, width, player, ref tempStats);
                nextVal.Score = next.aiValue.Score;
                if (game.AiDepth == 0)
                    tempStats.Moves.Add(nextVal);
                result[i] = nextVal;
            }
            else
                result[i] = game.aiValue;
            tries[i] = tempStats.Tries;
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

        static byte Rnd4() => (byte)Seed.Next(4);

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

        public void TryMove(Move move, ref AiStats stats, ref float total, ref byte tries,
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

                foreach (var move in GetNextMoves(width))
                    TryMove(move, ref stats, ref total, ref tries,
                        depth, width, player);

                if (tries == 0)
                    aiValue.Score = GetAiScore(player);
                else
                    aiValue.Score = total / tries;
            }
        }

        IEnumerable<Move> GetNextMoves(byte width)
        {
            if (AiDepth <= 1)
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
                    for (byte rx = 0; rx < 4; rx++)
                        for (byte ry = 0; ry < 4; ry++)
                            for (byte rz = 0; rz < 4; rz++)
                                if (CanRemoveStone(rx, ry, rz))
                                    yield return new Move(1, rx, ry, rz);
                }
            }
            else
                for (int i = 0; i < width; i++)
                    yield return  new Move(2, 0, 0, 0);
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

            //Eval(depth, width, GetPlayer(), ref AiStats);
            EvalInParallel(depth, width);

            aiTimer.Stop();

            AiMode = false;
            ApplyAiMove();

            if (ShowAiDebugInfo)
                MainControl.ShowAiDebugInfo();
        }

        void EvalInParallel(byte depth, byte width)
        {
            var result = new NativeArray<AiValue>(64, Allocator.Persistent);
            var tries = new NativeArray<int>(64, Allocator.Persistent);
            var movesArr = GetNextMoves(width).ToArray();
            var moves = new NativeArray<Move>(movesArr, Allocator.Persistent);

            var job = new GameAiJob();
            job.game = this;
            job.player = GetPlayer();
            job.depth = depth;
            job.width = width;
            job.move = moves;
            job.result = result;
            job.tries = tries;

            var handle = job.Schedule(movesArr.Length, 2);

            handle.Complete();

            var best = new AiValue()
            {
                Score = -10000,
                Move = new Move(3, 0, 0, 0)
            };
            float total = 0;

            AiStats.Tries = 0;
            for (int i = 0; i < movesArr.Length; i++)
            {
                var res = result[i];
                AiStats.Moves.Add(res);
                AiStats.Tries += tries[i];
                if (best.Score < res.Score)
                    best = res;
                total += res.Score;
            }

            aiValue = best;
            if (movesArr.Length > 0)
                aiValue.Score = total / movesArr.Length;
            else
                aiValue.Score = GetScore(GetPlayer());

            moves.Dispose();
            result.Dispose();
            tries.Dispose();
        }

        public static Stopwatch aiTimer;
    }
}