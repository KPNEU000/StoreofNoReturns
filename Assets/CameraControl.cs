using TMPro;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public static CameraControl Instance;
    public float mouseSensitivity = 100f;
    public float pitchMin = -90;
    public float pitchMax = 90;
    public Transform playerBody;
    public Transform computerAnchor;
    float pitch;
    float roll;
    public float armlength = 5;
    public bool active = true;
    public bool inComputer = false;
    public Transform inComputerTransform;
    Transform basePosition;

    private void Awake()
    {
        basePosition = transform;
        if (Instance != null & Instance != this) //If there is another Instance
        {
            Destroy(gameObject); //Destroy this one so there is only one
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerBody = transform.parent.transform; //the playerbody is the parent of the camera
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (active)
        {
            float moveX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float moveY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            //Yaw
            if (playerBody)
            {
                playerBody.Rotate(Vector3.up * moveX);
            }

            //Pitch
            pitch -= moveY;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        if(inComputer && Input.GetKeyDown(KeyCode.Escape))
        {
            inComputer = false;
            Cursor.visible = false; //Might not be what we're going for
            Cursor.lockState = CursorLockMode.Locked;
            transform.position = basePosition.position;
            transform.rotation = basePosition.rotation;
            active = true;
        }
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, armlength))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            if (hit.collider.CompareTag("Computer") && Input.GetMouseButton(0))
            {
                inComputer = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                transform.position = computerAnchor.position;
                transform.rotation = computerAnchor.rotation;
                active = false;
            }
        }
    }
}
