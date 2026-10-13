using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform cam;                                // lämna tom = använder Main Camera
    public Vector3 offset = new Vector3(0f, 5f, -12f);   // 5 m upp, 12 m bakom
    public float smooth = 5f;                            // högre = stelare, 0 = ingen utjämning
    public float lookHeight = 1.5f;                      // hur högt över objektet kameran tittar

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main.transform;

        // Bara objektets vridning runt y (så kameran inte gungar med när drönaren lutar)
        Quaternion yaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Offset vriden efter objektet -> alltid bakom
        Vector3 wantedPos = transform.position + yaw * offset;

        if (smooth > 0f)
            cam.position = Vector3.Lerp(cam.position, wantedPos, smooth * Time.deltaTime);
        else
            cam.position = wantedPos;

        // Titta på objektet
        cam.LookAt(transform.position + Vector3.up * lookHeight);
    }
}