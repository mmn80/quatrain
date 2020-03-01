using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace Quatrain
{
    public enum PlayerType { Human, Vegas, Carlos }

    public struct GameAiJob : IJobParallelFor
    {
        public PlayerType playerType;
        public byte depth, width;
        public int playouts;
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
                if (playerType == PlayerType.Vegas)
                    results[i] = next.EvalVegas(depth, width, player, ref totalTries);
                else if (playerType == PlayerType.Carlos)
                    results[i] = next.EvalCarlos(playouts, player, out totalTries);
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

    [Serializable]
    public struct ScoredMove
    {
        public double Score;
        public Move Move;
    }

    [Serializable]
    public struct Position
    {
        public Game Game;
        public Move Move;
        public double Score;
        public double Total;
        public int Tries;
        public ScoredMove[] Moves;
        public long AiMs, AiTicks;
    }

    [Serializable]
    public class GameStats
    {
        static string GetSettingsPath() =>
            System.IO.Path.Combine(UnityEngine.Application.persistentDataPath,
                "settings.json");

        public static GameStats FromFile(string path = "")
        {
            try
            {
                if (path == "")
                    path = GetSettingsPath();
                var json = System.IO.File.ReadAllText(path);
                return UnityEngine.JsonUtility.FromJson<GameStats>(json);
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"Error: {ex.Message}");
                return null;
            }
        }

        public static bool ToFile(GameStats history, string path = "")
        {
            try
            {
                if (path == "")
                    path = GetSettingsPath();
                var json = UnityEngine.JsonUtility.ToJson(history);
                System.IO.File.WriteAllText(path, json);
                return true;
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"Error: {ex.Message}");
                return false;
            }
        }

        public string[] PlayerNames = new string[]
        {
            "Player 1", "Player 2"
        };
        public PlayerType[] PlayerTypes = new PlayerType[]
        {
            PlayerType.Human, PlayerType.Human
        };
        public Position[] History = new Position[0];
        public int Current = -1;

        public void Clear()
        {
            History = new Position[0];
            Current = -1;
        }

        public void Add(Position pos)
        {
            var oldHistory = History;
            History = new Position[History.Length + 1];
            Array.Copy(oldHistory, History, oldHistory.Length);
            History[oldHistory.Length] = pos;
            Current = History.Length - 1;
        }

        public void Update(Position pos)
        {
            History[Current] = pos;
        }

        public Position GetPreviousPosition(byte player, out bool foundIt)
        {
            foundIt = false;
            if (History.Length > 0 && Current == History.Length - 1)
                for (int i = Current; i > 0; i--)
                {
                    var p = History[i];
                    if (p.Game.GetPlayer() != player)
                    {
                        foundIt = true;
                        return p;
                    }
                }
            return new Position();
        }

        public void GoToTheEnd()
        {
            if (MainControl.waitingForAi)
            {
                MainControl.ShowError("wait for ai to finish");
                return;
            }
            if (History.Length == 0)
            {
                MainControl.ShowError("empty game");
                return;
            }
            Current = History.Length - 1;
            UpdateGameFromHistory();
        }

        public void GoBack()
        {
            if (MainControl.waitingForAi)
            {
                MainControl.ShowError("wait for ai to finish");
                return;
            }
            if (Current < 0 || MainControl.game.GameMode == GameMode.Lobby)
            {
                MainControl.ShowError("no live game");
                return;
            }
            if (History.Length == 0 || Current <= 0)
            {
                MainControl.ShowError("back to start");
                return;
            }
            Current--;
            UpdateGameFromHistory();
        }

        public void GoForward()
        {
            if (MainControl.waitingForAi)
            {
                MainControl.ShowError("wait for ai to finish");
                return;
            }
            if (Current < 0 || MainControl.game.GameMode == GameMode.Lobby)
            {
                MainControl.ShowError("no live game");
                return;
            }
            if (History.Length == 0 || Current >= History.Length - 1)
            {
                MainControl.ShowError("already at the end");
                return;
            }
            Current++;
            UpdateGameFromHistory();
        }

        void UpdateGameFromHistory()
        {
            MainControl.paused = true;
            var g = History[Current];
            MainControl.game = g.Game;
            Stone.UpdateStones();
            MainControl.Instance.UpdateUI();
            MainControl.ShowInfo($"<color=#158>Game position:</color> {Current + 1} of {History.Length}");
        }

        static string fstr(double f) => f.ToString("0.000000000000");

        public void ShowAiDebugInfo()
        {
            if (Current == -1)
                return;
            var pos = History[Current];
            if (pos.Moves == null)
                return;
            var bests = pos.Moves.
                OrderByDescending(v => v.Score).
                Take(6).ToArray();
            var best = bests[0];
            var stats = $"<color=#158>Move:</color>\t{best.Move}\n";
            stats += $"<color=#158>Time:</color>\t{pos.AiMs} ms ({pos.AiTicks} ticks)\n";
            stats += $"<color=#158>Score:</color>\t{fstr(best.Score)}\n";
            stats += $"<color=#158>Moves:</color>\t{pos.Tries}\n\n";
            stats += $"<color=#158>Next best moves:</color>\n";
            foreach (var g in bests.Skip(1))
                stats += $"\t{g.Move}  ({fstr(g.Score)})\n";
            MainControl.ShowInfo(stats);
        }
    }

    public partial struct Game
    {
        public static System.Random Seed = new System.Random();
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

        public double EvalVegas(byte depth, byte width, byte player, ref int totalTries)
        {
            double score = -10000;
            if (this.depth >= depth || GameMode == GameMode.GameOver)
                score = EvalCurrent(player);
            else
            {
                byte tries = 0;
                double total = 0;

                var moves = GetValidMoves().ToArray();

                byte i = 0;
                if (this.depth > 1)
                    i = (byte)Seed.Next(moves.Length);
                UInt64 usedMoves = 0;
                byte usedMovesNo = 0;
                while (true)
                {
                    if (this.depth > 1)
                        while ((usedMoves & ((UInt64)1 << i)) != 0)
                            i = (byte)Seed.Next(moves.Length);

                    var move = moves[i];
                    double scoreNext = -10000;
                    var next = new Game(ref this);
                    if (next.ApplyMove(move))
                        scoreNext = next.EvalVegas(depth, width, player, ref totalTries);
                    if (scoreNext > -1000)
                    {
                        total += scoreNext;
                        tries++;
                        totalTries++;
                    }

                    if (this.depth > 1)
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

        public double EvalCarlos(int playouts, byte player, out int tries)
        {
            tries = 0;
            int wins = 0, losses = 0, draws = 0, lastTries = -1;
            while (tries < playouts * 64 && lastTries != tries)
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
            return ((double)wins + draws / 10d) / games;
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