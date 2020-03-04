using System.Collections.Generic;
using UnityEngine;

namespace Quatrain
{
    public class Stick : MonoBehaviour
    {
        public byte PosX, PosY;

        public Material[] NormalVariants, HighlightVariants;

        Renderer ownRenderer;
        AudioSource ownSound;
        bool selected;

        static List<Stick> Instances = new List<Stick>();
        public static void VariantChanged()
        {
            foreach (var stick in Instances)
                stick.ownRenderer.material = stick.selected ?
                    stick.HighlightVariants[Data.It.Variant] :
                    stick.NormalVariants[Data.It.Variant];
        }

        void Start()
        {
            Instances.Add(this);
            ownSound = GetComponent<AudioSource>();
            ownRenderer = GetComponent<Renderer>();
            if (ownRenderer)
                ownRenderer.material = NormalVariants[Data.It.Variant];
        }

        void OnMouseEnter()
        {
            selected = true;
            if (ownRenderer)
                ownRenderer.material = HighlightVariants[Data.It.Variant];
            if (ownSound && !Data.It.EffectsMuted)
                ownSound.Play();
        }

        void OnMouseExit()
        {
            selected = false;
            if (ownRenderer)
                ownRenderer.material = NormalVariants[Data.It.Variant];
        }

        void Update()
        {
            if (selected && Input.GetMouseButtonDown(0))
                Data.Current.game.AddStone(PosX, PosY);
        }
    }
}