using System.Collections.Generic;
using UnityEngine;

public enum StoneType { White = 0, Black = 1 }

public class MainControl : MonoBehaviour
{
    public static StoneType CurrentPlayer;

    struct StoneRef
    {
        public StoneType Stone;
        public GameObject Obj;
    }

    static List<StoneRef>[,] state = new List<StoneRef>[4,4];

    public static int GetHeight(int x, int y)
    {
        var l = state[x, y];
        if (l == null)
            return 0;
        return l.Count;
    }

    const float stoneHeight = 0.5f;

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
        var pos = new Vector3(-1.5f + x, stoneHeight / 2 + l.Count * stoneHeight, -1.5f + y);
        var go = GameObject.Instantiate(prefab, pos, Quaternion.identity, Instance.transform);
        go.GetComponentInChildren<Stone>().Init(x, y, l.Count);

        l.Add(new StoneRef() { Stone = stone, Obj = go });

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
        if (l == null || l.Count - 1 != h)
        {
            Debug.Log($"[{x},{y},{h}] is invalid. Can only remove last stone.");
            return false;
        }
        var s = l[h];
        GameObject.Destroy(s.Obj);
        l.RemoveAt(h);

        return true;
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
    }
}
