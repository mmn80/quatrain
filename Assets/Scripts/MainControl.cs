using UnityEngine;
using UnityEngine.UI;

public class MainControl : MonoBehaviour
{
    public static MainControl Instance { get; private set; }

    public static void ShowMessage(string message, bool error = false, bool reflectInUnity = true)
    {
        var txt = Instance.Messages;
        txt.color = error ? Color.red : new Color(0.4f, 0.4f, 0.4f);
        txt.text = message;
        if (string.IsNullOrEmpty(message) || !reflectInUnity)
            return;
        if (!error)
            Debug.Log(message);
        else
            Debug.LogError(message);
    }

    public static void ShowError(string message) => ShowMessage(message, true);

    public static void HideMessage() => ShowMessage("");

    public GameObject WhiteStonePrefab;
    public GameObject BlackStonePrefab;

    public Text Player1, Player2;
    public Text Player1Score, Player2Score;
    public Text Messages;

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

    void Awake() => Instance = this;

    void Start() => Game.NewGame();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
            Application.Quit();
        else if (Input.GetKeyDown(KeyCode.F6))
            Game.PlaceRandomStones(16);
        else if (Input.GetKeyUp(KeyCode.Alpha2))
            Stone.RotateRandomly = !Stone.RotateRandomly;
        else if (Input.GetKeyUp(KeyCode.Q) && Game.GameOver)
            Game.NewGame();
    }
}
