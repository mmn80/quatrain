using UnityEngine;

namespace Quatrene
{
    public class Stick : MonoBehaviour
    {
        public int PosX, PosY;

        public Material highlightMaterial;
        Material originalMaterial;

        Renderer ownRenderer;
        AudioSource ownSound;
        bool selected;

        void Start()
        {
            ownSound = GetComponent<AudioSource>();
            ownRenderer = GetComponent<Renderer>();
            originalMaterial = ownRenderer?.material;
        }

        void OnMouseEnter()
        {
            selected = true;
            if (ownRenderer)
                ownRenderer.material = highlightMaterial;
            if (ownSound && !MainControl.EffectsMuted)
                ownSound.Play();
        }

        void OnMouseExit()
        {
            selected = false;
            if (ownRenderer)
                ownRenderer.material = originalMaterial;
        }

        void Update()
        {
            if (selected && !Game.MadeQuatreneThisTurn && Input.GetMouseButtonDown(0))
                Game.AddStone(PosX, PosY);
        }
    }
}