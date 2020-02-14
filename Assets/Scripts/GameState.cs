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
        return CurrentPlayer;
    }

    static List<StoneRef>[,] state = new List<StoneRef>[4, 4];

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
            Debug.Log($"Stack [{x},{y}] is full.");
            return false;
        }
        if (CurrentPlayer.Stones <= 0)
        {
            Debug.Log($"No more stone for you!");
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
            Debug.Log($"There is no stone at [{x},{y},{h}].");
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