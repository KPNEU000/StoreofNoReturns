using System;
using UnityEngine;

public class TitleScreenAnimation : MonoBehaviour
{
    public float speed;
    public float radiusIncrement;
    [SerializeField] GameObject[] titles;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < titles.Length; i++)
        {
            Vector2 displacement = radiusIncrement * i * new Vector2(Mathf.Cos(Time.time * speed), Mathf.Sin(Time.time * speed));
            titles[i].GetComponent<RectTransform>().localPosition = displacement;
        }
    }
}
