using UnityEngine;

public class Stone : MonoBehaviour
{
    public float RotationSpeed;

    public StoneType StoneType;

    AudioSource ownSound;
    bool highlighted;

    void Start()
    {
        ownSound = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (highlighted)
            transform.parent.Rotate(Vector3.up, RotationSpeed * Time.deltaTime);
    }

    void OnMouseEnter()
    {
        highlighted = true;
        if (ownSound)
            ownSound.Play();
        
    }

    void OnMouseExit()
    {
        highlighted = false;
    }
}
