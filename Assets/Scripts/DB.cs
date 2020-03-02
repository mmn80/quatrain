using System;
using System.Linq;

namespace Quatrain
{
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
        static string GetBasePath() =>
            UnityEngine.Application.persistentDataPath.
                Replace('/', System.IO.Path.DirectorySeparatorChar);

        static string GetSettingsPath(string fileName) =>
            System.IO.Path.Combine(GetBasePath(), fileName);

        public static GameStats FromFile(ref string path)
        {
            try
            {
                if (path == "")
                {
                    var files = System.IO.Directory.GetFiles(
                        GetBasePath(), "game_*.json");
                    if (files.Length > 0)
                    {
                        Array.Sort(files, (f1, f2) =>
                        {
                            f1 = System.IO.Path.GetFileName(f1).Substring(5).
                                Substring(0, fileNameFormat.Length);
                            f2 = System.IO.Path.GetFileName(f2).Substring(5).
                                Substring(0, fileNameFormat.Length);
                            try
                            {
                                var d1 = DateTime.ParseExact(f1, fileNameFormat,
                                    System.Globalization.CultureInfo.InvariantCulture);
                                var d2 = DateTime.ParseExact(f2, fileNameFormat,
                                    System.Globalization.CultureInfo.InvariantCulture);
                                return d1.CompareTo(d2);
                            }
                            catch (System.Exception)
                            {
                                return 0;
                            }
                        });
                        path = files.LastOrDefault();
                    }
                    else
                        MainControl.ShowError("no game saves found at\n" +
                            GetBasePath());
                }
                var json = System.IO.File.ReadAllText(path);
                return UnityEngine.JsonUtility.FromJson<GameStats>(json);
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"Error: {ex.Message}");
                return null;
            }
        }

        public static bool ToFile(GameStats game, ref string path)
        {
            try
            {
                game.Time = DateTime.Now;
                if (path == "")
                    path = GetSettingsPath(game.GetGameName());
                var json = UnityEngine.JsonUtility.ToJson(game);
                System.IO.File.WriteAllText(path, json);
                return true;
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"Error: {ex.Message}");
                return false;
            }
        }

        static string fileNameFormat = "yy-MM-dd_HH-mm-ss";

        public string GetGameName()
        {
            var name = "game_" + Time.ToString(fileNameFormat);
            name += "_" + new string(Player1.Name.Take(10).ToArray());
            name += "_vs_" + new string(Player2.Name.Take(10).ToArray());
            name += ".json";
            return name;
        }

        [NonSerialized]
        public Game game = new Game(true);

        public DateTime Time;
        public PlayerInfo Player1 = new PlayerInfo();
        public PlayerInfo Player2 = new PlayerInfo();

        public PlayerInfo GetCurrentPlayer() =>
            game.GetPlayer() == 0 ? Player1 : Player2;

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
            if (Current < 0 || game.GameMode == GameMode.Lobby)
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
            if (Current < 0 || game.GameMode == GameMode.Lobby)
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
            game = g.Game;
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

    [Serializable]
    public class PlayerInfo
    {
        public string Name;
        public PlayerType Type;

        public override string ToString() => Name;
    }

    [Serializable]
    public class GameInfo
    {
        public DateTime Time;
        public PlayerInfo Player1, Player2;
        public int Moves;
        public bool Finished;
        public int P1Score, P2Score;
    }

    [Serializable]
    public class Settings
    {
        public GameInfo[] Games;
    }
}