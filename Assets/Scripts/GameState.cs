using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UnityEngine;

namespace Quatrene.AI
{
    public enum Value { None = 0, White = 1, Black = 2 }

    public sealed class GameState
    {
        UInt64[] board = new UInt64[] { 0, 0, 0, 0 };

        public void Dump()
        {
            MainControl.ShowMessage(String.Format(
                "#{0:X16}\n#{1:X16}\n#{2:X16}\n#{3:X16}",
                board[0], board[1], board[2], board[3]));
        }

        public Value GetStoneAt(byte x, byte y, byte z) =>
            (Value)(byte)(((board[z] << (60 - y * 16 - x * 4)) >> 60));

        public void SetStoneAt(byte x, byte y, byte z, Value v)
        {
            var shift = y * 16 + x * 4;
            var mask = (UInt64)0xF << shift;
            var val = (UInt64)v << shift;
            board[z] = (board[z] & ~mask) | val;
        }

        public void SetStoneAt(byte x, byte y, byte z, StoneType s) =>
            SetStoneAt(x, y, z, s == StoneType.Black ? Value.Black : Value.White);

        public bool AddStone(byte x, byte y, StoneType s, out byte z)
        {
            z = 0;
            while (z < 4)
            {
                if (GetStoneAt(x, y, z) == Value.None)
                {
                    SetStoneAt(x, y, z, s);
                    return true;
                }
                z++;
            }
            return false;
        }

        public bool RemoveStone(byte x, byte y, byte z)
        {
            if (GetStoneAt(x, y, z) == Value.None)
                return false;
            for (byte i = z; i < 4; i++)
                SetStoneAt(x, y, i, i >= 3 ? Value.None :
                    GetStoneAt(x, y, (byte)(i + 1)));
            return true;
        }

        public bool IsTopStone(int x, int y, int z) =>
            IsTopStone((byte)x, (byte)y, (byte)z);

        public bool IsTopStone(byte x, byte y, byte z)
        {
            var v = GetStoneAt(x, y, z);
            if (v == Value.None)
                return false;
            if (z >= 3)
                return true;
            v = GetStoneAt(x, y, (byte)(z + 1));
            return v == Value.None;
        }

        public static bool ShowQuatrainsDebugInfo = false;

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

        public Quatrain[] quatrains;

        public void RegenerateQuatrains()
        {
            if (quatrainsSrc == null)
                InitQuatrainsSrc();

            Stopwatch watch = null;
            if (ShowQuatrainsDebugInfo)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            quatrains = new Quatrain[76];
            for (byte i = 0; i < 76; i++)
            {
                var src = quatrainsSrc[i];
                quatrains[i] = new Quatrain(src,
                    (byte)GetStoneAt(src.P0.X, src.P0.Y, src.P0.Z),
                    (byte)GetStoneAt(src.P1.X, src.P1.Y, src.P1.Z),
                    (byte)GetStoneAt(src.P2.X, src.P2.Y, src.P2.Z),
                    (byte)GetStoneAt(src.P3.X, src.P3.Y, src.P3.Z));
            }

            if (ShowQuatrainsDebugInfo)
            {
                watch.Stop();
                var ms = watch.ElapsedMilliseconds;
                var ts = watch.ElapsedTicks;
                MainControl.ShowMessage($"quatrains evaluated in {ms} ms ({ts} ticks)");
            }
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

        public override string ToString() => $"({x} {y} {z})";
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

        public bool IsFull()
        {
            StoneType stoneType;
            return IsFull(out stoneType);
        }

        public override string ToString() =>
            (p0.Stone == 1 ? "White" : "Black") + $" ({p0} {p1} {p2} {p3})";
    }
}