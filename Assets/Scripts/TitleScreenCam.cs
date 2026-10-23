using UnityEngine;

public class TitleScreenCam : MonoBehaviour
{
    [SerializeField] GameObject filter;
    public float mouseSensitivity = 100f;
    public float Min = -90;
    public float Max = 90;

    float pitch, yaw;
    float roll;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float moveY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        //Yaw
        yaw += moveX;
        //transform.Rotate(Vector3.up * moveX);
        yaw = Mathf.Clamp(yaw, Min, Max);


        //Pitch
        pitch -= moveY;
        pitch = Mathf.Clamp(pitch, Min, Max);
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
    }

    public void SceneAdvance()
    {
        
    }
}
