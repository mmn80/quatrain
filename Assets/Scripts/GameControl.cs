using System.Collections.Generic;
using System.Linq;
using Quatrene.AI;

namespace Quatrene
{
    public static class Game
    {
        public static GameState state = new GameState();

        static Stone[,,] stones = new Stone[4, 4, 4];

        public static string[] PlayerNames = new string[]
        {
            "Player 1", "Player 2"
        };

        public static bool TakeTopStonesOnly = false;

        public static bool NewGame()
        {
            state = new GameState();

            DestroyAllStones();

            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = Stone.MakeStone(x, y, z,
                            x < 2 ? StoneType.White : StoneType.Black,
                            true, false);

            MainControl.ShowMessage("press <color=#158>N</color> to start new game");
            MainControl.Instance.UpdateUI();

            return true;
        }

        public static bool StartGame()
        {
            state.GameMode = GameMode.Add;

            MainControl.Instance.UpdateUI();
            MainControl.HideMessage();

            DestroyAllStones();

            if (stones == null)
                stones = new Stone[4, 4, 4];
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = null;

            state.RegenerateQuatrains();

            return true;
        }

        static byte NextTurn()
        {
            state.GameMode = GameMode.Add;
            var p = state.SwitchPlayer();
            MainControl.Instance.UpdateUI();

            if (state.GetStones(0) == 0 && state.GetStones(1) == 0)
            {
                byte winner = 2;
                if (state.GetScore(0) > state.GetScore(1))
                    winner = 0;
                else if (state.GetScore(1) > state.GetScore(0))
                    winner = 1;

                GameOver();

                MainControl.Instance.PlayAmenSound();
                MainControl.ShowMessage("game over\nwinner is <color=#D9471A>" +
                    (winner == 2 ? "nobody" : PlayerNames[winner]) +"</color>\n");
                MainControl.Instance.HighlightScore(5, winner);
            }
            return p;
        }

        public static void GameOver()
        {
            state.GameMode = GameMode.GameOver;

            MainControl.Instance.UpdateUI(true);
            MainControl.HideMessage();
        }

        public static bool AddStone(int x, int y)
        {
            MainControl.HideMessage();

            if (state.GameMode != GameMode.Add)
            {
                MainControl.ShowError("invalid move");
                return false;
            }

            var p = state.GetPlayer();
            if (state.GetStones(p) <= 0)
            {
                MainControl.ShowError("no more free stones, next");
                NextTurn();
                return true;
            }

            byte z;
            if (!state.AddStone((byte)x, (byte)y, out z))
            {
                MainControl.ShowError($"Stack [{x},{y}] is full.");
                return false;
            }
            stones[x, y, z] = Stone.MakeStone(x, y, z, (StoneType)p);

            MainControl.Instance.UpdateUI();

            state.RegenerateQuatrains();
            HighlightStones();

            ProcessQuatrains(x, y, z);

            return true;
        }

        public static bool RemoveStone(int x, int y, int z)
        {
            MainControl.HideMessage();

            if (state.GameMode != GameMode.Remove)
            {
                MainControl.ShowError($"invalid move");
                return false;
            }
            var st = state.GetStoneAt((byte)x, (byte)y, (byte)z);
            if (st == Value.None)
            {
                MainControl.ShowError($"There is no stone at [{x},{y},{z}].");
                return false;
            }
            if (state.RemovalType == GameState.Value2Stone(st))
            {
                MainControl.ShowError("can't take your own stone");
                return false;
            }
            if (state.IsQuatrainStone((byte)x, (byte)y, (byte)z))
            {
                MainControl.ShowError("can't take from quatrains");
                return false;
            }
            if (TakeTopStonesOnly && !state.IsTopStone(x, y, z))
            {
                MainControl.ShowError("only top stones can be taken in classic mode");
                return false;
            }

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

            HighlightStones(true);

            state.Scoreed();
            MainControl.Instance.UpdateUI(true);

            state.RegenerateQuatrains();
            HighlightStones();

            ProcessQuatrains(x, y, z, true);

            return true;
        }

        static bool HasStoneToTake()
        {
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                    {
                        var s = state.GetStoneAt(x, y, z);
                        if (s == Value.None)
                            break;
                        if (GameState.Value2Stone(s) != state.RemovalType &&
                            !state.IsQuatrainStone(x, y, z) &&
                            (!TakeTopStonesOnly || state.IsTopStone(x, y, z)))
                                return true;
                    }
            return false;
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
            state.RemovalType = ty;
            var toTake = state.RemovalType == StoneType.White ? "black" : "white";

            state.GameMode = GameMode.Remove;

            if (HasStoneToTake())
            {
                MainControl.ShowMessage($"....QUATRAIN....\ntake a {toTake} stone");
                return;
            }

            var other = (byte)(state.GetPlayer() == 0 ? 1 : 0);
            if (state.GetStones(other) > 0)
            {
                MainControl.ShowMessage($"....QUATRAIN....\nno {toTake} stone on board, taking a free one");
                state.TookStone(other);
                state.Scoreed();
                MainControl.Instance.UpdateUI(true);
            }
            else
                MainControl.ShowMessage($"....QUATRAIN....\nno {toTake} stone to take, next");

            NextTurn();
        }

        static void DestroyAllStones()
        {
            if (stones == null)
                return;
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                    {
                        var s = stones[x, y, z];
                        if (s)
                            Stone.DestroyStone(s);
                    }
        }

        static void HighlightStones(bool reset = false)
        {
            if (stones == null)
                return;
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                    {
                        var s = stones[x, y, z];
                        if (!s)
                            break;
                        if (reset)
                            s.Highlighted = false;
                        else if (state.IsQuatrainStone(x, y, z))
                            s.Highlighted = true;
                    }
        }
    }
}