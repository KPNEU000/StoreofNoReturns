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
        // The boss flashlight (right mouse) replaces the F toggle.
        if(BossFight.PlayerFlashlight.ActiveCount > 0) return;
        if(Input.GetKeyDown(KeyCode.F))
        {
            light.enabled = !light.enabled;
            StoreofNoReturnsSfx.Post(
                light.enabled
                    ? StoreofNoReturnsSfx.FlashlightOn
                    : StoreofNoReturnsSfx.FlashlightOff,
                gameObject
            );
        }
    }
}
