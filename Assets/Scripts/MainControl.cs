using UnityEngine;
using UnityEngine.UI;

namespace Quatrene
{
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

        public static bool IsInputOn() => Instance.UserInput.gameObject.activeSelf;

        public GameObject WhiteStonePrefab;
        public GameObject BlackStonePrefab;

        public Text Player1, Player2;
        public Text Player1Stones, Player2Stones;
        public Text Player1Score, Player2Score;
        public Text Messages, Info;

        public InputField UserInput;

        Color selectedPlayer = Color.green;
        Color origPlayer = new Color(0.4f, 0.4f, 0.4f);

        public void CurrentPlayerChanged()
        {
            Player1.color = Game.CurrentPlayer == Game.Player1 ? selectedPlayer : origPlayer;
            Player2.color = Game.CurrentPlayer == Game.Player2 ? selectedPlayer : origPlayer;
        }

        public void UpdateScore(bool highlight = false)
        {
            Player1Stones.text = MakeRows('○', Game.Player1.Stones);
            Player2Stones.text = MakeRows('●', Game.Player2.Stones);
            Player1Score.text = new System.String('●', Game.Player1.StonesWon);
            Player2Score.text = new System.String('○', Game.Player2.StonesWon);

            if (highlight && highlightScore <= 0)
                HighlightScore();
        }

        public void HighlightScore(float time = 1, Player player = null)
        {
            highlightSpeed = 1 / time;
            highlightScore = 1;
            highlightPlayer = player ?? Game.CurrentPlayer;
        }

        string MakeRows(char c, int no)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i <= no; i++)
            {
                sb.Append(c);
                if (i % 4 == 0)
                    sb.Append('\n');
                if (i % 16 == 0)
                    sb.Append('\n');
            }
            return sb.ToString();
        }

        void Awake() => Instance = this;

        void Start() => Game.NewGame();

        const string helpInfo = @"<color=#158>CONTROLS</color>

- <color=#158>A</color> or <color=#158>←</color>\t: rotate camera left
- <color=#158>D</color> or <color=#158>→</color>\t: rotate camera right
- <color=#158>W</color> or <color=#158>↑</color>\t: rotate camera up
- <color=#158>S</color> or <color=#158>↓</color>\t: rotate camera down
- <color=#158>=</color> and <color=#158>-</color>\t: zoom camera

- <color=#158>1</color> and <color=#158>2</color>\t: rename player 1 & 2
- <color=#158>3</color>\t: classic game rules
- <color=#158>4</color>\t: orthographic camera mode
- <color=#158>5</color>\t: stop slow rotation of stones
- <color=#158>6</color>\t: mute music
- <color=#158>7</color>\t: mute effects

- <color=#158>F1</color>\t: show this help
- <color=#158>F2</color>\t: show credits

- <color=#158>Alt + Q</color>\t: start new game
- <color=#158>Ctrl + Q</color> or <color=#158>Esc</color>\t: quit game
";

        const string creditsInfo = @"<color=#158>ASSET FLIPS</color>

<color=#158>Visuals</color>

- FireBolt Studios - Sci-Fi Texture Pack 1
    <size=14>https://assetstore.unity.com/packages/2d/textures-materials/sci-fi-texture-pack-1-23301</size>
- NimaVisual - Autobus Bold Font
    <size=14>https://www.fontspace.com/nimavisual/autobus-bold</size>

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

        float highlightSpeed = 1;
        float highlightScore = 0;
        Player highlightPlayer;

        static Color highlightColor = Color.green;
        static Color origColor = new Color(0.4f, 0.4f, 0.4f);

        Player renamingPlayer;

        void Update()
        {
            if (highlightScore > 0)
            {
                highlightScore = Mathf.Max(0, highlightScore - highlightSpeed * Time.deltaTime);
                var txt = highlightPlayer == Game.Player1 ?
                    Player1Score : Player2Score;
                txt.color = Color.Lerp(origColor, highlightColor, highlightScore);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsInputOn())
                {
                    UserInput.gameObject.SetActive(false);
                    HideMessage();
                }
                else
                    Application.Quit();
            }
            else if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
                Application.Quit();
            else if (Input.GetKeyUp(KeyCode.Q) && Input.GetKey(KeyCode.LeftAlt))
                Game.NewGame();
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha1))
            {
                renamingPlayer = Game.Player1;
                StartRename();
            }
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha2))
            {
                renamingPlayer = Game.Player2;
                StartRename();
            }
            else if (IsInputOn() && Input.GetKeyUp(KeyCode.Return))
            {
                renamingPlayer.Name = UserInput.text;
                (renamingPlayer == Game.Player1 ? Player1 : Player2).text =
                    renamingPlayer.Name;
                UserInput.gameObject.SetActive(false);
                HideMessage();
            }
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha3))
            {
                Game.TakeTopStonesOnly = !Game.TakeTopStonesOnly;
                ShowMessage(Game.TakeTopStonesOnly ?
                    "classic mode activated\ncan only take top stones" :
                    "neo mode activated\ncan take stones from bellow");
            }
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha5))
                Stone.RotateRandomly = !Stone.RotateRandomly;
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha6))
            {
                var m = GetComponent<AudioSource>();
                m.volume = m.volume > 0.5f ? 0 : 1;
            }
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha7))
                EffectsMuted = !EffectsMuted;
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha8))
                Game.state.Dump();
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha9))
                Game.ShowQuatrenesDebugInfo = !Game.ShowQuatrenesDebugInfo;
            else if (Input.GetKeyUp(KeyCode.F1))
                ShowInfo(helpInfo);
            else if (Input.GetKeyUp(KeyCode.F2))
                ShowInfo(creditsInfo);
            else if (Input.GetMouseButtonUp(0))
                HideInfo();
        }

        public static bool EffectsMuted = false;

        void StartRename()
        {
            ShowMessage($"new name for: {renamingPlayer}\npress ENTER when ready");
            UserInput.text = "";
            UserInput.gameObject.SetActive(true);
            var evs = UnityEngine.EventSystems.EventSystem.current;
            evs.SetSelectedGameObject(UserInput.gameObject, null);
            UserInput.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(evs));
        }
    }
}