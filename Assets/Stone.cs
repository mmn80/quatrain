using UnityEngine;

public class Stone : MonoBehaviour
{
    public float RotationSpeed;

    public StoneType StoneType;

    bool highlighted;

    int posX, posY, height;

    public void InitPosition(int posX, int posY, int height)
    {
        this.posX = posX;
        this.posY = posY;
        this.height = height;
    }

    void Update()
    {
        if (highlighted)
        {
            transform.parent.Rotate(Vector3.up, RotationSpeed * Time.deltaTime);
            if (Input.GetMouseButtonDown(0))
                MainControl.RemoveStone(posX, posY, height);
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
