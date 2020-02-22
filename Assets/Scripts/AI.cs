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

        public bool Apply(Game game, bool forReals = false)
        {
            if (moveType == 0)
                return forReals ? MainControl.Instance.AddStone(x, y) :
                    game.DoAddStone(x, y);
            else
                return forReals ? MainControl.Instance.RemoveStone(x, y, z) :
                    game.DoRemoveStone(x, y, z);
        }

        public override string ToString() =>
            (moveType == 0 ? $"add {x} {y}" : $"remove {x} {y} {z}");
    }

    public class GameState
    {
        public Move move;
        public float score;
        public byte depth;
        public Game game;
        public GameState best;

        public override string ToString() => move.ToString();

        public float GetCurrentScore()
        {
            var score = game.GetScore(AI.Player);
            var other = AI.Player == 0 ? 1 : 0;
            return score - game.GetScore((byte)other);
        }

        public delegate bool GenMoveFunction(bool onlyValid, Game next, out Move move);

        void GenNextMove(GenMoveFunction generator,
            ref float bestNext, ref int tries, ref float total)
        {
            var next = new Game(game);
            var nextState = new GameState();
            nextState.depth = (byte)(depth + 1);
            nextState.game = next;
            if (generator(true, next, out nextState.move))
            {
                var val = nextState.Eval();
                if (depth == 0)
                    AI.Moves.Add(nextState);
                if (val > bestNext || tries == 0)
                {
                    bestNext = val;
                    best = nextState;
                }
                total += val;
                tries++;
                AI.Tries++;
            }
        }

        static bool RandomGen(bool onlyValid, Game next, out Move move) =>
            next.RandomMoveExt(onlyValid, out move);

        static bool AllGen(bool onlyValid, Game next, out Move move)
        {
            move.moveType = allMove.moveType;
            move.x = allMove.x;
            move.y = allMove.y;
            move.z = allMove.z;
            return move.Apply(next);
        }

        static Move allMove;

        public float Eval()
        {
            score = 0;
            
            if (depth >= AI.Depth || game.GameMode == GameMode.GameOver)
                score = GetCurrentScore();
            else
            {
                float total = 0, bestNext = -10000;
                best = null;
                var tries = 0;
                if (depth <= 1)
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
                                    GenNextMove(AllGen, ref bestNext, ref tries, ref total);
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
                                        GenNextMove(AllGen, ref bestNext, ref tries, ref total);
                                    }
                    }
                }
                else
                    for (int i = 0; i < AI.Width; i++)
                        GenNextMove(RandomGen, ref bestNext, ref tries, ref total);
                if (tries == 0)
                    score = GetCurrentScore();
                else
                    score = total / AI.Width;
            }
            return score;
        }
    }

    public static class AI
    {
        public static int Width, Depth;
        public static byte Player;
        public static int Tries;
        public static List<GameState> Moves = new List<GameState>();

        public static GameState Move(Game game, int depth, int width)
        {
            Tries = 0;
            Moves.Clear();
            Width = width;
            Depth = depth;
            Player = game.GetPlayer();

            var state = new GameState();
            state.game = game;

            Game.AiMode = true;
            var score = state.Eval();
            Game.AiMode = false;

            if (state.best != null)
                state.best.move.Apply(game, true);
            return state.best;
        }
    }
}