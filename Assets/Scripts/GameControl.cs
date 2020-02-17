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

    public static class Game
    {
        public static GameState state = new GameState();

        static Stone[,,] stones;

        public static bool TakeTopStonesOnly = false;

        public static bool Playing { get; private set; }

        public static bool MadeQuatrainThisTurn = false;

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

        static Player _CurrentPlayer;
        public static Player CurrentPlayer
        {
            get => _CurrentPlayer;
            private set
            {
                _CurrentPlayer = value;
                MainControl.Instance.UpdatePlayers();
            }
        }

        public static StoneType LastQuatrainType { get; private set; }

        static void HighlightStone(Place p)
        {
            var s = stones[p.X, p.Y, p.Z];
            if (s == null)
                MainControl.ShowError($"No stone at {p}.");
            s.Highlighted = true;
        }

        static void HighlightStones(Quatrain q)
        {
            HighlightStone(q.P0);
            HighlightStone(q.P1);
            HighlightStone(q.P2);
            HighlightStone(q.P3);
        }

        static void HighlightStones()
        {
            foreach (var q in state.quatrains.Where(q => q.IsFull()))
                HighlightStones(q);
        }

        public static bool StonesPacked = false;

        public static void StopPlaying(bool packStones = false,
            bool won = false)
        {
            Playing = false;

            MainControl.Instance.UpdatePlayers(!packStones);
            MainControl.Instance.UpdateScore(!packStones);
            MainControl.HideMessage();

            if (stones == null)
                stones = new Stone[4, 4, 4];
            if (packStones)
            {
                StonesPacked = true;

                foreach (var s in AllStones().ToArray())
                    Stone.DestroyStone(s);
                for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 4; y++)
                        for (int z = 0; z < 4; z++)
                            stones[x, y, z] = Stone.MakeStone(x, y, z,
                                x < 2 ? StoneType.White : StoneType.Black,
                                true, false);
                MainControl.ShowMessage("press <color=#158>N</color> to start new game");
            }
            else if (won)
                MainControl.Instance.PlayAmenSound();
            else
                MainControl.Instance.PlayGameOverSound();
        }

        public static void NewGame()
        {
            Playing = true;

            Player1.Stones = 32;
            Player1.StonesWon = 0;
            Player2.Stones = 32;
            Player2.StonesWon = 0;
            CurrentPlayer = Player1;

            MainControl.Instance.UpdateScore();
            MainControl.HideMessage();

            StonesPacked = false;
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

        public static Player FinishTurn()
        {
            CurrentPlayer = CurrentPlayer == Player1 ? Player2 : Player1;
            if (Player1.Stones == 0 && Player2.Stones == 0)
            {
                Player winner = null;
                if (Player1.StonesWon > Player2.StonesWon)
                    winner = Player1;
                else if (Player2.StonesWon > Player1.StonesWon)
                    winner = Player2;

                StopPlaying(false, winner != null);

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
                FinishTurn();
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
            MainControl.Instance.UpdateScore();

            state.RegenerateQuatrains();
            HighlightStones();
            if (!AnyQuatrainsMadeThisTurn(x, y, z))
                FinishTurn();

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

            HighlightAllStones(false);
            CurrentPlayer.StonesWon += 1;
            MainControl.Instance.UpdateScore(true);

            state.RegenerateQuatrains();
            HighlightStones();
            if (!AnyQuatrainsMadeThisTurn(x, y, z, true))
                FinishTurn();

            return true;
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

        public static void HighlightAllStones(bool highlight)
        {
            foreach (var s in AllStones())
                s.Highlighted = highlight;
        }

        static bool AnyQuatrainsMadeThisTurn(int x, int y, int z, bool z_greater = false)
        {
            MadeQuatrainThisTurn = false;
            LastQuatrainType = CurrentPlayer.StoneType;
            foreach (var q in state.quatrains)
            {
                StoneType stoneTy;
                if (q.IsFull(out stoneTy))
                {
                    if ((q.P0.X == x && q.P0.Y == y && (q.P0.Z == z || (z_greater && q.P0.Z > z))) ||
                        (q.P1.X == x && q.P1.Y == y && (q.P1.Z == z || (z_greater && q.P1.Z > z))) ||
                        (q.P2.X == x && q.P2.Y == y && (q.P2.Z == z || (z_greater && q.P2.Z > z))) ||
                        (q.P3.X == x && q.P3.Y == y && (q.P3.Z == z || (z_greater && q.P3.Z > z))))
                    {
                        MadeQuatrainThisTurn = true;
                        LastQuatrainType = stoneTy;
                        break;
                    }
                }
            }
            var toTake = LastQuatrainType == StoneType.White ? "black" : "white";
            if (MadeQuatrainThisTurn)
            {
                bool foundRemovableStone = false;
                foreach (var s in AllStones())
                    if (s.StoneType != LastQuatrainType && !s.Highlighted &&
                        (!Game.TakeTopStonesOnly ||
                            Game.state.IsTopStone(s.PosX, s.PosY, s.PosZ)))
                    {
                        foundRemovableStone = true;
                        break;
                    }
                if (!foundRemovableStone)
                {
                    MadeQuatrainThisTurn = false;
                    var other = CurrentPlayer == Player1 ? Player2 : Player1;
                    if (other.Stones > 0)
                    {
                        MainControl.ShowMessage($"....QUATRAIN....\nno {toTake} stone on board, taking a free one");
                        other.Stones--;
                        CurrentPlayer.StonesWon++;
                        MainControl.Instance.UpdateScore(true);
                    }
                    else
                        MainControl.ShowMessage($"....QUATRAIN....\nno {toTake} stone to take, next");
                }
            }
            if (MadeQuatrainThisTurn)
                MainControl.ShowMessage($"....QUATRAIN....\ntake a {toTake} stone");
            return MadeQuatrainThisTurn;
        }
    }
}