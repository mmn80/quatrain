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
        [NonSerialized]
        public Game game = new Game(true);

        public string Time;
        public Player Player1 = new Player();
        public Player Player2 = new Player();

        public Player GetCurrentPlayer() =>
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
    public class Player
    {
        public Player() {}

        public Player(Player src)
        {
            Name = src.Name;
            Type = src.Type;
        }

        public string Name;
        public PlayerType Type;

        public override string ToString() => Name;
    }

    [Serializable]
    public class GameInfo
    {
        public void UpdateFromGame(GameStats src)
        {
            Player1 = new Player(src.Player1);
            Player2 = new Player(src.Player2);
            Time = src.Time;
            Moves = src.History.Length;
            P1Score = src.game.GetScore(0);
            P2Score = src.game.GetScore(1);
            Finished = src.game.GameMode == GameMode.GameOver;

            Game = src;
        }

        public string FileName;
        public string Time;
        public Player Player1, Player2;
        public int Moves;
        public bool Finished;
        public int P1Score, P2Score;

        [NonSerialized]
        public GameStats Game;

        public override string ToString() =>
            $"{Time} #{Moves}:\t{Player1} ({P1Score}) vs. {Player2} ({P2Score})";
    }

    [Serializable]
    public class Data
    {
        public static Data It;

        public static string GetBasePath() =>
            UnityEngine.Application.persistentDataPath.
                Replace('/', System.IO.Path.DirectorySeparatorChar);

        public static string GetDataFilePath(string fileName) =>
            System.IO.Path.Combine(GetBasePath(), fileName);

        public static void Load()
        {
            try
            {
                var path = GetDataFilePath("Data.json");
                if (System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path);
                    It = UnityEngine.JsonUtility.FromJson<Data>(json);
                }
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"failed loading user data file:\n{ex.Message}");
            }
            if (It == null)
                It = new Data();
        }

        static bool SaveHead()
        {
            try
            {
                var path = GetDataFilePath("Data.json");
                var json = UnityEngine.JsonUtility.ToJson(It);
                System.IO.File.WriteAllText(path, json);
                return true;
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"failed saving user data file:\n{ex.Message}");
                return false;
            }
        }

        public static GameStats Current;

        public static void SaveCurrent()
        {
            var gi = It.Games.FirstOrDefault(g => g.Game == Current);
            if (gi == null)
            {
                gi = new GameInfo();
                gi.FileName = $"game_{It.Games.Length}.json";

                var oldGames = It.Games;
                It.Games = new GameInfo[It.Games.Length + 1];
                Array.Copy(oldGames, It.Games, oldGames.Length);
                It.Games[oldGames.Length] = gi;
            }
            Current.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            gi.UpdateFromGame(Current);

            var path = GetDataFilePath(gi.FileName);
            try
            {
                var json = UnityEngine.JsonUtility.ToJson(Current);
                System.IO.File.WriteAllText(path, json);
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"failed saving game:\n{ex.Message}");
                return;
            }

            if (SaveHead())
                MainControl.ShowMessage("current game saved");
        }

        public static bool LoadGame(GameInfo game)
        {
            try
            {
                var path = GetDataFilePath(game.FileName);
                if (!System.IO.File.Exists(path))
                {
                    MainControl.ShowError($"file not found:\n{path}");
                    return false;
                }
                var json = System.IO.File.ReadAllText(path);
                game.Game = UnityEngine.JsonUtility.FromJson<GameStats>(json);
                if (game.Game == null)
                {
                    MainControl.ShowError($"failed loading game:\n{path}");
                    return false;
                }
                Current = game.Game;
                Current.GoToTheEnd();
                MainControl.ShowMessage("game loaded");
                return true;
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"failed loading game: {ex.Message}");
                return false;
            }
        }

        public static bool gamesListOpened = false;
        static int pageStart, pageEnd, selected;
        const int pageSize = 20;

        public static void ActivateGamesList()
        {
            if (Data.It.Games.Length == 0)
            {
                MainControl.ShowError("no games to load");
                return;
            }
            selected = 0;
            pageStart = 0;
            pageEnd = Math.Min(It.Games.Length - 1, pageSize - 1);
            if (pageEnd >= 0)
            {
                ShowGamesList();
                gamesListOpened = true;
            }
        }

        public static void GamesListMoveDown()
        {
            if (selected >= It.Games.Length - 1)
                return;
            selected++;
            if (selected > pageEnd)
            {
                pageStart++;
                pageEnd++;
            }
            ShowGamesList();
        }

        public static void GamesListMoveUp()
        {
            if (selected <= 0)
                return;
            selected--;
            if (selected < pageStart)
            {
                pageStart--;
                pageEnd--;
            }
            ShowGamesList();
        }

        static void ShowGamesList()
        {
            var str = "<color=#158>select game to load:</color>\n\n";
            for (int i = pageStart; i <= pageEnd; i++)
            {
                var g = It.Games[i];
                if (i == selected)
                    str += $"<color=green>{g}</color>\n";
                else
                    str += $"{g}\n";
            }
            MainControl.ShowInfo(str);
        }

        public static void LoadSelectedGame()
        {
            gamesListOpened = false;
            MainControl.HideInfo();
            if (selected >= 0 && selected < It.Games.Length)
                LoadGame(It.Games[selected]);
        }

        public GameInfo[] Games = new GameInfo[0];
    }
}