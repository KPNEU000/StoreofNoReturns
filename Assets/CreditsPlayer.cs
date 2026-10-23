using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CreditsPlayer : MonoBehaviour
{
    [SerializeField] List<string> credits;
    int creditsIndex = 0;
    TextMeshProUGUI creditsText;
    Animation[] animations;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        creditsText = GetComponentInChildren<TextMeshProUGUI>();
        animations = GameObject.FindObjectsByType<Animation>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        foreach (Animation animation in animations)
        {
            animation.Play();
        }
        StartCoroutine("NextCredits");
    }

    public IEnumerator NextCredits()
    {
        if(creditsIndex <= credits.Count - 1)
        creditsText.text = credits[creditsIndex];
        creditsIndex++;
        yield return new WaitForSeconds(2.5f);
        print("next credit");
        StartCoroutine("NextCredits");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
