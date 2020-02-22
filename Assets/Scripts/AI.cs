using System.Collections.Generic;
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

    public static class AI
    {
        public static float GetCurrentScore(Game game)
        {
            var score = game.GetScore(AI.Player);
            var other = AI.Player == 0 ? 1 : 0;
            return score - game.GetScore((byte)other);
        }

        public delegate bool GenMoveFunction(bool onlyValid, ref Game next, out Move move);

        static void GenNextMove(ref Game game, GenMoveFunction generator,
            ref float bestNext, ref Game best, ref int tries, ref float total)
        {
            var next = new Game(ref game);
            if (generator(true, ref next, out next.AiMove))
            {
                Game nn;
                var val = Eval(ref next, out nn);
                if (game.AiDepth == 0)
                    AI.Moves.Add(next);
                if (val > bestNext || tries == 0)
                {
                    bestNext = val;
                    best = next;
                }
                total += val;
                tries++;
                AI.Tries++;
            }
        }

        static bool RandomGen(bool onlyValid, ref Game next, out Move move) =>
            next.RandomMoveExt(onlyValid, out move);

        static bool AllGen(bool onlyValid, ref Game next, out Move move)
        {
            move.moveType = allMove.moveType;
            move.x = allMove.x;
            move.y = allMove.y;
            move.z = allMove.z;
            return move.Apply(ref next);
        }

        static Move allMove;

        public static float Eval(ref Game game, out Game best)
        {
            game.AiScore = 0;
            best = new Game();
            
            if (game.AiDepth >= AI.Depth || game.GameMode == GameMode.GameOver)
                game.AiScore = game.GetAiScore();
            else
            {
                float total = 0, bestNext = -10000;
                var tries = 0;
                if (game.AiDepth <= 1)
                {
                    if (game.GameMode == GameMode.Add)
                    {
                        for (byte x = 0; x < 4; x++)
                            for (byte y = 0; y < 4; y++)
                                if (game.CanAddStone(x, y))
                                {
                                    allMove.moveType = 0;
                                    allMove.x = x;
                                    allMove.y = y;
                                    allMove.z = 0;
                                    GenNextMove(ref game, AllGen, ref bestNext, ref best, ref tries, ref total);
                                }
                    }
                    else if (game.GameMode == GameMode.Remove)
                    {
                        for (byte rx = 0; rx < 4; rx++)
                            for (byte ry = 0; ry < 4; ry++)
                                for (byte rz = 0; rz < 4; rz++)
                                    if (game.CanRemoveStone(rx, ry, rz))
                                    {
                                        allMove.moveType = 1;
                                        allMove.x = rx;
                                        allMove.y = ry;
                                        allMove.z = rz;
                                        GenNextMove(ref game, AllGen, ref bestNext, ref best, ref tries, ref total);
                                    }
                    }
                }
                else
                    for (int i = 0; i < AI.Width; i++)
                        GenNextMove(ref game, RandomGen, ref bestNext, ref best, ref tries, ref total);
                if (tries == 0)
                    game.AiScore = game.GetAiScore();
                else
                    game.AiScore = total / AI.Width;
            }
            return game.AiScore;
        }

        public static int Width, Depth;
        public static byte Player;
        public static int Tries;
        public static List<Game> Moves = new List<Game>();

        public static void Move(ref Game game, int depth, int width)
        {
            Tries = 0;
            Moves.Clear();
            Width = width;
            Depth = depth;
            Player = game.GetPlayer();

            Game.AiMode = true;
            Game best;
            var score = Eval(ref game, out best);
            Game.AiMode = false;

            if (best.AiDepth > 0)
                best.AiMove.Apply(ref game);
        }
    }
}