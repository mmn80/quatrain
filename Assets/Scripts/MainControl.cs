using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Quatrain
{
    public class MainControl : MonoBehaviour
    {
        public static MainControl Instance { get; private set; }

        public static GameObject Load(string path)
        {
#if UNITY_EDITOR
            var go = UnityEditor.AssetDatabase.
                LoadAssetAtPath<GameObject>(path);
#else
            const string BASE_PATH = "Assets/Resources/";
            if (path.StartsWith(BASE_PATH))
                path = path.Substring(BASE_PATH.Length);
            var ext = System.IO.Path.GetExtension(path);
            if (ext != "")
                path = path.Substring(0, path.Length - ext.Length);
            GameObject go;
            try
            {
                go = Resources.Load<GameObject>(path);
                if (!go)
                    MainControl.ShowError($"NULL");
            }
            catch (System.Exception ex)
            {
                MainControl.ShowError($"Exception: {ex.Message}");
                throw;
            }
#endif
            return go;
        }

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

        public static void ShowStoneError(string message, byte x, byte y, byte z)
        {
            if (Game.AiMode)
                return;
            var stone = stones[x, y, z];
            if (stone)
                stone.ShowError(message);
        }

        public static void HideMessage() => ShowMessage("");

        public static bool AutoShowAiDebugInfo = false;

        public static void ShowInfo(string message)
        {
            var txt = Instance.Info;
            txt.text = message.Replace("\\t", "\t");
        }

        public static void HideInfo() => ShowInfo("");

        public static bool IsInputOn() => Instance.UserInput.gameObject.activeSelf;

        public static Game game = new Game(true);
        public static GameHistory history = new GameHistory();

        static Stone[,,] stones = new Stone[4, 4, 4];
        static string[] PlayerNames = new string[]
        {
            "Player 1", "Player 2"
        };
        static PlayerType[] PlayerTypes = new PlayerType[]
        {
            PlayerType.Human, PlayerType.Human
        };

        static bool NewGame()
        {
            game = new Game(true);
            history.Clear();

            DestroyAllStones();

            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                        stones[x, y, z] = Stone.MakeStone(x, y, z,
                            x < 2 ? StoneType.White : StoneType.Black,
                            true, false);

            ShowMessage("press <color=#158>H</color> to play against a human\n" +
                "press <color=#158>V</color> or <color=#158>C</color> to play against AI");
            Instance.UpdateUI();

            return true;
        }

        static bool StartGame(PlayerType player1, PlayerType player2)
        {
            history.Clear();

            if (PlayerTypes[0] != player1)
            {
                PlayerTypes[0] = player1;
                PlayerNames[0] = player1.ToString();
                Instance.Player1.text = PlayerNames[0];
            }
            if (PlayerTypes[1] != player2)
            {
                PlayerTypes[1] = player2;
                PlayerNames[1] = player2.ToString();
                Instance.Player2.text = PlayerNames[1];
            }

            game.GameMode = GameMode.Add;
            history.Add(new Position()
            {
                Game = game, Move = new Move(),
                Score = 0, TotalScore = 0,
            });

            Instance.UpdateUI();
            HideMessage();

            DestroyAllStones();

            if (stones == null)
                stones = new Stone[4, 4, 4];
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                        stones[x, y, z] = null;

            return true;
        }

        public static void OnGameOver()
        {
            var winner = game.GetWinner();
            Instance.UpdateUI(true);
            HideMessage();
            if (winner != 2 && (PlayerTypes[winner] == PlayerType.Human || (
                PlayerTypes[0] != PlayerType.Human &&
                PlayerTypes[1] != PlayerType.Human
            )))
                Instance.PlayAmenSound();
            else
                Instance.PlayGameOverSound();
            ShowMessage("game over\nwinner is <color=#D9471A>" +
                (winner == 2 ? "nobody" : PlayerNames[winner]) + "</color>\n");
            if (winner != 2)
                Instance.HighlightScore(5, winner);
        }

        public static void OnPlayerSwitch() => Instance.UpdateUI();

        public static void RefreshStones()
        {
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                    {
                        var s = stones[x, y, z];
                        var g = game.GetStoneAt(x, y, z);
                        if (g == StoneAtPos.None)
                        {
                            if (s != null)
                            {
                                Stone.DestroyStone(s);
                                stones[x, y, z] = null;
                            }
                            continue;
                        }
                        var gs = Game.StoneAtPos2Stone(g);
                        if (s == null)
                        {
                            stones[x, y, z] = Stone.MakeStone(x, y, z, gs);
                            continue;
                        }
                        if (s.StoneType == gs)
                            continue;
                        Stone.DestroyStone(s);
                        stones[x, y, z] = Stone.MakeStone(x, y, z, gs);
                    }
        }

        public static void OnAfterAdd(byte x, byte y, byte z)
        {
            stones[x, y, z] = Stone.MakeStone(x, y, z,
                (StoneType)game.GetPlayer());

            Instance.UpdateUI();

            HighlightStones();
        }

        public static void OnAfterRemove(byte x, byte y, byte z)
        {
            Stone.DestroyStone(stones[x, y, z]);
            stones[x, y, z] = null;
            for (byte i = z; i < 4; i++)
                stones[x, y, i] = i >= 3 ? null :
                    stones[x, y, i + 1];
            for (byte i = z; i < 4; i++)
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
            var last = game.GetLastStone();
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
                        s.IsLastStone = (last.Stone != 0 &&
                            last.X == x && last.Y == y && last.Z == z);
                    }
        }

        IEnumerator AiLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                var player = PlayerTypes[game.GetPlayer()];
                if (!waitingForAi && game.GameMode != GameMode.Lobby &&
                    game.GameMode != GameMode.GameOver &&
                    player != PlayerType.Human && !paused)
                        MakeAiMove(player);
            }
        }

        void MakeAiMove(PlayerType player) =>
            StartCoroutine(MakeAiMoveAsync(player));

        public static bool waitingForAi = false;
        public static System.Diagnostics.Stopwatch aiTimer;
        public static bool paused = false;

        IEnumerator MakeAiMoveAsync(PlayerType player,
            byte depth = 6, byte width = 4, byte playouts = 100)
        {
            if (game.GameMode == GameMode.Lobby || game.GameMode == GameMode.GameOver)
                yield break;

            Game.Seed = new System.Random();
            aiTimer = new System.Diagnostics.Stopwatch();

            Game.AiMode = true;
            aiTimer.Start();

            var movesArr = game.GetValidMoves().ToArray();
            var results = new NativeArray<double>(64, Allocator.Persistent);
            var tries = new NativeArray<int>(64, Allocator.Persistent);
            var moves = new NativeArray<Move>(movesArr, Allocator.Persistent);

            var job = new GameAiJob() { game = game, player = game.GetPlayer(),
                playerType = player, depth = depth, width = width,
                playouts = playouts, moves = moves, results = results,
                tries = tries };
            var handle = job.Schedule(movesArr.Length, 2);

            waitingForAi = true;
            while (waitingForAi)
            {
                yield return new WaitForSecondsRealtime(0.1f);

                if (handle.IsCompleted)
                    waitingForAi = false;
            }
            handle.Complete();

            double total = 0;
            var best = new ScoredMove()
            {
                Score = -10000,
                Move = new Move(2, 0, 0, 0)
            };
            var totalTries = 0;
            var scoredMoves = new List<ScoredMove>();
            for (int i = 0; i < movesArr.Length; i++)
            {
                var result = results[i];
                var val = new ScoredMove()
                {
                    Score = result,
                    Move = movesArr[i]
                };
                scoredMoves.Add(val);
                totalTries += tries[i];
                if (best.Score < result)
                    best = val;
                total += result;
            }

            moves.Dispose();
            results.Dispose();
            tries.Dispose();

            aiTimer.Stop();
            Game.AiMode = false;

            if (paused)
                yield break;
            if (game.GameMode == GameMode.GameOver)
            {
                OnGameOver();
                yield break;
            }

            if ((total == 0 && player == PlayerType.Carlos) || best.Score < -5)
            {
                AiDialog("This is hopeless... I quit.");
                game.GameOver();
                history.Add(new Position());
            }
            else
            {
                bool found;
                var last = history.GetPreviousPosition(game.GetPlayer(), out found);
                if (player == PlayerType.Carlos)
                {
                    if (found && best.Score - last.Score > 0.2 &&
                            best.Move.moveType == 0)
                        AiDialog("I bet you didn't see this comming!");
                }
                else if (player == PlayerType.Vegas)
                {
                    if (found && best.Score - last.Score > 1 &&
                            best.Move.moveType == 0)
                        AiDialog("Gotcha!");
                }
                game.ApplyMove(best.Move);
            }

            history.Update(new Position()
            {
                Game = game, Move = best.Move, Moves = scoredMoves,
                Score = best.Score, TotalScore = total, Tries = totalTries
            });

            if (AutoShowAiDebugInfo)
                history.ShowAiDebugInfo();
        }

        public void AiDialog(string message) =>
            StartCoroutine(AiDialogAsync(message));

        public IEnumerator AiDialogAsync(string message)
        {
            var player = PlayerTypes[game.GetPlayer()];
            var txt = Instance.AiDialogue;
            message = message.Replace("\\t", "\t");
            txt.text = $"<color=#158>{player}:</color> {message}";
            yield return new WaitForSecondsRealtime(3);
            txt.text = "";
        }

        void Awake() => Instance = this;

        void Start()
        {
            camOpts = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            NewGame();
            StartCoroutine(AiLoop());
        }

        public string WhiteStonePath, BlackStonePath;

        public Text Player1, Player2;
        public Text Player1Stones, Player2Stones;
        public Text Player1Score, Player2Score;
        public Text Messages, Info, AiDialogue;

        public InputField UserInput;

        Color selectedPlayer = Color.green;
        Color origPlayer = new Color(0.4f, 0.4f, 0.4f);

        UniversalAdditionalCameraData camOpts;

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

- <color=#158>Ctrl+12</color>\t: rename player 1 (or 2)
- <color=#158>Ctrl+N</color>\t: toggle classic game rules
- <color=#158>Ctrl+R</color>\t: toggle slow rotation of stones
- <color=#158>Ctrl+O</color>\t: toggle orthographic camera mode
- <color=#158>Ctrl+P</color>\t: toggle rendering of post processing effects
- <color=#158>Ctrl+A</color>\t: toggle MSAA
- <color=#158>Ctrl+S</color>\t: toggle rendering of shadows
- <color=#158>Ctrl+M</color>\t: (un)mute music
- <color=#158>Ctrl+E</color>\t: (un)mute effects

- <color=#158>F1</color>\t: show this help
- <color=#158>F2</color>\t: show credits
- <color=#158>F3</color>\t: show AI info
- <color=#158>F5 F6</color>\t: make Vegas or Carlos AI move

- <color=#158>Space</color>: pause AI vs. AI game
- <color=#158>Ctrl+←→</color>: navigate game history

- <color=#158>H</color>\t: start new game against a human
- <color=#158>VC</color>\t: start new game against Vegas or Carlos
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

            if (IsInputOn())
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    UserInput.gameObject.SetActive(false);
                    HideMessage();
                }
                else if (Input.GetKeyUp(KeyCode.Return))
                {
                    PlayerNames[renamingPlayer] = UserInput.text;
                    (renamingPlayer == 0 ? Player1 : Player2).text =
                        UserInput.text;
                    UserInput.gameObject.SetActive(false);
                    HideMessage();
                }
                return;
            }

            var ctrl = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (game.GameMode == GameMode.Lobby)
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                else if (game.GameMode == GameMode.GameOver)
                    NewGame();
                else
                    game.GameOver();
            }
            else if (game.GameMode == GameMode.Lobby &&
                Input.GetKeyUp(KeyCode.H))
                    StartGame(PlayerType.Human, PlayerType.Human);
            else if (game.GameMode == GameMode.Lobby &&
                Input.GetKeyUp(KeyCode.V))
                    StartGame(PlayerType.Human, PlayerType.Vegas);
            else if (game.GameMode == GameMode.Lobby &&
                Input.GetKeyUp(KeyCode.C))
                StartGame(PlayerType.Human, PlayerType.Carlos);
            else if (game.GameMode == GameMode.Lobby &&
                Input.GetKeyUp(KeyCode.X))
                StartGame(PlayerType.Vegas, PlayerType.Carlos);
            else if (ctrl && Input.GetKeyUp(KeyCode.Alpha1))
            {
                renamingPlayer = 0;
                StartRename();
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.Alpha2))
            {
                renamingPlayer = 1;
                StartRename();
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.N))
            {
                Game.TakeTopStonesOnly = !Game.TakeTopStonesOnly;
                ShowMessage(Game.TakeTopStonesOnly ?
                    "classic mode activated\ncan only take top stones" :
                    "neo mode activated\ncan take stones from bellow");
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.R))
                Stone.RotateRandomly = !Stone.RotateRandomly;
            else if (ctrl && Input.GetKeyUp(KeyCode.O))
            {
                CameraControl.Orthographic = !CameraControl.Orthographic;
                if (CameraControl.Orthographic)
                    Camera.main.orthographicSize = 4;
                Camera.main.orthographic = CameraControl.Orthographic;
                MainControl.ShowMessage(CameraControl.Orthographic ?
                    "orthgraphic" : "perspective");
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.P))
            {
                camOpts.renderPostProcessing = !camOpts.renderPostProcessing;
                MainControl.ShowMessage("post processing " +
                    (camOpts.renderPostProcessing ?
                        "enabled" : "disabled"));
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.A))
            {
                Camera.main.allowMSAA = !Camera.main.allowMSAA;
                MainControl.ShowMessage("MSAA " +
                    (Camera.main.allowMSAA ?
                        "enabled" : "disabled"));
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.S))
            {
                camOpts.renderShadows = !camOpts.renderShadows;
                MainControl.ShowMessage("shadows " +
                    (camOpts.renderShadows ?
                        "enabled" : "disabled"));
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.M))
            {
                var m = GetComponents<AudioSource>()[0];
                m.mute = !m.mute;
                MainControl.ShowMessage("music " +
                    (m.mute ? "muted" : "enabled"));
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.E))
            {
                EffectsMuted = !EffectsMuted;
                MainControl.ShowMessage("sound effects " +
                    (EffectsMuted ? "muted" : "enabled"));
            }
            else if (ctrl && Input.GetKeyUp(KeyCode.Alpha0))
            {
                AutoShowAiDebugInfo = !AutoShowAiDebugInfo;
                ShowMessage((AutoShowAiDebugInfo ? "showing" : "hiding") +
                    " AI debug info");
            }
            else if (Input.GetKeyUp(KeyCode.F1))
                ShowInfo(helpInfo);
            else if (Input.GetKeyUp(KeyCode.F2))
                ShowInfo(creditsInfo);
            else if (Input.GetKeyUp(KeyCode.F3))
                history.ShowAiDebugInfo();
            else if (Input.GetKeyUp(KeyCode.F5))
                MakeAiMove(PlayerType.Vegas);
            else if (Input.GetKeyUp(KeyCode.F6))
                MakeAiMove(PlayerType.Carlos);
            else if ((Input.GetKeyUp(KeyCode.LeftArrow) && ctrl) ||
                    Input.GetKeyUp(KeyCode.Backspace))
                history.GoBack();
            else if (Input.GetKeyUp(KeyCode.RightArrow) && ctrl)
                history.GoForward();
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                paused = !paused;
                AiDialog(paused ? "I'm taking a break. Brb." : "I'm back!");
            }
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