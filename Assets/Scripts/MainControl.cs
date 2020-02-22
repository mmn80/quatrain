using System.Collections;
using System.Linq;
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

        public static Game game = new Game(true);

        static Stone[,,] stones = new Stone[4, 4, 4];

        static string[] PlayerNames = new string[]
        {
            "Player 1", "Player 2"
        };

        static bool[] AiPlayers = new bool[]
        {
            false, false
        };

        static bool NewGame()
        {
            game = new Game(true);

            DestroyAllStones();

            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = Stone.MakeStone(x, y, z,
                            x < 2 ? StoneType.White : StoneType.Black,
                            true, false);

            ShowMessage("press <color=#158>H</color> to play against a human\n" +
                "press <color=#158>C</color> to play against the computer");
            Instance.UpdateUI();

            return true;
        }

        static bool StartGame(bool againstAi)
        {
            AiPlayers[0] = false;
            AiPlayers[1] = againstAi;
            if (againstAi)
            {
                if (PlayerNames[0] == "Player 1")
                    PlayerNames[0] = "hombre";
                if (PlayerNames[1] == "Player 2")
                    PlayerNames[1] = "computador";
                Instance.Player1.text = PlayerNames[0];
                Instance.Player2.text = PlayerNames[1];
            }

            game.GameMode = GameMode.Add;

            Instance.UpdateUI();
            HideMessage();

            DestroyAllStones();

            if (stones == null)
                stones = new Stone[4, 4, 4];
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = null;

            return true;
        }

        public static void OnGameOver(bool quit, byte winner)
        {
            Instance.UpdateUI(true);
            HideMessage();
            if (quit)
            {
                Instance.PlayGameOverSound();
                ShowMessage("game over");
            }
            else
            {
                Instance.PlayAmenSound();
                ShowMessage("game over\nwinner is <color=#D9471A>" +
                    (winner == 2 ? "nobody" : PlayerNames[winner]) + "</color>\n");
                Instance.HighlightScore(5, winner);
            }
        }

        public static void OnPlayerSwitch() => Instance.UpdateUI();

        public static void ShowAiDebugInfo()
        {
            var bests = AI.Moves.
                OrderByDescending(s => s.AiScore).
                Take(5).ToArray();
            var best = bests[0];
            var ms = Game.aiTimer.ElapsedMilliseconds;
            var ts = Game.aiTimer.ElapsedTicks;
            var stats = $"<color=#158>Move:</color>\t{best.AiMove}\n";
            stats += $"<color=#158>Time:</color>\t{ms} ms ({ts} ticks)\n";
            stats += $"<color=#158>Score:</color>\t{best.AiScore}\n";
            stats += $"<color=#158>Moves:</color>\t{AI.Tries}\n\n";
            stats += $"<color=#158>Next best moves:</color>\n";
            foreach (var g in bests.Skip(1))
                stats += $"\t{g.AiMove}  ({g.AiScore})\n";
            ShowInfo(stats);
        }

        public static void OnAfterAdd(int x, int y, int z)
        {
            stones[x, y, z] = Stone.MakeStone(x, y, z,
                (StoneType)game.GetPlayer());

            Instance.UpdateUI();

            HighlightStones();
        }

        public static void OnAfterRemove(int x, int y, int z)
        {
            Stone.DestroyStone(stones[x, y, z]);
            stones[x, y, z] = null;
            for (int i = z; i < 4; i++)
                stones[x, y, i] = i >= 3 ? null :
                    stones[x, y, i + 1];
            for (int i = z; i < 4; i++)
            {
                var stone = stones[x, y, i];
                if (stone == null)
                    break;
                stone.FallOneSlot();
            }

            HighlightStones(true);
            Instance.UpdateUI(true);
            HighlightStones();
        }

        static string ToRemove() => game.ToRemove.ToString().ToLower();

        public static void OnTakeAStone() =>
            ShowMessage($"....QUATRAIN....\ntake a {ToRemove()} stone");

        public static void OnTakingFreeStone() =>
            ShowMessage($"....QUATRAIN....\nno {ToRemove()} stone on board, taking a free one");

        public static void OnNoStoneToTake() =>
            ShowMessage($"....QUATRAIN....\nno {ToRemove()} stone to take, next");

        static void DestroyAllStones()
        {
            if (stones == null)
                return;
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                    {
                        var s = stones[x, y, z];
                        if (s)
                            Stone.DestroyStone(s);
                    }
        }

        static void HighlightStones(bool reset = false)
        {
            if (stones == null)
                return;
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                    {
                        var s = stones[x, y, z];
                        if (!s)
                            break;
                        if (reset)
                            s.Highlighted = false;
                        else if (game.IsQuatrainStone(x, y, z))
                            s.Highlighted = true;
                    }
        }


        IEnumerator AiLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                if (game.GameMode != GameMode.Lobby &&
                    game.GameMode != GameMode.GameOver &&
                    AiPlayers[game.GetPlayer()])
                        game.AIMove();
            }
        }

        void Awake() => Instance = this;

        void Start()
        {
            NewGame();
            StartCoroutine(AiLoop());
        }

        public GameObject WhiteStonePrefab;
        public GameObject BlackStonePrefab;

        public Text Player1, Player2;
        public Text Player1Stones, Player2Stones;
        public Text Player1Score, Player2Score;
        public Text Messages, Info;

        public InputField UserInput;

        Color selectedPlayer = Color.green;
        Color origPlayer = new Color(0.4f, 0.4f, 0.4f);

        public void UpdateUI(bool highlight = false)
        {
            if (game.GameMode != GameMode.Lobby)
            {
                var gameEnd = game.GameMode == GameMode.GameOver;
                Player1.color = !gameEnd && game.GetPlayer() == 0 ?
                    selectedPlayer : origPlayer;
                Player2.color = !gameEnd && game.GetPlayer() == 1 ?
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

            var g = game.GameMode == GameMode.Add ||
                game.GameMode == GameMode.Remove || highlight;
            Player1Stones.text = MakeRows('○', g ? game.GetStones(0) : 0);
            Player2Stones.text = MakeRows('●', g ? game.GetStones(1) : 0);
            Player1Score.text = new System.String('●', g ? game.GetScore(0) : 0);
            Player2Score.text = new System.String('○', g ? game.GetScore(1) : 0);

            if (highlight && this.highlightScore <= 0)
                HighlightScore();
        }

        public void HighlightScore(float time = 1, byte player = 2)
        {
            highlightSpeed = 1 / time;
            highlightScore = 1;
            highlightPlayer = player < 2 ? player : game.GetPlayer();
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

- <color=#158>H</color>\t: start new hot seat game
- <color=#158>C</color>\t: start new game against the computer
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
        byte highlightPlayer;

        static Color highlightColor = Color.green;
        static Color origColor = new Color(0.4f, 0.4f, 0.4f);

        byte renamingPlayer;

        void Update()
        {
            if (highlightScore > 0)
            {
                highlightScore = Mathf.Max(0, highlightScore - highlightSpeed * Time.deltaTime);
                var txt = highlightPlayer == 0 ? Player1Score : Player2Score;
                txt.color = Color.Lerp(origColor, highlightColor, highlightScore);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsInputOn())
                {
                    UserInput.gameObject.SetActive(false);
                    HideMessage();
                }
                else if (game.GameMode == GameMode.Lobby)
                    Application.Quit();
                else if (game.GameMode == GameMode.GameOver)
                    NewGame();
                else
                    game.GameOver(true, 2);
            }
            else if (game.GameMode == GameMode.Lobby &&
                    Input.GetKeyUp(KeyCode.H))
                StartGame(false);
            else if (game.GameMode == GameMode.Lobby &&
                    Input.GetKeyUp(KeyCode.C))
                StartGame(true);
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha1))
            {
                renamingPlayer = 0;
                StartRename();
            }
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha2))
            {
                renamingPlayer = 1;
                StartRename();
            }
            else if (IsInputOn() && Input.GetKeyUp(KeyCode.Return))
            {
                PlayerNames[renamingPlayer] = UserInput.text;
                (renamingPlayer == 0 ? Player1 : Player2).text =
                    UserInput.text;
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
                ShowAiDebugInfo();
            else if (!IsInputOn() && Input.GetKeyUp(KeyCode.Alpha9))
            {
                Game.ShowAiDebugInfo = !Game.ShowAiDebugInfo;
                ShowMessage((Game.ShowAiDebugInfo ? "showing" : "hiding") +
                    " AI debug info");
            }
            else if (Input.GetKeyUp(KeyCode.F1))
                ShowInfo(helpInfo);
            else if (Input.GetKeyUp(KeyCode.F2))
                ShowInfo(creditsInfo);
            else if (Input.GetKeyUp(KeyCode.F5))
                game.RandomMove();
            else if (Input.GetKeyUp(KeyCode.F6))
                game.AIMove();
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