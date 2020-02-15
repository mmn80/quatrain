﻿using UnityEngine;

public class Stone : MonoBehaviour
{
    public static bool RotateRandomly = true;

    const float StoneHeight = 0.3f;

    public static Vector3 GetStonePos(int x, int y, int h) =>
        new Vector3(-1.5f + x, StoneHeight / 2 + h * StoneHeight, -1.5f + y);
    
    public static Stone MakeStone(int x, int y, int z)
    {
        var prefab = Game.CurrentPlayer.StoneType == StoneType.White ?
            MainControl.Instance.WhiteStonePrefab : MainControl.Instance.BlackStonePrefab;
        var go = GameObject.Instantiate(prefab, Stone.GetStonePos(x, y, z),
            Quaternion.identity, MainControl.Instance.transform);
        var sc = go.GetComponentInChildren<Stone>();
        sc.Init(x, y, z);
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
        fallToY = GetStonePos(PosX, PosY, Height).y;
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
            if (mouseIsOver && Game.MadeQuatreneThisTurn && Input.GetMouseButtonDown(0))
            {
                if (Highlighted)
                    MainControl.ShowError("can't take from quatrenes");
                else if (Game.LastQuatreneType == StoneType)
                    MainControl.ShowError("can't take your own stone");
                else
                {
                    AudioSource.PlayClipAtPoint(RemoveSound, transform.parent.position);
                    Game.RemoveStone(PosX, PosY, Height);
                }
            }
        }
    }

    bool mouseIsOver;

    void OnMouseEnter()
    {
        mouseIsOver = true;
    }

    void OnMouseExit()
    {
        mouseIsOver = false;
    }
}