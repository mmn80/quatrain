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

        public void UpdatePlayers(bool gameEnd = false)
        {
            if (Game.Playing || gameEnd)
            {
                Player1.color = !gameEnd && Game.CurrentPlayer == Game.Player1 ?
                    selectedPlayer : origPlayer;
                Player2.color = !gameEnd && Game.CurrentPlayer == Game.Player2 ?
                    selectedPlayer : origPlayer;
                if (!Player1.gameObject.activeSelf)
                {
                    Player1.gameObject.SetActive(true);
                    Player2.gameObject.SetActive(true);
                }
            }
            else if (Player1.gameObject.activeSelf)
            {
                Player1.gameObject.SetActive(false);
                Player2.gameObject.SetActive(false);
            }
        }

        public void UpdateScore(bool highlight = false)
        {
            var g = Game.Playing || highlight;
            Player1Stones.text = MakeRows('○', g ? Game.Player1.Stones : 0);
            Player2Stones.text = MakeRows('●', g ? Game.Player2.Stones : 0);
            Player1Score.text = new System.String('●', g ? Game.Player1.StonesWon : 0);
            Player2Score.text = new System.String('○', g ? Game.Player2.StonesWon : 0);

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

        void Start() => Game.GameOver(true);

        const string helpInfo = @"<color=#158>CONTROLS</color>

- <color=#158>WSAD</color> or <color=#158>↑↓←→</color>: rotate camera
- <color=#158>=-</color>\t: zoom camera
- <color=#158>Alt+Enter</color>: toggle full screen

- <color=#158>12</color>\t: rename player 1 & 2
- <color=#158>3</color>\t: toggle classic game rules
- <color=#158>4</color>\t: toggle orthographic camera mode
- <color=#158>5</color>\t: toggle slow rotation of stones
- <color=#158>6</color>\t: (un)mute music
- <color=#158>7</color>\t: (un)mute effects

- <color=#158>F1</color>\t: show this help
- <color=#158>F2</color>\t: show credits

- <color=#158>N</color>\t: start new game
- <color=#158>Esc</color>\t: quit (current) game
";

        const string creditsInfo = @"<color=#158>ASSET FLIPS</color>

- FireBolt Studios - Sci-Fi Texture Pack 1
    <size=14>https://assetstore.unity.com/packages/2d/textures-materials/sci-fi-texture-pack-1-23301</size>
- NimaVisual - Autobus Bold Font
    <size=14>https://www.fontspace.com/nimavisual/autobus-bold</size>
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
- Walter_Odington - Synth Bass » Baz Bass III Short (Nova).aif
    <size=14>https://freesound.org/people/Walter_Odington/sounds/25955/</size>
- Kevcio - Amen Break D (200 BPM)
    <size=14>https://freesound.org/people/Kevcio/sounds/263871/</size>
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
                else if (Game.Playing)
                {
                    Game.GameOver(false);
                    ShowMessage("game over");
                }
                else if (!Game.StonesPacked)
                    Game.GameOver(true);
                else
                    Application.Quit();
            }
            else if (!Game.Playing && Input.GetKeyUp(KeyCode.N))
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
                var m = GetComponents<AudioSource>()[0];
                m.mute = !m.mute;
            }
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha7))
                EffectsMuted = !EffectsMuted;
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha8))
                Game.state.Dump();
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha9))
                AI.GameState.ShowQuatrainsDebugInfo =
                    !AI.GameState.ShowQuatrainsDebugInfo;
            else if (Input.GetKeyUp(KeyCode.F1))
                ShowInfo(helpInfo);
            else if (Input.GetKeyUp(KeyCode.F2))
                ShowInfo(creditsInfo);
            else if (Input.GetMouseButtonUp(0))
                HideInfo();
        }

        public void PlayGameOverSound()
        {
            if (EffectsMuted)
                return;
            var s = GetComponents<AudioSource>()[1];
            s.Play();
        }

        public void PlayAmenSound()
        {
            if (EffectsMuted)
                return;
            var s = GetComponents<AudioSource>()[2];
            s.Play();
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