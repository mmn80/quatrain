using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

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
        struct Place
        {
            private readonly byte x, y, z;
            private readonly byte stone;

            public Place(byte _x, byte _y, byte _z, byte _stone)
            {
                x = _x; y = _y; z = _z; stone = _stone;
            }

            public Place(Place src, byte _stone)
            {
                x = src.x; y = src.y; z = src.z;
                stone = _stone;
            }

            public byte X { get => x; }
            public byte Y { get => y; }
            public byte Z { get => z; }
            public byte Stone { get => stone; }

            public override string ToString() => $"({x} {y} {z})";

            public void HighlightStone()
            {
                var s = stones[X, Y, Z];
                if (s == null)
                    MainControl.ShowError($"No stone at {this}.");
                s.Highlighted = true;
            }
        }

        struct Quatrene
        {
            private readonly Place p0, p1, p2, p3;

            public Quatrene(Place _p0, Place _p1, Place _p2, Place _p3)
            {
                p0 = _p0; p1 = _p1; p2 = _p2; p3 = _p3;
            }

            public Quatrene(Quatrene src, byte s0, byte s1, byte s2, byte s3)
            {
                p0 = new Place(src.p0, s0);
                p1 = new Place(src.p1, s1);
                p2 = new Place(src.p2, s2);
                p3 = new Place(src.p3, s3);
            }

            public Place P0 { get => p0; }
            public Place P1 { get => p1; }
            public Place P2 { get => p2; }
            public Place P3 { get => p3; }

            public bool IsFull(out StoneType stoneType)
            {
                byte stone = p0.Stone;
                stoneType = stone == 1 ? StoneType.White : StoneType.Black;
                return stone != 0 && p1.Stone == stone && p2.Stone == stone && p3.Stone == stone;
            }

            public bool IsFull()
            {
                StoneType stoneType;
                return IsFull(out stoneType);
            }

            public void HighlightStones()
            {
                P0.HighlightStone();
                P1.HighlightStone();
                P2.HighlightStone();
                P3.HighlightStone();
            }

            public override string ToString() =>
                (p0.Stone == 1 ? "White" : "Black") + $" ({p0} {p1} {p2} {p3})";
        }

        public static AI.GameState state = new AI.GameState();
        static Stone[,,] stones;

        public static bool TakeTopStonesOnly = false;

        public static bool GameOver = true;
        public static bool MadeQuatreneThisTurn = false;

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
                MainControl.Instance.CurrentPlayerChanged();
            }
        }

        public static StoneType LastQuatreneType { get; private set; }

        public static void NewGame()
        {
            GameOver = false;

            Player1.Stones = 32;
            Player1.StonesWon = 0;
            Player2.Stones = 32;
            Player2.StonesWon = 0;
            CurrentPlayer = Player1;

            MainControl.Instance.UpdateScore();
            MainControl.HideMessage();

            foreach (var s in AllStones().ToArray())
                Stone.DestroyStone(s);

            state = new AI.GameState();

            if (stones == null)
                stones = new Stone[4, 4, 4];
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = null;

            RegenerateQuatrenes();
        }

        public static Player FinishTurn()
        {
            CurrentPlayer = CurrentPlayer == Player1 ? Player2 : Player1;
            if (Player1.Stones == 0 && Player2.Stones == 0)
            {
                GameOver = true;
                Player winner = null;
                if (Player1.StonesWon > Player2.StonesWon)
                    winner = Player1;
                else if (Player2.StonesWon > Player1.StonesWon)
                    winner = Player2;
                MainControl.ShowMessage(
                    $"game over\nwinner is {winner?.Name ?? "nobody"}\n" +
                    "alt + q for new game");
                MainControl.Instance.HighlightScore(5, winner);
            }
            return CurrentPlayer;
        }

        static Quatrene[] quatrenesSrc, quatrenes;

        public static bool AddStone(int x, int y)
        {
            MainControl.HideMessage();

            if (CurrentPlayer.Stones <= 0)
            {
                MainControl.ShowError($"No more stone for you!");
                return false;
            }

            byte z;
            if (!state.AddStone((byte)x, (byte)y, CurrentPlayer.StoneType, out z))
            {
                MainControl.ShowError($"Stack [{x},{y}] is full.");
                return false;
            }
            stones[x, y, z] = Stone.MakeStone(x, y, z);

            CurrentPlayer.Stones--;
            MainControl.Instance.UpdateScore();

            RegenerateQuatrenes();
            if (!AnyQuatrenesMadeThisTurn(x, y, z))
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

            RegenerateQuatrenes();
            if (!AnyQuatrenesMadeThisTurn(x, y, z, true))
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

        public static bool IsTopStone(int x, int y, int z)
        {
            var v = state.GetStoneAt((byte)x, (byte)y, (byte)z);
            if (v == AI.Value.None)
                return false;
            if (z >= 3)
                return true;
            v = state.GetStoneAt((byte)x, (byte)y, (byte)(z + 1));
            return v == AI.Value.None;
        }

        #region Quatrene generators

        static ReadOnlyCollection<Vector3Int> orthoDirs =
            System.Array.AsReadOnly(new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, 0, 1)
            });

        static ReadOnlyCollection<Vector3Int> diagDirs =
            System.Array.AsReadOnly(new Vector3Int[]
            {
                new Vector3Int(1, 0, 1), new Vector3Int(1, 0, -1),
                new Vector3Int(0, 1, 1), new Vector3Int(0, 1, -1),
                new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
            });

        static ReadOnlyCollection<Vector3Int> diag2Dirs =
            System.Array.AsReadOnly(new Vector3Int[]
            {
                new Vector3Int(1, 1, 1), new Vector3Int(1, 1, -1),
                new Vector3Int(-1, 1, 1), new Vector3Int(-1, 1, -1)
            });

        #endregion

        public static bool ShowQuatrenesDebugInfo = false;

        static void RegenerateQuatrenes(bool highlightStones = true)
        {
            if (quatrenesSrc == null)
                InitQuatrenesSrc();

            Stopwatch watch = null;
            if (ShowQuatrenesDebugInfo)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            quatrenes = new Quatrene[76];
            for (byte i = 0; i < 76; i++)
            {
                var src = quatrenesSrc[i];
                quatrenes[i] = new Quatrene(src,
                    (byte)state.GetStoneAt(src.P0.X, src.P0.Y, src.P0.Z),
                    (byte)state.GetStoneAt(src.P1.X, src.P1.Y, src.P1.Z),
                    (byte)state.GetStoneAt(src.P2.X, src.P2.Y, src.P2.Z),
                    (byte)state.GetStoneAt(src.P3.X, src.P3.Y, src.P3.Z));
            }

            if (ShowQuatrenesDebugInfo)
            {
                watch.Stop();
                var ms = watch.ElapsedMilliseconds;
                var ts = watch.ElapsedTicks;
                MainControl.ShowMessage($"quatrenes evaluated in {ms} ms ({ts} ticks)");
            }

            if (highlightStones)
                foreach (var q in quatrenes.Where(q => q.IsFull()))
                    q.HighlightStones();
        }

        static void InitQuatrenesSrc()
        {
            byte q_no = 0;
            quatrenesSrc = new Quatrene[76];

            foreach (var dir in orthoDirs)
                for (byte p0 = 0; p0 < 4; p0++)
                    for (byte p1 = 0; p1 < 4; p1++)
                    {
                        var qarr = new Place[4];
                        for (byte i = 0; i < 4; i++)
                        {
                            var x = dir.x == 1 ? i : p0;
                            var y = dir.y == 1 ? i : (dir.x == 1 ? p0 : p1);
                            var z = dir.z == 1 ? i : p1;
                            qarr[i] = new Place(x, y, z, 0);
                        }
                        quatrenesSrc[q_no] = new Quatrene(qarr[0], qarr[1], qarr[2], qarr[3]);
                        q_no++;
                    }

            foreach (var dir in diagDirs)
                for (byte p0 = 0; p0 < 4; p0++)
                {
                    var qarr = new Place[4];
                    byte d_x = (byte)dir.x, d_y = (byte)dir.y, d_z = (byte)dir.z;
                    byte curr_x = 0, curr_y = 0, curr_z = 0;
                    if (dir.x == -1) curr_x = 3;
                    if (dir.y == -1) curr_y = 3;
                    if (dir.z == -1) curr_z = 3;
                    for (byte i = 0; i < 4; i++)
                    {
                        var x = p0;
                        if (dir.x != 0) { x = curr_x; curr_x += d_x; }
                        var y = p0;
                        if (dir.y != 0) { y = curr_y; curr_y += d_y; }
                        var z = p0;
                        if (dir.z != 0) { z = curr_z; curr_z += d_z; }
                        qarr[i] = new Place(x, y, z, 0);
                    }
                    quatrenesSrc[q_no] = new Quatrene(qarr[0], qarr[1], qarr[2], qarr[3]);
                    q_no++;
                }

            foreach (var dir in diag2Dirs)
            {
                var qarr = new Place[4];
                byte d_x = (byte)dir.x, d_y = (byte)dir.y, d_z = (byte)dir.z;
                byte curr_x = 0, curr_y = 0, curr_z = 0;
                if (dir.x == -1) curr_x = 3;
                if (dir.y == -1) curr_y = 3;
                if (dir.z == -1) curr_z = 3;
                for (byte i = 0; i < 4; i++)
                {
                    var x = curr_x; curr_x += d_x;
                    var y = curr_y; curr_y += d_y;
                    var z = curr_z; curr_z += d_z;
                    qarr[i] = new Place(x, y, z, 0);
                }
                quatrenesSrc[q_no] = new Quatrene(qarr[0], qarr[1], qarr[2], qarr[3]);
                q_no++;
            }
        }

        static bool AnyQuatrenesMadeThisTurn(int x, int y, int z, bool z_greater = false)
        {
            MadeQuatreneThisTurn = false;
            LastQuatreneType = CurrentPlayer.StoneType;
            foreach (var q in quatrenes)
            {
                StoneType stoneTy;
                if (q.IsFull(out stoneTy))
                {
                    if ((q.P0.X == x && q.P0.Y == y && (q.P0.Z == z || (z_greater && q.P0.Z > z))) ||
                        (q.P1.X == x && q.P1.Y == y && (q.P1.Z == z || (z_greater && q.P1.Z > z))) ||
                        (q.P2.X == x && q.P2.Y == y && (q.P2.Z == z || (z_greater && q.P2.Z > z))) ||
                        (q.P3.X == x && q.P3.Y == y && (q.P3.Z == z || (z_greater && q.P3.Z > z))))
                    {
                        MadeQuatreneThisTurn = true;
                        LastQuatreneType = stoneTy;
                        break;
                    }
                }
            }
            var toTake = LastQuatreneType == StoneType.White ? "black" : "white";
            if (MadeQuatreneThisTurn)
            {
                bool foundRemovableStone = false;
                foreach (var s in AllStones())
                    if (s.StoneType != LastQuatreneType && !s.Highlighted)
                    {
                        foundRemovableStone = true;
                        break;
                    }
                if (!foundRemovableStone)
                {
                    MadeQuatreneThisTurn = false;
                    var other = CurrentPlayer == Player1 ? Player2 : Player1;
                    if (other.Stones > 0)
                    {
                        MainControl.ShowMessage($"....QUATRENE....\nno {toTake} stone on board, taking a free one");
                        other.Stones--;
                        MainControl.Instance.UpdateScore();
                    }
                    else
                        MainControl.ShowMessage($"....QUATRENE....\nno {toTake} stone to take, next");
                }
            }
            if (MadeQuatreneThisTurn)
                MainControl.ShowMessage($"....QUATRENE....\ntake a {toTake} stone");
            return MadeQuatreneThisTurn;
        }
    }
}