using UnityEngine;

namespace Quatrain
{
    public class Stone : MonoBehaviour
    {
        public static bool RotateRandomly = true;

        const float StoneHeight = 0.3f;

        static Stone[,,] stones = new Stone[4, 4, 4];

        public static Vector3 GetStonePos(byte x, byte y, byte h) =>
            new Vector3(-1.5f + x, StoneHeight / 2 + h * StoneHeight, -1.5f + y);

        public static void MakeStone(byte x, byte y, byte z, StoneType type,
            bool animation = false, bool sound = true)
        {
            var prefabPath = type == StoneType.White ?
                MainControl.Instance.WhiteStoneVariants[Data.It.Variant] :
                MainControl.Instance.BlackStoneVariants[Data.It.Variant];
            var prefab = MainControl.Load(prefabPath);
            var pos = GetStonePos(x, y, z);
            if (animation)
                pos.y += StoneHeight * 40 + pos.y * Random.Range(0f, 1f);
            var go = GameObject.Instantiate(prefab,
                pos, Quaternion.identity, MainControl.Instance.transform);
            var sc = go.GetComponentInChildren<Stone>();
            sc.Init(x, y, z, animation, sound);
            stones[x, y, z] = sc;
        }

        public static void DestroyStone(byte x, byte y, byte z,
            bool fallStack = false)
        {
            var s = stones[x, y, z];
            GameObject.Destroy(s.transform.parent.gameObject);
            stones[x, y, z] = null;
            if (fallStack)
            {
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
            }
        }

        public static void DestroyAllStones(bool addStartStones)
        {
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                    {
                        if (stones[x, y, z])
                            DestroyStone(x, y, z);
                        if (addStartStones)
                            MakeStone(x, y, z,
                                x < 2 ? StoneType.White : StoneType.Black,
                                true, false);
                    }
        }

        public static void UpdateStones()
        {
            for (byte x = 0; x < 4; x++)
                for (byte y = 0; y < 4; y++)
                    for (byte z = 0; z < 4; z++)
                    {
                        var s = stones[x, y, z];
                        var g = Data.Current.game.GetStoneAt(x, y, z);
                        if (g == StoneAtPos.None)
                        {
                            if (s != null)
                                DestroyStone(x, y, z);
                            continue;
                        }
                        var gs = Game.StoneAtPos2Stone(g);
                        if (s == null)
                        {
                            MakeStone(x, y, z, gs, false, false);
                            continue;
                        }
                        if (s.StoneType == gs)
                            continue;
                        DestroyStone(x, y, z);
                        MakeStone(x, y, z, gs, false, false);
                    }
        }

        public static void HighlightStones(bool reset = false)
        {
            var last = Data.Current.game.GetLastStone();
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
                        else if (Data.Current.game.IsQuatrainStone(x, y, z))
                            s.Highlighted = true;
                        s.IsLastStone = (last.Stone != 0 &&
                            last.X == x && last.Y == y && last.Z == z);
                    }
        }

        public static void ShowError(string message, byte x, byte y, byte z)
        {
            if (Game.AiMode)
                return;
            var stone = stones[x, y, z];
            if (stone)
                stone.ShowError(message);
        }

        public float RotationSpeed;
        public StoneType StoneType;
        public AudioClip RemoveSound;

        public bool Highlighted { get; set; }
        public bool IsLastStone { get; set; }

        public byte PosX { get; private set; }
        public byte PosY { get; private set; }
        public byte PosZ { get; private set; }

        float normalRotationSpeed;
        bool normalRotationDir;

        void Start()
        {
            mat = GetComponent<MeshRenderer>().material;
            origColor = mat.GetColor("_EmissionColor");
        }

        public void Init(byte x, byte y, byte z, bool animation, bool sound)
        {
            this.PosX = x;
            this.PosY = y;
            this.PosZ = z;

            normalRotationSpeed = Random.Range(0, RotationSpeed / 10);
            normalRotationDir = Random.Range(0, 2) == 0;

            if (sound)
                PlayPlaceSound();
            if (animation)
            {
                fallToY = GetStonePos(PosX, PosY, PosZ).y;
                falling = true;
                fallSpeed = 10;
            }
        }

        public void PlayPlaceSound()
        {
            if (!MainControl.EffectsMuted)
                GetComponents<AudioSource>()[0].Play();
        }

        void PlayErrorSound()
        {
            if (!MainControl.EffectsMuted)
                GetComponents<AudioSource>()[1].Play();
        }

        public void ShowError(string message)
        {
            MainControl.ShowError(message);
            PlayErrorSound();
        }

        bool falling;
        float fallToY;
        float fallSpeed = 2;

        public void FallOneSlot()
        {
            if (PosZ == 0)
            {
                ShowError("Cannot fall any more.");
                return;
            }
            PosZ -= 1;
            fallToY = GetStonePos(PosX, PosY, PosZ).y;
            falling = true;
        }

        Material mat;
        Color origColor;
        bool wasLastStone = false;

        void Update()
        {
            if (falling)
            {
                var pos = transform.parent.position;
                pos.y = Mathf.Max(fallToY, pos.y - Time.deltaTime * fallSpeed);
                if (pos.y <= fallToY)
                    falling = false;
                transform.parent.position = pos;
            }

            if (RotateRandomly || Highlighted || mouseIsOver)
            {
                var speed = RotationSpeed;
                if (!Highlighted && !mouseIsOver)
                    speed = normalRotationSpeed * (normalRotationDir ? 1 : -1);
                transform.parent.Rotate(Vector3.up, speed * Time.deltaTime);
            }

            if (IsLastStone != wasLastStone)
            {
                mat.SetColor("_EmissionColor",
                    IsLastStone ? Color.yellow : origColor);
                wasLastStone = IsLastStone;
            }

            if (mouseIsOver && Input.GetMouseButtonDown(0) &&
                Data.Current.game.RemoveStone(PosX, PosY, PosZ) &&
                !MainControl.EffectsMuted)
                    AudioSource.PlayClipAtPoint(RemoveSound, transform.parent.position);
        }

        bool mouseIsOver;
        void OnMouseEnter() => mouseIsOver = true;
        void OnMouseExit() => mouseIsOver = false;
    }
}