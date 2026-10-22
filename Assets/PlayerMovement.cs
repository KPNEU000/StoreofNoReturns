using UnityEngine;
using System;
using BossFight.Rules;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    public float speed = 10f;

    public float mouseSensitivity = MoveSmoothing.DefaultSensitivity; // degrees per pixel; legacy scene values (100) are normalized at runtime
    public float pitchMin = -90f;
    public float pitchMax = 90f;
    [SerializeField] Transform body;
    [SerializeField] Transform cameraTrans;
    float pitch;

    public float jumpHeight = 0.4f;
    public float gravity = 9.81f;
    public float airControll = 10f; // unused

    public bool MovementEnabled { get; private set; } = false;

    Vector3 input;
    Vector3 moveDirection;
    CharacterController controller;

    [SerializeField] float navRecalcThreshold = 1f;

    public static Action PlayerMoved;

    Vector3 PathTrail;

    // Other components (e.g. the slash) can slow the player by setting this; 1 = full speed.
    public float SpeedMultiplier { get; set; } = 1f;
    public float EffectiveSpeed => speed * SpeedMultiplier;

    [SerializeField] float accelTime = MoveSmoothing.DefaultAccelTime;
    MoveSmoothing smoothing;
    Vector2 planarVelocity;
    float verticalVelocity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PathTrail = transform.position;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        BossBattleTest.BossFight += enableMovement;
        controller = GetComponent<CharacterController>();
        smoothing = new MoveSmoothing(accelTime);
    }

    public void enableMovement()
    {
        MovementEnabled = true;
        MovementLocked = false;
    }

    // Free look with WASD ignored (the opening cinematic).
    public void EnableLookOnly()
    {
        MovementEnabled = true;
        MovementLocked = true;
    }

    // WASD is ignored while locked; mouse look keeps working.
    public bool MovementLocked { get; set; }

    // A script is moving the player (the counter vault); physics and input movement are skipped.
    public bool ScriptedMotion { get; set; }

    // While set, the view turns toward this world point instead of following the mouse (the opening cinematic).
    public Vector3? LookTarget { get; set; }
    public float LookTurnDegreesPerSecond { get; set; } = 120f;

    // Yaw on the body, pitch on the camera, each eased toward looking at `point`.
    void TurnTowards(Vector3 point)
    {
        Vector3 to = point - cameraTrans.position;
        float flat = new Vector2(to.x, to.z).magnitude;
        if (flat < 1e-4f) return;
        float step = LookTurnDegreesPerSecond * Time.deltaTime;
        var yaw = Quaternion.LookRotation(new Vector3(to.x, 0f, to.z));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, yaw, step);
        float targetPitch = Mathf.Clamp(-Mathf.Atan2(to.y, flat) * Mathf.Rad2Deg, pitchMin, pitchMax);
        pitch = Mathf.MoveTowardsAngle(pitch, targetPitch, step);
        cameraTrans.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if(!MovementEnabled) return;

        var mouse = Mouse.current;
        var kb = Keyboard.current;

        if (LookTarget.HasValue)
        {
            TurnTowards(LookTarget.Value);
        }
        else if (mouse != null)
        {
            Vector2 look = MoveSmoothing.LookDelta(mouse.delta.ReadValue(), MoveSmoothing.NormalizeSensitivity(mouseSensitivity));
            //yaw at the player
            if (body)
            {
                transform.Rotate(Vector3.up * look.x);
            }
            //pitch at the camera
            pitch -= look.y;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            cameraTrans.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        if (ScriptedMotion)
        {
            planarVelocity = Vector2.zero;
            verticalVelocity = 0f;
            return;
        }

        //get input (raw keys, eased by MoveSmoothing)
        Vector2 keys = Vector2.zero;
        if (kb != null && !MovementLocked)
        {
            keys.x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            keys.y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        }
        keys = Vector2.ClampMagnitude(keys, 1f);
        planarVelocity = smoothing.Approach(planarVelocity, keys, 1f, Time.deltaTime);

        input = transform.right * planarVelocity.x + transform.forward * planarVelocity.y;
        moveDirection = input;
        if (controller.isGrounded)
        {
            moveDirection.y = 0.0f;
        }
        else
        {
            moveDirection.y = verticalVelocity;
        }
        moveDirection.y -= gravity * Time.deltaTime;
        verticalVelocity = moveDirection.y;
        controller.Move(new Vector3(moveDirection.x * EffectiveSpeed, moveDirection.y, moveDirection.z * EffectiveSpeed) * Time.deltaTime);
        if(Vector3.Distance(transform.position, PathTrail) >= navRecalcThreshold)
        {
            PathTrail = transform.position;
            PlayerMoved?.Invoke();
        }
    }
}
