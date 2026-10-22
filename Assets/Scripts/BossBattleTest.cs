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
        // F no longer triggers the fight (F belongs to the shop flashlight); the P debug key arrives later.
    }
}
