using UnityEngine;
using System;
public class PlayerMovement : MonoBehaviour
{
    public float speed = 10f;

    public float mouseSensitivity = 100F;
    public float pitchMin = -90f;
    public float pitchMax = 90f;
    [SerializeField] Transform body;
    [SerializeField] Transform cameraTrans;
    float pitch;

    public float jumpHeight = 0.4f;
    public float gravity = 9.81f;
    public float airControll = 10f;

    public bool MovementEnabled { get; private set; } = false;

    Vector3 input;
    Vector3 moveDirection;
    CharacterController controller;

    [SerializeField] float navRecalcThreshold = 1f;

    public static Action PlayerMoved;

    Vector3 PathTrail;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PathTrail = transform.position;
        BossBattleTest.BossFight += enableMovement;
        controller = GetComponent<CharacterController>();
    }

    public void enableMovement()
    {
        MovementEnabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(!MovementEnabled) return;

        float moveX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float moveY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        //yaw at the player
        if (body)
        {
            Debug.Log("turn");
            transform.Rotate(Vector3.up * moveX);

        }
        //pitch at the camera
        pitch -= moveY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        cameraTrans.localRotation = Quaternion.Euler(pitch, 0, 0);


        //get input
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        //input vector 

        input = transform.right * moveHorizontal + transform.forward * moveVertical;
        input.Normalize();
        if (controller.isGrounded)
        {
            moveDirection = input;
            if (Input.GetButton("Jump"))
            {
                //moveDirection.y = Mathf.Sqrt(2 * jumpHeight * gravity);
            }
            else
            {
                moveDirection.y = 0.0f;
            }
        }
        else
        {
            input.y = moveDirection.y;
            moveDirection = Vector3.Lerp(moveDirection, input, Time.deltaTime);
        }
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * speed * Time.deltaTime);
        if(Vector3.Distance(transform.position, PathTrail) >= navRecalcThreshold)
        {
            PlayerMoved?.Invoke();
        }
    }
}
