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

    public static void ShowInfo(string message)
    {
        var txt = Instance.Info;
        txt.text = message.Replace("\\t", "\t");
    }

    public static void HideInfo() => ShowInfo("");

    public GameObject WhiteStonePrefab;
    public GameObject BlackStonePrefab;

    public Text Player1, Player2;
    public Text Player1Score, Player2Score;
    public Text Messages, Info;

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

    string helpInfo = @"<color=#158>CONTROLS</color>

- <color=#158>A</color> or <color=#158>←</color>\t: rotate camera left
- <color=#158>D</color> or <color=#158>→</color>\t: rotate camera right
- <color=#158>W</color> or <color=#158>↑</color>\t: rotate camera up
- <color=#158>S</color> or <color=#158>↓</color>\t: rotate camera down
- <color=#158>=</color> and <color=#158>-</color>\t: zoom camera

- <color=#158>1</color>\t\t: switch camera between orthographic & perspective
- <color=#158>2</color>\t: switch slow rotation of stones
- <color=#158>3</color>\t: switch between classic & neo game rules

- <color=#158>F1</color>\t: show this help
- <color=#158>F2</color>\t: show credits

- <color=#158>Alt + Q</color>\t: start new game
- <color=#158>Ctrl + Q</color>\t: quit game
";

    string creditsInfo = @"<color=#158>ASSET FLIPS</color>

<color=#158>Textures</color>

- FireBolt Studios - Sci-Fi Texture Pack 1
    <size=14>https://assetstore.unity.com/packages/2d/textures-materials/sci-fi-texture-pack-1-23301</size>

<color=#158>Sounds</color>

- Erokia - Amberque
    <size=14>https://freesound.org/people/Erokia/sounds/460578/</size>
- NenadSimic - Mixed Samples » Menu Selection Click
    <size=14>https://freesound.org/people/NenadSimic/sounds/171697/</size>
- minerjr - BallHit.wav
    <size=14>https://freesound.org/people/minerjr/sounds/89977/</size>
- Electroviolence - Swooshes and Transitions » Swoosh 5
    <size=14>https://freesound.org/people/Electroviolence/sounds/234553/</size>
- Autistic Lucario - Error.wav
    <size=14>https://freesound.org/people/Autistic%20Lucario/sounds/142608/</size>
";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
            Application.Quit();
        else if (Input.GetKeyUp(KeyCode.Q) && Input.GetKey(KeyCode.LeftAlt))
            Game.NewGame();
        else if (Input.GetKeyUp(KeyCode.Alpha2))
            Stone.RotateRandomly = !Stone.RotateRandomly;
        else if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            Game.TakeTopStonesOnly = !Game.TakeTopStonesOnly;
            ShowMessage(Game.TakeTopStonesOnly ?
                "classic mode activated\ncan only take top stones" :
                "neo mode activated\ncan take stones from bellow");
        }
        else if (Input.GetKeyUp(KeyCode.Alpha9))
            Game.ShowQuatrenesDebugInfo = !Game.ShowQuatrenesDebugInfo;
        else if (Input.GetKeyUp(KeyCode.F1))
            ShowInfo(helpInfo);
        else if (Input.GetKeyUp(KeyCode.F2))
            ShowInfo(creditsInfo);
        else if (Input.GetMouseButtonUp(0))
            HideInfo();
    }
}
