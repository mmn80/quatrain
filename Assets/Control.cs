using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    void Start()
    {
    }

    void Update()
    {
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical") + 1;
        var t = gameObject.transform;
        t.localRotation = Quaternion.Euler(0, h * 180, 0);
        t.localScale = v * Vector3.one;

        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
            Application.Quit();
    }
}
