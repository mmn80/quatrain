using UnityEngine;
using UnityEngine.UI;

public class MainControl : MonoBehaviour
{
    public static MainControl Instance { get; private set;}

    public GameObject WhiteStonePrefab;
    public GameObject BlackStonePrefab;

    public Text Player1, Player2;
    public Text Player1Score, Player2Score;

    Color selectedPlayer = Color.green;
    Color origPlayer = new Color(0.4f, 0.4f, 0.4f);

    public void CurrentPlayerChanged()
    {
        Player1.color = Game.CurrentPlayer == Game.Player1 ? selectedPlayer : origPlayer;
        Player2.color = Game.CurrentPlayer == Game.Player2 ? selectedPlayer : origPlayer;
    }

    public void UpdateScore()
    {
        Player1Score.text = $"s:{Game.Player1.Stones} w:{Game.Player1.StonesWon}";
        Player2Score.text = $"s:{Game.Player2.Stones} w:{Game.Player2.StonesWon}";
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Game.ChangePlayer();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
            Application.Quit();
        else if (Input.GetKeyDown(KeyCode.F6))
            Game.PlaceRandomStones(16);
        else if (Input.GetKeyUp(KeyCode.Alpha2))
            Stone.RotateRandomly = !Stone.RotateRandomly;
    }
}
