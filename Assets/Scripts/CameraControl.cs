using UnityEngine;

namespace Quatrain
{
    public class CameraControl : MonoBehaviour
    {
        public float AngularSpeed, AngularAcceleration;
        public float ZoomSpeed, ZoomAcceleration;

        float aSpeed, hSpeed, zSpeed;
        Camera cam;

        public static bool Orthographic = false;

        void Start() => cam = GetComponent<Camera>();

        void Update()
        {
            if (MainControl.IsInputOn())
                return;

            var ctrl = Input.GetKey(KeyCode.LeftControl);
            if (!ctrl && (Input.GetKey(KeyCode.A) ||
                    Input.GetKey(KeyCode.LeftArrow)))
                aSpeed = -AngularSpeed;
            else if (!ctrl && (Input.GetKey(KeyCode.D) ||
                    Input.GetKey(KeyCode.RightArrow)))
                aSpeed = AngularSpeed;
            else if (aSpeed < 0)
                aSpeed = Mathf.Min(aSpeed + AngularAcceleration * Time.deltaTime, 0);
            else if (aSpeed > 0)
                aSpeed = Mathf.Max(aSpeed - AngularAcceleration * Time.deltaTime, 0);
            if (aSpeed != 0 && cam)
                cam.transform.RotateAround(Vector3.zero, Vector3.up, aSpeed * Time.deltaTime);

            var hMaxSpeed = AngularSpeed / 2;
            if (!ctrl && !Data.gamesListOpened && (Input.GetKey(KeyCode.W) ||
                    Input.GetKey(KeyCode.UpArrow)))
                hSpeed = -hMaxSpeed;
            else if (!ctrl && !Data.gamesListOpened && (Input.GetKey(KeyCode.S) ||
                    Input.GetKey(KeyCode.DownArrow)))
                hSpeed = hMaxSpeed;
            else if (hSpeed < 0)
                hSpeed = Mathf.Min(hSpeed + AngularAcceleration * Time.deltaTime, 0);
            else if (hSpeed > 0)
                hSpeed = Mathf.Max(hSpeed - AngularAcceleration * Time.deltaTime, 0);
            if (hSpeed != 0 && cam)
                cam.transform.RotateAround(Vector3.zero, cam.transform.right, hSpeed * Time.deltaTime);

            if (Input.GetKey(KeyCode.Equals))
                zSpeed = ZoomSpeed;
            else if (Input.GetKey(KeyCode.Minus))
                zSpeed = -ZoomSpeed;
            else if (zSpeed < 0)
                zSpeed = Mathf.Min(zSpeed + ZoomAcceleration * Time.deltaTime, 0);
            else if (zSpeed > 0)
                zSpeed = Mathf.Max(zSpeed - ZoomAcceleration * Time.deltaTime, 0);
            if (zSpeed != 0 && cam)
            {
                if (Orthographic)
                {
                    var scale = cam.orthographicSize;
                    if (scale > 1 || zSpeed < 0)
                    {
                        scale -= zSpeed * Time.deltaTime * 0.5f;
                        cam.orthographicSize = scale;
                    }
                }
                else
                {
                    var p = cam.transform.position;
                    if (p.sqrMagnitude > 20 || zSpeed < 0)
                    {
                        p = Vector3.MoveTowards(p, Vector3.zero, zSpeed * Time.deltaTime);
                        cam.transform.position = p;
                    }
                }
            }
        }
    }
}