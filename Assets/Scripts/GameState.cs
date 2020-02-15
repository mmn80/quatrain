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
    public byte Z { get => z; }
    public byte Stone { get => stone; }

    public override string ToString() => $"({x} {y} {z})";
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

    public override string ToString() =>
        (p0.Stone == 1 ? "White" : "Black") + $" ({p0} {p1} {p2} {p3})";
}

public static class Game
{
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

        foreach (var s in AllStones())
            GameObject.Destroy(s.Obj.transform.parent.gameObject);
        state = new List<StoneRef>[4, 4];
        RegenerateQuatrenes();
    }

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
        if (Player1.Stones == 0 && Player2.Stones == 0)
        {
            GameOver = true;
            var winner = "nobody";
            if (Player1.StonesWon > Player2.StonesWon)
                winner = "Player 1";
            else if (Player2.StonesWon > Player1.StonesWon)
                winner = "Player 2";
            MainControl.ShowMessage($"game over\nwinner is {winner}\nalt + q for new game");
        }
        return CurrentPlayer;
    }

    static List<StoneRef>[,] state = new List<StoneRef>[4, 4];
    static Quatrene[] quatrenes;

    public static bool MadeQuatreneThisTurn = false;

    public static bool GameOver = true;

    static void RegenerateQuatrenes()
    {
        byte q_no = 0;
        quatrenes = new Quatrene[76];
        var orthoDirs = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, 0, 1)
        };
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
                        var stack = state[x, y];
                        byte stone = 0;
                        if (stack?.Count > z)
                            stone = (byte)(stack[z].Stone == StoneType.White ? 1 : 2);
                        qarr[i] = new Place(x, y, z, stone);
                    }
                    quatrenes[q_no] = new Quatrene(qarr[0], qarr[1], qarr[2], qarr[3]);
                    q_no++;
                }
        var diagDirs = new Vector3Int[]
        {
            new Vector3Int(1, 0, 1), new Vector3Int(1, 0, -1),
            new Vector3Int(0, 1, 1), new Vector3Int(0, 1, -1),
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
        };
        foreach (var dir in diagDirs)
            for (byte p0 = 0; p0 < 4; p0++)
            {
                var qarr = new Place[4];
                byte d_x = (byte)dir.x, d_y = (byte)dir.y, d_z = (byte)dir.x;
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
                    var stack = state[x, y];
                    byte stone = 0;
                    if (stack?.Count > z)
                        stone = (byte)(stack[z].Stone == StoneType.White ? 1 : 2);
                    qarr[i] = new Place(x, y, z, stone);
                }
                quatrenes[q_no] = new Quatrene(qarr[0], qarr[1], qarr[2], qarr[3]);
                q_no++;
            }
        var diag2Dirs = new Vector3Int[]
        {
            new Vector3Int(1, 1, 1), new Vector3Int(1, 1, -1),
            new Vector3Int(-1, 1, 1), new Vector3Int(-1, 1, -1)
        };
        foreach (var dir in diag2Dirs)
        {
            var qarr = new Place[4];
            byte d_x = (byte)dir.x, d_y = (byte)dir.y, d_z = (byte)dir.x;
            byte curr_x = 0, curr_y = 0, curr_z = 0;
            if (dir.x == -1) curr_x = 3;
            if (dir.y == -1) curr_y = 3;
            if (dir.z == -1) curr_z = 3;
            for (byte i = 0; i < 4; i++)
            {
                var x = curr_x; curr_x += d_x;
                var y =  curr_y; curr_y += d_y;
                var z = curr_z; curr_z += d_z;
                var stack = state[x, y];
                byte stone = 0;
                if (stack?.Count > z)
                    stone = (byte)(stack[z].Stone == StoneType.White ? 1 : 2);
                qarr[i] = new Place(x, y, z, stone);
            }
            quatrenes[q_no] = new Quatrene(qarr[0], qarr[1], qarr[2], qarr[3]);
            q_no++;
        }

        // var msg = "";
        foreach (var q in quatrenes)
        {
            StoneType stone;
            if (q.IsFull(out stone))
            {
                // msg += $"Quatrene: {q}\n";
                HighlightStone(q.P0);
                HighlightStone(q.P1);
                HighlightStone(q.P2);
                HighlightStone(q.P3);
            }
        }
        // if (!string.IsNullOrEmpty(msg))
        //     MainControl.ShowMessage(msg, false, false);
    }

    static void HighlightStone(Place p)
    {
        var stack = state[p.X, p.Y];
        if (stack == null || stack.Count <= p.Z)
            MainControl.ShowError($"Bad stack at {p}");
        var s = stack[p.Z].Obj;
        s.Highlighted = true;
    }

    public static int GetHeight(int x, int y)
    {
        var l = state[x, y];
        if (l == null)
            return 0;
        return l.Count;
    }

    const float StoneHeight = 0.3f;

    public static StoneType CurrentQuatrenePlayer { get; private set; }

    static bool GetMadeQuatreneThisTurn(int x, int y, int z, bool z_greater = false)
    {
        MadeQuatreneThisTurn = false;
        CurrentQuatrenePlayer = CurrentPlayer.StoneType;
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
                    CurrentQuatrenePlayer = stoneTy;
                    break;
                }
            }
        }
        var toTake = CurrentQuatrenePlayer == StoneType.White ? "black" : "white";
        if (MadeQuatreneThisTurn)
        {
            bool foundRemovableStone = false;
            foreach (var s in AllStones())
                if (s.Stone != CurrentQuatrenePlayer && !s.Obj.Highlighted)
                {
                    foundRemovableStone = true;
                    break;
                }
            if (!foundRemovableStone)
            {
                MadeQuatreneThisTurn = false;
                MainControl.ShowMessage($"....QUATRENE....\nbut no free {toTake} stone to take, next");
            }
        }
        if (MadeQuatreneThisTurn)
            MainControl.ShowMessage($"....QUATRENE....\ntake a {toTake} stone");
        return MadeQuatreneThisTurn;
    }

    public static Vector3 GetStonePos(int x, int y, int h) =>
        new Vector3(-1.5f + x, StoneHeight / 2 + h * StoneHeight, -1.5f + y);

    public static bool AddStone(int x, int y)
    {
        MainControl.HideMessage();

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

        RegenerateQuatrenes();

        CurrentPlayer.Stones--;
        MainControl.Instance.UpdateScore();

        if (!GetMadeQuatreneThisTurn(x, y, l.Count - 1))
            ChangePlayer();

        return true;
    }

    public static bool RemoveStone(int x, int y, int h)
    {
        MainControl.HideMessage();

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

        HighlightAllStones(false);

        RegenerateQuatrenes();

        MadeQuatreneThisTurn = false;

        CurrentPlayer.StonesWon += 1;
        MainControl.Instance.UpdateScore();

        if (!GetMadeQuatreneThisTurn(x, y, h, true))
            ChangePlayer();

        return true;
    }

    static IEnumerable<StoneRef> AllStones()
    {
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                foreach (var s in state[x, y] ?? new List<StoneRef>())
                    yield return s;
    }

    public static void HighlightAllStones(bool highlight)
    {
        foreach (var s in AllStones())
            s.Obj.Highlighted = highlight;
    }
}