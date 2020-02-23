using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UnityEngine;

namespace Quatrene
{
    public enum GameMode { Lobby = 0, Add = 1, Remove = 2, GameOver = 3 }
    public enum StoneType { White = 0, Black = 1 }
    public enum StoneAtPos { None = 0, White = 1, Black = 2 }

    public partial struct Game
    {
        static StoneType Value2Stone(StoneAtPos val) =>
            val == StoneAtPos.Black ? StoneType.Black : StoneType.White;

        public static bool TakeTopStonesOnly = true;
        public static bool AiMode = false;

        public Game(ref Game src)
        {
            stones0 = src.stones0;
            stones1 = src.stones1;
            score0 = src.score0;
            score1 = src.score1;
            board0 = src.board0;
            board1 = src.board1;
            board2 = src.board2;
            board3 = src.board3;
            game = src.game;
            quatrainStones = src.quatrainStones;

            aiValue = new AiValue();
            AiDepth = (byte)(src.AiDepth + 1);
        }

        public Game(bool dummy)
        {
            game = 0;
            stones0 = stones1 = 32;
            score0 = score1 = 0;
            board0 = board1 = board2 = board3 = 0;
            quatrainStones = 0;

            aiValue = new AiValue();
            AiDepth = 0;
        }

        byte game;
        byte stones0, stones1;
        byte score0, score1;
        UInt64 board0, board1, board2, board3;
        UInt64 quatrainStones;

        public AiValue aiValue;
        public byte AiDepth;

        public byte GetStones(byte player) => player == 0 ? stones0 : stones1;
        void TookStone(byte player)
        {
            if (player == 0) stones0--; else stones1--;
            if (player == 0) score1++; else score0++;
        }
        public byte GetScore(byte player) => player == 0 ? score0 : score1;
        byte GetMode() => (byte)(game & 0x0F);
        void SetMode(byte m) => game = (byte)(game & 0xF0 | m);
        public byte GetPlayer() => (byte)(game >> 6);
        void SetPlayer(byte p) => game = (byte)(game & 0x3F | (p << 6));
        byte GetToRemove() => (byte)((game >> 4) & 0x3);
        void SetToRemove(byte p) => game = (byte)(game & 0xCF | (p << 4));

        public GameMode GameMode
        {
            get => (GameMode)GetMode();
            set => SetMode((byte)value);
        }

        public void GameOver(bool quit, byte winner)
        {
            GameMode = GameMode.GameOver;
            if (!AiMode)
                MainControl.OnGameOver(quit, winner);
        }

        void NextTurn()
        {
            GameMode = GameMode.Add;
            var p = (byte)(GetPlayer() == 0 ? 1 : 0);
            SetPlayer(p);

            if (!AiMode)
                MainControl.OnPlayerSwitch();

            if (GetStones(0) == 0 && GetStones(1) == 0)
            {
                byte winner = 2;
                if (GetScore(0) > GetScore(1))
                    winner = 0;
                else if (GetScore(1) > GetScore(0))
                    winner = 1;
                GameOver(false, winner);
            }
        }

        public StoneAtPos ToRemove
        {
            get => (StoneAtPos)GetToRemove();
            set => SetToRemove((byte)value);
        }

        StoneAtPos GetStoneAt(byte x, byte y, byte z)
        {
            switch (z)
            {
                case 0: return (StoneAtPos)(byte)
                    (((board0 << (60 - y * 16 - x * 4)) >> 60));
                case 1: return (StoneAtPos)(byte)
                    (((board1 << (60 - y * 16 - x * 4)) >> 60));
                case 2: return (StoneAtPos)(byte)
                    (((board2 << (60 - y * 16 - x * 4)) >> 60));
                case 3: return (StoneAtPos)(byte)
                    (((board3 << (60 - y * 16 - x * 4)) >> 60));
            }
            return 0;
        }

        void SetStoneAt(byte x, byte y, byte z, StoneAtPos v)
        {
            var shift = y * 16 + x * 4;
            var mask = (UInt64)0xF << shift;
            var val = (UInt64)v << shift;
            switch (z)
            {
                case 0:
                    board0 = (board0 & ~mask) | val;
                    break;
                case 1:
                    board1 = (board1 & ~mask) | val;
                    break;
                case 2:
                    board2 = (board2 & ~mask) | val;
                    break;
                case 3:
                    board3 = (board3 & ~mask) | val;
                    break;
            }
        }

        bool AddStoneAt(byte x, byte y, out byte z)
        {
            var p = GetPlayer();
            z = 0;
            while (z < 4)
            {
                if (GetStoneAt(x, y, z) == StoneAtPos.None)
                {
                    SetStoneAt(x, y, z, (StoneAtPos)(p + 1));
                    if (p == 0) stones0--; else stones1--;
                    RegenerateQuatrains();
                    return true;
                }
                z++;
            }
            return false;
        }

        public bool CanAddStone(byte x, byte y)
        {
            if (GameMode != GameMode.Add)
                return false;

            var p = GetPlayer();
            if (GetStones(p) <= 0)
                return true;

            if (GetStoneAt(x, y, 3) == StoneAtPos.None)
                return true;
            return false;
        }

        public bool AddStone(int x, int y)
        {
            if (!AiMode)
                MainControl.HideMessage();

            if (GameMode != GameMode.Add)
                return false;

            var p = GetPlayer();
            if (GetStones(p) <= 0)
            {
                if (!AiMode)
                    MainControl.ShowError("no more free stones, next");
                NextTurn();
                return true;
            }

            byte z;
            if (!AddStoneAt((byte)x, (byte)y, out z))
            {
                if (!AiMode)
                    MainControl.ShowError($"Stack [{x},{y}] is full.");
                return false;
            }

            if (!AiMode)
                MainControl.OnAfterAdd(x, y, z);

            ProcessQuatrains(x, y, z);

            return true;
        }

        bool RemoveStoneAt(byte x, byte y, byte z)
        {
            var s = GetStoneAt(x, y, z);
            if (s == StoneAtPos.None)
                return false;
            if (s == StoneAtPos.Black) score0++; else score1++;
            for (byte i = z; i < 4; i++)
                SetStoneAt(x, y, i, i >= 3 ? StoneAtPos.None :
                    GetStoneAt(x, y, (byte)(i + 1)));
            RegenerateQuatrains();
            return true;
        }

        public bool CanRemoveStone(int x, int y, int z)
        {
            if (GameMode != GameMode.Remove)
                return false;
            var st = GetStoneAt((byte)x, (byte)y, (byte)z);
            if (st == StoneAtPos.None)
            {
                if (!AiMode)
                    MainControl.ShowError($"there is no stone at [{x},{y},{z}]");
                return false;
            }
            if (ToRemove != st)
            {
                if (!AiMode)
                    MainControl.ShowStoneError("can't take your own stone", x, y, z);
                return false;
            }
            if (IsQuatrainStone((byte)x, (byte)y, (byte)z))
            {
                if (!AiMode)
                    MainControl.ShowStoneError("can't take from quatrains", x, y, z);
                return false;
            }
            if (TakeTopStonesOnly && !IsTopStone(x, y, z))
            {
                if (!AiMode)
                    MainControl.ShowStoneError("only top stones can be taken in classic mode", x, y, z);
                return false;
            }
            return true;
        }

        public bool RemoveStone(int x, int y, int z)
        {
            if (!AiMode)
                MainControl.HideMessage();

            if (!CanRemoveStone(x, y, z))
                return false;

            if (!RemoveStoneAt((byte)x, (byte)y, (byte)z))
            {
                if (!AiMode)
                    MainControl.ShowError($"There is no stone at [{x},{y},{z}].");
                return false;
            }

            if (!AiMode)
                MainControl.OnAfterRemove(x, y, z);

            ProcessQuatrains(x, y, z, true);

            return true;
        }

        bool IsTopStone(int x, int y, int z) =>
            IsTopStone((byte)x, (byte)y, (byte)z);

        bool IsTopStone(byte x, byte y, byte z)
        {
            var v = GetStoneAt(x, y, z);
            if (v == StoneAtPos.None)
                return false;
            if (z >= 3)
                return true;
            v = GetStoneAt(x, y, (byte)(z + 1));
            return v == StoneAtPos.None;
        }

        bool HasStoneToTake()
        {
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                    {
                        var s = GetStoneAt(x, y, z);
                        if (s == StoneAtPos.None)
                            break;
                        if (s == ToRemove &&
                            !IsQuatrainStone(x, y, z) &&
                            (!TakeTopStonesOnly || IsTopStone(x, y, z)))
                            return true;
                    }
            return false;
        }

        public void Dump()
        {
            MainControl.ShowMessage(String.Format(
                "#{0:X16}\n#{1:X16}\n#{2:X16}\n#{3:X16}",
                board0, board1, board2, board3));
        }

        public static bool ShowQuatrainsDebugInfo = false;
        public static bool ShowAiDebugInfo = false;

        #region Quatrains generators

        static Quatrain[] quatrainsSrc;

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

        static void InitQuatrainsSrc()
        {
            byte q_no = 0;
            quatrainsSrc = new Quatrain[76];

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
                        quatrainsSrc[q_no] = new Quatrain(qarr[0], qarr[1], qarr[2], qarr[3]);
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
                    quatrainsSrc[q_no] = new Quatrain(qarr[0], qarr[1], qarr[2], qarr[3]);
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
                quatrainsSrc[q_no] = new Quatrain(qarr[0], qarr[1], qarr[2], qarr[3]);
                q_no++;
            }
        }

        #endregion

        void SetQuatrainStone(byte x, byte y, byte z) =>
            quatrainStones |= ((UInt64)1 << (16 * z + 4 * y + x));

        public bool IsQuatrainStone(byte x, byte y, byte z) =>
            (quatrainStones & ((UInt64)1 << (16 * z + 4 * y + x))) != 0;

        void RegenerateQuatrains()
        {
            quatrainStones = 0;
            var quatrains = new Quatrain[76];

            if (quatrainsSrc == null)
                InitQuatrainsSrc();

            Stopwatch watch = null;
            if (ShowQuatrainsDebugInfo && !AiMode)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            for (byte i = 0; i < 76; i++)
            {
                var src = quatrainsSrc[i];
                quatrains[i] = new Quatrain(src,
                    (byte)GetStoneAt(src.P0.X, src.P0.Y, src.P0.Z),
                    (byte)GetStoneAt(src.P1.X, src.P1.Y, src.P1.Z),
                    (byte)GetStoneAt(src.P2.X, src.P2.Y, src.P2.Z),
                    (byte)GetStoneAt(src.P3.X, src.P3.Y, src.P3.Z));
                if (quatrains[i].IsFull())
                {
                    SetQuatrainStone(src.P0.X, src.P0.Y, src.P0.Z);
                    SetQuatrainStone(src.P1.X, src.P1.Y, src.P1.Z);
                    SetQuatrainStone(src.P2.X, src.P2.Y, src.P2.Z);
                    SetQuatrainStone(src.P3.X, src.P3.Y, src.P3.Z);
                }
            }

            if (!AiMode && ShowQuatrainsDebugInfo)
            {
                watch.Stop();
                var ms = watch.ElapsedMilliseconds;
                var ts = watch.ElapsedTicks;
                MainControl.ShowMessage($"quatrains evaluated in {ms} ms ({ts} ticks)");
            }
        }

        bool AnyQuatrainAt(int x, int y, int z, bool allowAbove,
            out StoneType quatrainType)
        {
            quatrainType = StoneType.White;
            while (z < 4)
            {
                if (IsQuatrainStone((byte)x, (byte)y, (byte)z))
                {
                    var s = GetStoneAt((byte)x, (byte)y, (byte)z);
                    quatrainType = Value2Stone(s);
                    return true;
                }
                if (!allowAbove)
                    return false;
                z++;
            }
            return false;
        }

        void ProcessQuatrains(int x, int y, int z,
            bool allowAbove = false)
        {
            StoneType ty;
            if (!AnyQuatrainAt(x, y, z, allowAbove, out ty))
            {
                NextTurn();
                return;
            }

            ToRemove = ty == StoneType.White ? StoneAtPos.Black : StoneAtPos.White;
            GameMode = GameMode.Remove;

            if (HasStoneToTake())
            {
                if (!AiMode)
                    MainControl.OnTakeAStone();
                return;
            }

            var other = (byte)(ToRemove == StoneAtPos.Black ? 1 : 0);
            if (GetStones(other) > 0)
            {
                if (!AiMode)
                    MainControl.OnTakingFreeStone();
                TookStone(other);
                if (!AiMode)
                    MainControl.Instance.UpdateUI(true);
            }
            else if(!AiMode)
                MainControl.OnNoStoneToTake();

            NextTurn();
        }
    }

    public struct Place
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

        public override string ToString() =>
            $"({(stone == 0 ? "-" : (stone == 1 ? "w" : "b"))} {x} {y} {z})";
    }

    public struct Quatrain
    {
        private readonly Place p0, p1, p2, p3;

        public Quatrain(Place _p0, Place _p1, Place _p2, Place _p3)
        {
            p0 = _p0; p1 = _p1; p2 = _p2; p3 = _p3;
        }

        public Quatrain(Quatrain src, byte s0, byte s1, byte s2, byte s3)
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
            return stone != 0 && p1.Stone == stone &&
                p2.Stone == stone && p3.Stone == stone;
        }

        public bool IsFull() => p0.Stone != 0 &&
            p1.Stone == p0.Stone && p2.Stone == p0.Stone && p3.Stone == p0.Stone;

        public override string ToString() => $"({p0} {p1} {p2} {p3})";
    }
}