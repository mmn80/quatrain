using UnityEngine;

namespace Quatrene
{
    public class Stone : MonoBehaviour
    {
        public static bool RotateRandomly = true;

        const float StoneHeight = 0.3f;

        public static Vector3 GetStonePos(byte x, byte y, byte h) =>
            new Vector3(-1.5f + x, StoneHeight / 2 + h * StoneHeight, -1.5f + y);

        public static Stone MakeStone(byte x, byte y, byte z, StoneType type,
            bool animation = false, bool sound = true)
        {
            var prefab = type == StoneType.White ?
                MainControl.Instance.WhiteStonePrefab :
                MainControl.Instance.BlackStonePrefab;
            var pos = Stone.GetStonePos(x, y, z);
            if (animation)
                pos.y += Stone.StoneHeight * 40  + pos.y * Random.Range(0f, 1f);
            var go = GameObject.Instantiate(prefab,
                pos, Quaternion.identity, MainControl.Instance.transform);
            var sc = go.GetComponentInChildren<Stone>();
            sc.Init(x, y, z, animation, sound);
            return sc;
        }

        public static void DestroyStone(Stone s)
        {
            GameObject.Destroy(s.transform.parent.gameObject);
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
                MainControl.game.RemoveStone(PosX, PosY, PosZ) &&
                !MainControl.EffectsMuted)
                    AudioSource.PlayClipAtPoint(RemoveSound, transform.parent.position);
        }

        bool mouseIsOver;
        void OnMouseEnter() => mouseIsOver = true;
        void OnMouseExit() => mouseIsOver = false;
    }
}