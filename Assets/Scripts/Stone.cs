using UnityEngine;

public class Stone : MonoBehaviour
{
    public static bool RotateRandomly = true;

    public float RotationSpeed;

    public StoneType StoneType;

    public AudioClip RemoveSound;

    public bool Highlighted { get; set; }

    public int PosX { get; private set; }
    public int PosY { get; private set; }
    public int Height { get; private set; }

    float normalRotationSpeed;
    bool normalRotationDir;

    public void Init(int posX, int posY, int height)
    {
        this.PosX = posX;
        this.PosY = posY;
        this.Height = height;

        normalRotationSpeed = Random.Range(0, RotationSpeed / 10);
        normalRotationDir = Random.Range(0, 2) == 0;

        GetComponent<AudioSource>().Play();
    }

    bool falling;
    float fallToY;
    const float fallSpeed = 2;

    public void FallOneSlot()
    {
        if (Height == 0)
        {
            MainControl.ShowError("Cannot fall any more.");
            return;
        }
        Height -= 1;
        fallToY = Game.GetStonePos(PosX, PosY, Height).y;
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
        if (RotateRandomly || Highlighted)
        {
            var speed = RotationSpeed;
            if (!Highlighted)
                speed = normalRotationSpeed * (normalRotationDir ? 1 : -1);
            transform.parent.Rotate(Vector3.up, speed * Time.deltaTime);
            if (mouseIsOver && Input.GetMouseButtonDown(0))
            {
                AudioSource.PlayClipAtPoint(RemoveSound, transform.parent.position);
                Game.RemoveStone(PosX, PosY, Height);
            }
        }
    }

    bool mouseIsOver;

    void OnMouseEnter()
    {
        Highlighted = true;
        mouseIsOver = true;
    }

    void OnMouseExit()
    {
        Highlighted = false;
        mouseIsOver = true;
    }
}
