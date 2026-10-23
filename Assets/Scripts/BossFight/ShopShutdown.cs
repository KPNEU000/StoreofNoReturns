using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossFight
{
    // Runtime guard: the boss fight has no shop customers and no shop clock. The Boss Scene builder already
    // saves these off; this catches anything left on. Only touches the given scene, so a TimeManager carried
    // over from MainScene by DontDestroyOnLoad keeps working when the shop is played again.
    public static class ShopShutdown
    {
        // Returns how many objects/components it switched off.
        public static int Apply(Scene scene)
        {
            var on = StillOn(scene);
            foreach (var target in on)
            {
                // Customers go inactive as a whole object; the spawner and clock only lose their component.
                if (target is CustomerBehaviour) target.gameObject.SetActive(false);
                else ((Behaviour)target).enabled = false;
            }
            return on.Count;
        }

        // Shop pieces still running, in scene order: active customer objects (not under a WitchBoss), then
        // enabled CustomerSpawner, TimeManager and shop-camera AudioListener components, per root. Shared with the
        // builder's validator. Only the player camera (the one holding the sword) keeps a listener: several enabled
        // listeners leave Unity to pick one, and the fight's 3D sounds then come from a shop camera.
        public static List<Component> StillOn(Scene scene)
        {
            var result = new List<Component>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var customer in root.GetComponentsInChildren<CustomerBehaviour>(true))
                {
                    if (customer.GetComponentInParent<WitchBoss>(true) != null) continue;
                    if (customer.gameObject.activeSelf) result.Add(customer);
                }
                foreach (var spawner in root.GetComponentsInChildren<CustomerSpawner>(true))
                {
                    if (spawner.enabled) result.Add(spawner);
                }
                foreach (var clock in root.GetComponentsInChildren<TimeManager>(true))
                {
                    if (clock.enabled) result.Add(clock);
                }
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    if (listener.enabled && !IsPlayerListener(listener)) result.Add(listener);
                }
            }
            return result;
        }

        public static bool IsPlayerListener(AudioListener listener)
        {
            return listener.GetComponentInChildren<PlayerSword>(true) != null;
        }
    }
}
