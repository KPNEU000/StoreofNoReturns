using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraBehaviour : MonoBehaviour
{

    [SerializeField] LayerMask interactableLayer;
    Camera cam;
    PhysicsRaycaster physicsRaycaster;

    Ray ray;

    [SerializeField] private FrontdeskTrigger frontdesk;

    [SerializeField] private AnimationCurve zoomCurve;

    private float baseFOV, currentFOV;

    private float timerBegin = 0;

    private bool zoomedIn = false;
    [SerializeField] private Transform computerAnchor;
    [SerializeField] private Transform basePosition;
    [SerializeField] private GameObject pixelationFilter;
    bool active = true;

    public bool cameraBehaviourEnabled {get; private set;} = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(Screen.width + ", " + Screen.height);
        ExitCustomerInteraction();

        cam = GetComponent<Camera>();
        physicsRaycaster = GetComponent<PhysicsRaycaster>();
        baseFOV = cam.fieldOfView;
        currentFOV = baseFOV;
        cam.fieldOfView = currentFOV;
        frontdesk.CustomerInService += EnterCustomerInteraction;

        BossBattleTest.BossFight += disable;
    }

    void disable()
    {
        cameraBehaviourEnabled = false;
    }
    // Update is called once per frame
    void Update()
    {
        if(!cameraBehaviourEnabled) return;
        //float targetFOV = zoomedIn ? baseFOV * zoom : baseFOV;
        //currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime);
        if(zoomedIn)
        {
            currentFOV = InterpolateZoomIn(Time.time - timerBegin) * baseFOV;
        }
        else
        {
            currentFOV = baseFOV;
        }

        cam.fieldOfView = currentFOV;

        var mouse = Mouse.current;
        if (mouse == null) {Debug.Log("mouse null"); return;} // no mouse connected
        //Mouse.current.position.ReadValue()
        //cam.ScreenToViewportPoint(Mouse.current.position.ReadValue()
        //this returns a normalized (0-1) unsigned vector2 about the position of mouse on screen

        if (mouse.leftButton.wasPressedThisFrame && active)
        {
            Debug.Log("mouse clicked");
            Vector2 mousePos = Mouse.current.position.ReadValue(); // scaled to 780 x 440
            Vector2 viewportFactor = new Vector2((float)cam.pixelWidth / Screen.width, (float)cam.pixelHeight / Screen.height);
            ray = cam.ScreenPointToRay(mousePos * viewportFactor);
            RaycastHit hit;

            print(mousePos + " " + viewportFactor);
            if(Physics.Raycast(ray, out hit, 50f))
            {
                if(hit.transform.CompareTag("Customer"))
                {
                    Debug.Log("customer clicked");
                }

                if (hit.transform.CompareTag("Computer") && zoomedIn)
                {
                    Debug.Log("computer clicked");
                    EnterComputerInteraction();
                }
                
                if(hit.transform.GetComponent<Button>())
                {
                    Debug.Log("button clicked");
                }

            }
        }
    }

    void OnDrawGizmos()
    {
        if (cam == null) cam = GetComponent<Camera>();
        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray);
    }

    private void EnterCustomerInteraction()
    {
        zoomedIn = true;
        timerBegin = Time.time;
        Debug.Log("zoomed");
    }

    private void ExitCustomerInteraction()
    {
        zoomedIn = false;
    }

    private float InterpolateZoomIn(float x)
    {
        if (x > 1) { return zoomCurve.Evaluate(1); }
        float y = zoomCurve.Evaluate(x);
        return y;
    }

    private void EnterComputerInteraction()
    {
        cam.enabled = false;
        physicsRaycaster.enabled = false;
        pixelationFilter.SetActive(false);
        active = false;
    }
    
    public void ExitComputerInteraction()
    {
        cam.enabled = true;
        physicsRaycaster.enabled = true;
        pixelationFilter.SetActive(true);
        active = true;
    }


}
