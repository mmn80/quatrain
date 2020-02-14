using UnityEngine;

public class Stone : MonoBehaviour
{
    public static bool RotateRandomly = true;

    public float RotationSpeed;

    public StoneType StoneType;

    public AudioClip RemoveSound;

    bool highlighted;

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
            Debug.LogError("Cannot fall any more.");
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
        if (RotateRandomly || highlighted)
        {
            var speed = RotationSpeed;
            if (!highlighted)
                speed = normalRotationSpeed * (normalRotationDir ? 1 : -1);
            transform.parent.Rotate(Vector3.up, speed * Time.deltaTime);
            if (highlighted && Input.GetMouseButtonDown(0))
            {
                AudioSource.PlayClipAtPoint(RemoveSound, transform.parent.position);
                Game.RemoveStone(PosX, PosY, Height);
            }
        }
    }

    void OnMouseEnter()
    {
        highlighted = true;
    }

    void OnMouseExit()
    {
        highlighted = false;
    }
}
