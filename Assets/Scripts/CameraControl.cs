using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float AngularSpeed, AngularAcceleration, ZoomSpeed, ZoomAcceleration;

    float aSpeed, zSpeed;
    Camera cam;

    void Start() => cam = GetComponent<Camera>();

    void Update()
    {
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            aSpeed = -AngularSpeed;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            aSpeed = AngularSpeed;
        else if (aSpeed < 0)
            aSpeed = Mathf.Min(aSpeed + AngularAcceleration * Time.deltaTime, 0);
        else if (aSpeed > 0)
            aSpeed = Mathf.Max(aSpeed - AngularAcceleration * Time.deltaTime, 0);
        if (aSpeed != 0 && cam)
            cam.transform.RotateAround(Vector3.zero, Vector3.up, aSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            zSpeed = ZoomSpeed;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            zSpeed = -ZoomSpeed;
        else if (zSpeed < 0)
            zSpeed = Mathf.Min(zSpeed + ZoomAcceleration * Time.deltaTime, 0);
        else if (zSpeed > 0)
            zSpeed = Mathf.Max(zSpeed - ZoomAcceleration * Time.deltaTime, 0);
        if (zSpeed != 0 && cam)
        {
            var p = cam.transform.position;
            if (p.sqrMagnitude > 20 || zSpeed < 0)
            {
                p = Vector3.MoveTowards(p, Vector3.zero, zSpeed * Time.deltaTime);
                cam.transform.position = p;
            }
        }

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            cam.orthographic = !cam.orthographic;
        }
    }
}
