using System.Collections.Generic;
using System.Linq;
using Quatrene.AI;

namespace Quatrene
{
    public class Player
    {
        public string Name;
        public StoneType StoneType;
        public int Stones;
        public int StonesWon;

        public override string ToString() => Name;
    }

    public enum StoneType { White = 0, Black = 1 }

    public enum GameMode { Lobby, Add, Remove, GameOver }

    public static class Game
    {
        public static GameState state = new GameState();

        static Stone[,,] stones = new Stone[4, 4, 4];

        public static bool TakeTopStonesOnly = false;

        public static GameMode Mode { get; private set; }

        public static bool IsPlaying() =>
            Mode != GameMode.Lobby && Mode != GameMode.GameOver;

        public static StoneType RemovalType { get; private set; }
        public static Player CurrentPlayer { get; private set; }

        public static Player Player1 = new Player()
        {
            Name = "Player 1",
            StoneType = StoneType.White,
            Stones = 32
        };
        public static Player Player2 = new Player()
        {
            Name = "Player 2",
            StoneType = StoneType.Black,
            Stones = 32
        };

        public static void SetCurrentPlayer(Player player)
        {
            CurrentPlayer = player;
            MainControl.Instance.UpdateUI();
        }

        public static void GameOver()
        {
            Mode = GameMode.GameOver;

            MainControl.Instance.UpdateUI(true);
            MainControl.HideMessage();
        }

        public static void GoToLobby()
        {
            Mode = GameMode.Lobby;

            foreach (var s in AllStones().ToArray())
                Stone.DestroyStone(s);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = Stone.MakeStone(x, y, z,
                            x < 2 ? StoneType.White : StoneType.Black,
                            true, false);

            MainControl.ShowMessage("press <color=#158>N</color> to start new game");
            MainControl.Instance.UpdateUI();
        }

        public static void NewGame()
        {
            Mode = GameMode.Add;

            Player1.Stones = 32;
            Player1.StonesWon = 0;
            Player2.Stones = 32;
            Player2.StonesWon = 0;
            CurrentPlayer = Player1;

            MainControl.Instance.UpdateUI();
            MainControl.HideMessage();

            foreach (var s in AllStones().ToArray())
                Stone.DestroyStone(s);

            state = new GameState();

            if (stones == null)
                stones = new Stone[4, 4, 4];
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = null;

            state.RegenerateQuatrains();
        }

        public static Player NextTurn()
        {
            Mode = GameMode.Add;
            SetCurrentPlayer(CurrentPlayer == Player1 ? Player2 : Player1);

            if (Player1.Stones == 0 && Player2.Stones == 0)
            {
                Player winner = null;
                if (Player1.StonesWon > Player2.StonesWon)
                    winner = Player1;
                else if (Player2.StonesWon > Player1.StonesWon)
                    winner = Player2;

                GameOver();

                MainControl.Instance.PlayAmenSound();
                MainControl.ShowMessage("game over\nwinner is <color=#D9471A>" +
                    (winner?.Name ?? "nobody") +"</color>\n");
                MainControl.Instance.HighlightScore(5, winner);
            }
            return CurrentPlayer;
        }

        public static bool AddStone(int x, int y)
        {
            MainControl.HideMessage();

            if (CurrentPlayer.Stones <= 0)
            {
                MainControl.ShowError("no more free stones, next");
                NextTurn();
                return false;
            }

            byte z;
            if (!state.AddStone((byte)x, (byte)y, CurrentPlayer.StoneType, out z))
            {
                MainControl.ShowError($"Stack [{x},{y}] is full.");
                return false;
            }
            stones[x, y, z] = Stone.MakeStone(x, y, z, CurrentPlayer.StoneType);

            CurrentPlayer.Stones--;
            MainControl.Instance.UpdateUI();

            state.RegenerateQuatrains();
            HighlightStones();

            ProcessQuatrains(x, y, z);

            return true;
        }

        public static bool RemoveStone(int x, int y, int z)
        {
            MainControl.HideMessage();

            if (!state.RemoveStone((byte)x, (byte)y, (byte)z))
            {
                MainControl.ShowError($"There is no stone at [{x},{y},{z}].");
                return false;
            }

            Stone.DestroyStone(stones[x, y, z]);
            stones[x, y, z] = null;
            for (int i = z; i < 4; i++)
                stones[x, y, i] = i >= 3 ? null :
                    stones[x, y, i + 1];

            for (int i = z; i < 4; i++)
            {
                var stone = stones[x, y, i];
                if (stone == null)
                    break;
                stone.FallOneSlot();
            }

            foreach (var s in AllStones())
                s.Highlighted = false;

            CurrentPlayer.StonesWon += 1;
            MainControl.Instance.UpdateUI(true);

            state.RegenerateQuatrains();
            HighlightStones();

            ProcessQuatrains(x, y, z, true);

            return true;
        }

        static void ProcessQuatrains(int x, int y, int z,
            bool allowAbove = false)
        {
            StoneType ty;
            if (!state.AnyQuatrainAt(x, y, z, allowAbove, out ty))
            {
                NextTurn();
                return;
            }
            RemovalType = ty;
            var toTake = RemovalType == StoneType.White ? "black" : "white";

            Mode = GameMode.Remove;

            foreach (var s in AllStones())
                if (s.StoneType != RemovalType && !s.Highlighted &&
                    (!Game.TakeTopStonesOnly ||
                        Game.state.IsTopStone(s.PosX, s.PosY, s.PosZ)))
                {
                    MainControl.ShowMessage($"....QUATRAIN....\ntake a {toTake} stone");
                    return;
                }

            var other = CurrentPlayer == Player1 ? Player2 : Player1;
            if (other.Stones > 0)
            {
                MainControl.ShowMessage($"....QUATRAIN....\nno {toTake} stone on board, taking a free one");
                other.Stones--;
                CurrentPlayer.StonesWon++;
                MainControl.Instance.UpdateUI(true);
            }
            else
                MainControl.ShowMessage($"....QUATRAIN....\nno {toTake} stone to take, next");

            NextTurn();
        }

        static IEnumerable<Stone> AllStones()
        {
            if (stones == null)
                yield break;
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                    {
                        var s = stones[x, y, z];
                        if (s != null)
                            yield return s;
                    }
        }

        static void HighlightStone(Place p)
        {
            var s = stones[p.X, p.Y, p.Z];
            if (s == null)
                MainControl.ShowError($"No stone at {p}.");
            s.Highlighted = true;
        }

        static void HighlightStones()
        {
            foreach (var q in state.quatrains.Where(q => q.IsFull()))
            {
                HighlightStone(q.P0);
                HighlightStone(q.P1);
                HighlightStone(q.P2);
                HighlightStone(q.P3);
            }
        }
    }
}