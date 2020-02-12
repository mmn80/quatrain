using UnityEngine;

public class Stone : MonoBehaviour
{
    public float RotationSpeed;

    public StoneType StoneType;

    bool highlighted;

    void Update()
    {
        if (highlighted)
            transform.parent.Rotate(Vector3.up, RotationSpeed * Time.deltaTime);
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
