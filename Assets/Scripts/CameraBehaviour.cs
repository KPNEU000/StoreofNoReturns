using UnityEngine;
using System;
[RequireComponent(typeof(Camera))]
public class CameraBehaviour : MonoBehaviour
{

    Camera cam;

    [SerializeField] private FrontdeskTrigger frontdesk;

    [SerializeField] private AnimationCurve zoomCurve;

    private float baseFOV, currentFOV;

    private float timerBegin = 0;

    private bool zoomedIn = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ExitCustomerInteraction();

        cam = GetComponent<Camera>();
        baseFOV = cam.fieldOfView;
        currentFOV = baseFOV;

        frontdesk.CustomerInService += EnterCustomerInteraction;
    }

    // Update is called once per frame
    void Update()
    {
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
        if(x > 1) {return zoomCurve.Evaluate(1);}
        float y = zoomCurve.Evaluate(x);
        return y;
    }
}
