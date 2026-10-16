using UnityEngine;

public class FlashlightControl : MonoBehaviour
{
    Light light;
    void Start()
    {
        light = GetComponentInChildren<Light>();  
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            light.enabled = !light.enabled;
        }
    }
}
