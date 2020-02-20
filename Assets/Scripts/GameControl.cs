using Quatrene.AI;

namespace Quatrene
{
    public static class Game
    {
        public static GameState state = new GameState();

        static Stone[,,] stones = new Stone[4, 4, 4];

        public static string[] PlayerNames = new string[]
        {
            "Player 1", "Player 2"
        };

        public static bool NewGame()
        {
            state = new GameState();

            DestroyAllStones();

            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    for (int z = 0; z < 4; z++)
                        stones[x, y, z] = Stone.MakeStone(x, y, z,
                            x < 2 ? StoneType.White : StoneType.Black,
                            true, false);

            MainControl.ShowMessage("press <color=#158>N</color> to start new game");
            MainControl.Instance.UpdateUI();

            return true;
        }

        public static bool StartGame()
        {
            state.GameMode = GameMode.Add;

            MainControl.Instance.UpdateUI();
            MainControl.HideMessage();

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
            MainControl.Instance.UpdateUI(true);
            MainControl.HideMessage();
            if (quit)
            {
                MainControl.Instance.PlayGameOverSound();
                MainControl.ShowMessage("game over");
            }
            else
            {
                MainControl.Instance.PlayAmenSound();
                MainControl.ShowMessage("game over\nwinner is <color=#D9471A>" +
                    (winner == 2 ? "nobody" : PlayerNames[winner]) + "</color>\n");
                MainControl.Instance.HighlightScore(5, winner);
            }
        }

        public static void OnPlayerSwitch() => MainControl.Instance.UpdateUI();

        public static void OnAfterAdd(int x, int y, int z)
        {
            stones[x, y, z] = Stone.MakeStone(x, y, z,
                (StoneType)state.GetPlayer());

            MainControl.Instance.UpdateUI();

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
            MainControl.Instance.UpdateUI(true);
            HighlightStones();
        }

        static string ToRemove() => state.ToRemove.ToString().ToLower();

        public static void OnTakeAStone() =>
            MainControl.ShowMessage($"....QUATRAIN....\ntake a {ToRemove()} stone");

        public static void OnTakingFreeStone() =>
            MainControl.ShowMessage($"....QUATRAIN....\nno {ToRemove()} stone on board, taking a free one");

        public static void OnNoStoneToTake() =>
            MainControl.ShowMessage($"....QUATRAIN....\nno {ToRemove()} stone to take, next");

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
                        else if (state.IsQuatrainStone(x, y, z))
                            s.Highlighted = true;
                    }
        }
    }
}