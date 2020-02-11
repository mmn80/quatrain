using UnityEngine;

public class Highlightable : MonoBehaviour
{
    public Material highlightMaterial;
    Material originalMaterial;

    Renderer ownRenderer;
    AudioSource ownSound;

    void Start()
    {
        ownSound = GetComponent<AudioSource>();
        ownRenderer = GetComponent<Renderer>();
        originalMaterial = ownRenderer?.material;
    }

    void OnMouseEnter()
    {
        if (ownRenderer)
            ownRenderer.material = highlightMaterial;
        if (ownSound)
            ownSound.Play();
    }

    void OnMouseExit()
    {
        if (ownRenderer)
            ownRenderer.material = originalMaterial;        
    }
}
