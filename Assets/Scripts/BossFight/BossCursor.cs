using BossFight.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight
{
    // Keeps the mouse locked for first-person look while the boss opening or fight is running. main moved cursor
    // locking out of PlayerMovement (the shop's computer and dialogue need a free mouse), so the boss scene owns
    // it now. A click re-locks it (as in any FPS); it is left alone while paused and after the fight ends.
    public class BossCursor : MonoBehaviour
    {
        private BossDirector director;
        private bool lockedOnce;

        private void Awake()
        {
            director = FindFirstObjectByType<BossDirector>();
        }

        private void Update()
        {
            if (director == null) return;
            var phase = director.Phase;
            bool fighting = director.OpeningRunning || phase == BossPhase.Phase1 || phase == BossPhase.Phase2;
            if (!fighting || Time.timeScale == 0f) return;
            if (Cursor.lockState == CursorLockMode.Locked) return;
            var mouse = Mouse.current;
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            // Lock straight away when the fight starts, then again whenever the player clicks back into the game.
            if (!lockedOnce || clicked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                lockedOnce = true;
            }
        }
    }
}
