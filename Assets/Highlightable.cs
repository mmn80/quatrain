using UnityEngine;

public class Highlightable : MonoBehaviour
{
    public Material highlightMaterial;

    private Renderer ownRenderer = null;
    private Material originalMaterial;

    private void Start()
    {
        ownRenderer = GetComponent<Renderer>();
        originalMaterial = ownRenderer?.material;
    }

    private void OnMouseEnter()
    {
        if (!ownRenderer)
            return;
        ownRenderer.material = highlightMaterial;
    }

    private void OnMouseExit()
    {
        if (!ownRenderer)
            return;
        ownRenderer.material = originalMaterial;        
    }
}
