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
    Camera polCam;
    PhysicsRaycaster physicsRaycaster;

    Ray ray;

    [SerializeField] private FrontdeskTrigger frontdesk;

    [SerializeField] private AnimationCurve zoomCurve;

    private float baseFOV, currentFOV;

    private float timerBegin = 0;

    private bool zoomedIn = false;
    Camera comCam;

    [SerializeField] private Transform computerAnchor;

    [SerializeField] private Transform posterAnchor;

    [SerializeField] private Transform basePosition;
    [SerializeField] private GameObject pixelationFilter;
    [SerializeField] private GameObject customerCanvas;

    [SerializeField] Material outlineMaterial;
    [SerializeField] GameObject posterCanvas;
    [SerializeField] GameObject computerCanvas;
    bool active = true;

    public bool cameraBehaviourEnabled { get; private set; } = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraBehaviourEnabled = true;
        Debug.Log(Screen.width + ", " + Screen.height);

        cam = GetComponent<Camera>();
        comCam = computerAnchor.GetComponentInChildren<Camera>();
        comCam.enabled = false;
        polCam = posterAnchor.GetComponentInChildren<Camera>();
        polCam.enabled = false;
        physicsRaycaster = GetComponent<PhysicsRaycaster>();
        baseFOV = cam.fieldOfView;
        currentFOV = baseFOV;
        cam.fieldOfView = currentFOV;
        frontdesk.CustomerInService += performZoom;

        BossBattleTest.BossFight += disable;
    }

    void disable()
    {
        cameraBehaviourEnabled = false;
    }

    // The boss opening takes the mouse: clicks must slash, not open the computer or poster views.
    public void DisableForBoss()
    {
        disable();
    }
    // Update is called once per frame
    void Update()
    {
        if (!cameraBehaviourEnabled) return;
        //float targetFOV = zoomedIn ? baseFOV * zoom : baseFOV;
        //currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime);
        if (zoomedIn)
        {
            currentFOV = InterpolateZoomIn(Time.time - timerBegin) * baseFOV;
        }
        else
        {
            currentFOV = baseFOV;
        }

        cam.fieldOfView = currentFOV;

        var mouse = Mouse.current;
        if (mouse == null) { Debug.Log("mouse null"); return; } // no mouse connected
        //Mouse.current.position.ReadValue()
        //cam.ScreenToViewportPoint(Mouse.current.position.ReadValue()
        //this returns a normalized (0-1) unsigned vector2 about the position of mouse on screen

        Vector2 mousePos = Mouse.current.position.ReadValue(); // scaled to 780 x 440
        Vector2 viewportFactor = new Vector2((float)cam.pixelWidth / Screen.width, (float)cam.pixelHeight / Screen.height);
        ray = cam.ScreenPointToRay(mousePos * viewportFactor);
        RaycastHit hit;
        outlineMaterial.SetFloat("_Scale", 0f);
        if(Physics.Raycast(ray, out hit, 50f))
        {
            if(hit.transform.CompareTag("Computer") && active)
            {
                outlineMaterial.SetFloat("_Scale", 1.1f);
            }

            if (mouse.leftButton.wasPressedThisFrame && active)
            {
                Debug.Log("mouse clicked");
                

                print(mousePos + " " + viewportFactor);


                if (hit.transform.CompareTag("Computer"))
                {
                    Debug.Log("computer clicked");
                    EnterComputerInteraction();
                }
                if (hit.transform.CompareTag("Poster"))
                {
                    Debug.Log("poster clicked");
                    EnterPosterDisplay();
                }

                if (hit.transform.GetComponent<Button>())
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

    private float InterpolateZoomIn(float x)
    {
        if (x > 1) { return zoomCurve.Evaluate(1); }
        float y = zoomCurve.Evaluate(x);
        return y;
    }

    public void performZoom()
    {

    }

    private void EnterComputerInteraction()
    {
        posterCanvas.SetActive(false);
        cam.enabled = false;
        comCam.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        physicsRaycaster.enabled = false;
        pixelationFilter.SetActive(false);
        active = false;
    }

    public void ExitComputerInteraction()
    {
        posterCanvas.SetActive(true);
        cam.enabled = true;
        comCam.enabled = false;
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        physicsRaycaster.enabled = true;
        pixelationFilter.SetActive(true);
        active = true;
    }

    private void EnterPosterDisplay()
    {
        computerCanvas.SetActive(false);
        cam.enabled = false;
        polCam.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        physicsRaycaster.enabled = false;
        pixelationFilter.SetActive(false);
        //customerCanvas.SetActive(true);
        active = false;
    }

    public void ExitPosterDisplay()
    {
        computerCanvas.SetActive(true);
        cam.enabled = true;
        polCam.enabled = false;
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        physicsRaycaster.enabled = true;
        pixelationFilter.SetActive(true);
        //customerCanvas.SetActive(false);
        active = true;
    }

}
