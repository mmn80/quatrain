using System.Collections.Generic;
using UnityEngine;

public enum StoneType { White = 0, Black = 1 }

public class MainControl : MonoBehaviour
{
    public static StoneType CurrentPlayer;

    struct StoneRef
    {
        public StoneType Stone;
        public Stone Obj;
    }

    static List<StoneRef>[,] state = new List<StoneRef>[4,4];

    public static int GetHeight(int x, int y)
    {
        var l = state[x, y];
        if (l == null)
            return 0;
        return l.Count;
    }

    const float StoneHeight = 0.5f;

    public static Vector3 GetStonePos(int x, int y, int h) =>
        new Vector3(-1.5f + x, StoneHeight / 2 + h * StoneHeight, -1.5f + y);

    public static bool AddStone(int x, int y, StoneType stone)
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

        var prefab = stone == StoneType.White ? Instance.WhiteStonePrefab : Instance.BlackStonePrefab;
        var go = GameObject.Instantiate(prefab, GetStonePos(x, y, l.Count), Quaternion.identity, Instance.transform);
        var sc = go.GetComponentInChildren<Stone>();
        sc.Init(x, y, l.Count);

        l.Add(new StoneRef() { Stone = stone, Obj = sc });

        CurrentPlayer = CurrentPlayer == StoneType.White ? StoneType.Black : StoneType.White;

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
            var s = (StoneType)Random.Range(0, 2);
            AddStone(x, y, s);
        }
    }

    public static MainControl Instance { get; private set;}

    public GameObject WhiteStonePrefab;
    public GameObject BlackStonePrefab;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
            Application.Quit();
        else if (Input.GetKeyDown(KeyCode.F6))
            PlaceRandomStones(16);
        else if (Input.GetKeyUp(KeyCode.Alpha2))
            Stone.RotateRandomly = !Stone.RotateRandomly;
    }
}
