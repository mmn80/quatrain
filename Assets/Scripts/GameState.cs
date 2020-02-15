using System;

namespace Quatrene.AI
{
    public enum Value { None = 0, White = 1, Black = 2 }

    public class GameState
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
    }
}