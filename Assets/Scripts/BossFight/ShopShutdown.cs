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
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var customer in root.GetComponentsInChildren<CustomerBehaviour>(true))
                {
                    if (customer.GetComponentInParent<WitchBoss>(true) != null) continue;
                    if (!customer.gameObject.activeSelf) continue;
                    customer.gameObject.SetActive(false);
                    count++;
                }
                foreach (var spawner in root.GetComponentsInChildren<CustomerSpawner>(true))
                {
                    if (!spawner.enabled) continue;
                    spawner.enabled = false;
                    count++;
                }
                foreach (var clock in root.GetComponentsInChildren<TimeManager>(true))
                {
                    if (!clock.enabled) continue;
                    clock.enabled = false;
                    count++;
                }
            }
            return count;
        }
    }
}
