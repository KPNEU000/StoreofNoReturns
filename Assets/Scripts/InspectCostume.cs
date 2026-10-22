using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InspectCostume : MonoBehaviour
{

    float yaw, pitch, roll;

    bool dragging = false;

    [SerializeField] float sensitivity = 0.2f;

    [SerializeField] Material Costume;

    [SerializeField] float pitchMin, pitchMax;

    MeshRenderer renderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dragging = false;
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
        roll = e.z;

        renderer = GetComponent<MeshRenderer>();

        Rerender();
    }

    void Rerender()
    {
        renderer.material = Costume;
    }

    // Update is called once per frame
    void Update()
    {

        if(dragging) rotate();

        if (Mouse.current.leftButton.wasReleasedThisFrame) dragging = false;

        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            Debug.Log("Hit UI: " + r.gameObject.name);  // sorted front to back
            if(r.gameObject.CompareTag("ScrollRect"))
            {
                dragging = true;
            }
        }
        
        
    }

    void rotate()
    {
        Vector2 delta = Mouse.current.delta.ReadValue();
            yaw -= delta.x * sensitivity;
            pitch -= delta.y * sensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }
}
