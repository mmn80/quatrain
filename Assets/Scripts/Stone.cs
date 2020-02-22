using UnityEngine;

namespace Quatrene
{
    public class Stone : MonoBehaviour
    {
        public static bool RotateRandomly = true;

        const float StoneHeight = 0.3f;

        public static Vector3 GetStonePos(int x, int y, int h) =>
            new Vector3(-1.5f + x, StoneHeight / 2 + h * StoneHeight, -1.5f + y);

        public static Stone MakeStone(int x, int y, int z, StoneType type,
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

        public int PosX { get; private set; }
        public int PosY { get; private set; }
        public int PosZ { get; private set; }

        float normalRotationSpeed;
        bool normalRotationDir;

        public void Init(int x, int y, int z, bool animation, bool sound)
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

            if (mouseIsOver && Input.GetMouseButtonDown(0) &&
                MainControl.game.DoRemoveStone(PosX, PosY, PosZ) &&
                !MainControl.EffectsMuted)
                    AudioSource.PlayClipAtPoint(RemoveSound, transform.parent.position);
        }

        bool mouseIsOver;
        void OnMouseEnter() => mouseIsOver = true;
        void OnMouseExit() => mouseIsOver = false;
    }
}