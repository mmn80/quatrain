using UnityEngine;

public class MainControl : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftControl))
            Application.Quit();
    }
}
