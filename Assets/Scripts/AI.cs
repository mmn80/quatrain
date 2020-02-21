namespace Quatrene
{
    public class Move
    {
        public byte moveType;
        public byte x, y, z;
        public float score;
        public byte depth;
        public Game game;
        public Move best;

        public override string ToString() =>
            (moveType == 0 ? "add" : "remove") + $" {x} {y} {z}";


        public float GetCurrentScore()
        {
            var score = game.GetScore(AI.Player);
            var other = AI.Player == 0 ? 1 : 0;
            return score - game.GetScore((byte)other);
        }

        public float Eval()
        {
            score = 0;
            
            if (depth >= AI.Depth || game.GameMode == GameMode.GameOver)
                score = GetCurrentScore();
            else
            {
                float total = 0, bestNext = 0;
                best = null;
                var tries = 0;
                for (int i = 0; i < AI.Width; i++)
                {
                    var next = new Game(game);
                    var move = new Move();
                    move.depth = (byte)(depth + 1);
                    move.game = next;
                    if (next.RandomMoveExt(true, out move.moveType,
                        out move.x, out move.y, out move.z))
                    {
                        var val = move.Eval();
                        if (val > bestNext || tries == 0)
                        {
                            bestNext = val;
                            best = move;
                        }
                        total += val;
                        tries++;
                    }
                }
                if (tries == 0)
                    score = GetCurrentScore();
                else
                    score = total / AI.Width;
            }
            return score;
        }

        public void Apply(Game game)
        {
            if (moveType == 0)
                game.DoAddStone(x, y);
            else
                game.DoRemoveStone(x, y, z);
        }
    }

    public static class AI
    {
        public static int Width, Depth;
        public static byte Player;

        public static Move Move(Game position, int depth, int width)
        {
            Game.AiMode = true;
            Width = width;
            Depth = depth;
            Player = position.GetPlayer();
            var move = new Move();
            move.game = position;
            var score = move.Eval();
            Game.AiMode = false;

            UnityEngine.Debug.Log($"Value of current position is {score}.");
            if (move.best != null)
            {
                UnityEngine.Debug.Log($"Best move: {move.best}.");
                move.best.Apply(position);
            }
            return move.best;
        }
    }
}