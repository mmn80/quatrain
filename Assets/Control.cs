using UnityEngine;

public class Control : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
            Application.Quit();
    }
}
