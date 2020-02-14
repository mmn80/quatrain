using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public StoneType StoneType;
    public int Stones;
    public int StonesWon;
}

public enum StoneType { White = 0, Black = 1 }

struct StoneRef
{
    public StoneType Stone;
    public Stone Obj;
}


struct Place
{
    private readonly byte x, y, z;
    private readonly byte stone;

    public Place(byte _x, byte _y, byte _z, byte _stone)
    {
        x = _x; y = _y; z = _z; stone = _stone;
    }

    public byte X { get => x; }
    public byte Y { get => y; }
    public byte Z { get => Z; }
    public byte Stone { get => stone; }
}

struct Quatrene
{
    private readonly Place p0, p1, p2, p3;

    public Quatrene(Place _p0, Place _p1, Place _p2, Place _p3)
    {
        p0 = _p0; p1 = _p1; p2 = _p2; p3 = _p3;
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
}

public static class Game
{
    public static Player Player1 = new Player() { StoneType = StoneType.White, Stones = 32 };
    public static Player Player2 = new Player() { StoneType = StoneType.Black, Stones = 32 };

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

    public static Player ChangePlayer()
    {
        CurrentPlayer = CurrentPlayer == Player1 ? Player2 : Player1;
        MainControl.HideMessage();
        return CurrentPlayer;
    }

    static List<StoneRef>[,] state = new List<StoneRef>[4, 4];
    static Quatrene[] quatrenes;

    static void RegenerateQuatrenes()
    {
        byte q_no = 0;
        quatrenes = new Quatrene[228];
        var flatDirs = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, 0, 1)
        };
        foreach (var flatDir in flatDirs)
        {
            for (byte p0 = 0; p0 < 4; p0++)
                for (byte p1 = 0; p1 < 4; p1++)
                {
                    var qarr = new Place[4];
                    for (byte i = 0; i < 4; i++)
                    {
                        var x = flatDir.x == 1 ? i : p0;
                        var y = flatDir.y == 1 ? i : (flatDir.x == 1 ? p0 : p1);
                        var z = flatDir.z == 1 ? i : p1;
                        var stack = state[x, y];
                        byte stone = 0;
                        if (stack.Count > z)
                            stone = (byte)(stack[z].Stone == StoneType.White ? 1 : 2);
                        qarr[i] = new Place(x, y, z, stone);
                    }
                    quatrenes[q_no] = new Quatrene(qarr[0], qarr[1], qarr[2], qarr[3]);
                    q_no++;
                }
        }
    }

    public static int GetHeight(int x, int y)
    {
        var l = state[x, y];
        if (l == null)
            return 0;
        return l.Count;
    }

    const float StoneHeight = 0.3f;

    public static Vector3 GetStonePos(int x, int y, int h) =>
        new Vector3(-1.5f + x, StoneHeight / 2 + h * StoneHeight, -1.5f + y);

    public static bool AddStone(int x, int y)
    {
        var l = state[x, y];
        if (l == null)
        {
            l = new List<StoneRef>();
            state[x, y] = l;
        }
        if (l.Count >= 4)
        {
            MainControl.ShowError($"Stack [{x},{y}] is full.");
            return false;
        }
        if (CurrentPlayer.Stones <= 0)
        {
            MainControl.ShowError($"No more stone for you!");
            return false;
        }

        var prefab = CurrentPlayer.StoneType == StoneType.White ?
            MainControl.Instance.WhiteStonePrefab : MainControl.Instance.BlackStonePrefab;
        var go = GameObject.Instantiate(prefab, GetStonePos(x, y, l.Count), Quaternion.identity, MainControl.Instance.transform);
        var sc = go.GetComponentInChildren<Stone>();
        sc.Init(x, y, l.Count);

        l.Add(new StoneRef() { Stone = CurrentPlayer.StoneType, Obj = sc });

        CurrentPlayer.Stones--;
        MainControl.Instance.UpdateScore();

        ChangePlayer();

        return true;
    }

    public static bool RemoveStone(int x, int y, int h)
    {
        var l = state[x, y];
        if (l == null || l.Count <= h)
        {
            MainControl.ShowError($"There is no stone at [{x},{y},{h}].");
            return false;
        }

        var s = l[h];
        GameObject.Destroy(s.Obj.transform.parent.gameObject);
        l.RemoveAt(h);

        for (int i = 0; i < l.Count; i++)
        {
            var stone = l[i].Obj;
            if (stone.Height > i)
                stone.FallOneSlot();
        }

        return true;
    }

    public static void PlaceRandomStones(int stones)
    {
        for (int i = 0; i < stones; i++)
        {
            var x = Random.Range(0, 4);
            var y = Random.Range(0, 4);
            AddStone(x, y);
        }
    }


}