using UnityEngine;
using UnityEngine.InputSystem;
using System;
public class BossBattleTest : MonoBehaviour
{
    [SerializeField] string tip;
    public static Action BossFight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.fKey.wasPressedThisFrame)
        {
            BossFight?.Invoke();
        }
    }
}
