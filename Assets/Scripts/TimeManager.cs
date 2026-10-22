using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{

    public static TimeManager Instance;

    [Header("Starting Time (in 24-hour format)")]
    [Range(0,23)] public int startHour = 18; //6:00PM
    [Range(0, 59)] public int starMinute = 0;
    [Range(0, 23)] public int endHour = 0; //12:00AM
    [Range(0, 59)] public int endMinute = 0;

    [Header("Time Settings")]
    public float realSecondsPerInGameMinute = 1f;

    private float timer;
    private bool shopClosed = false;
    public int CurrentHour { get; private set; }
    public int CurrentMinute { get; private set; }

    [Header("UI Settings")]
    [SerializeField]
    private TMP_Text timeText;
    [SerializeField]
    CustomerSpawner customerSpawner;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CurrentHour = startHour;
        CurrentMinute = starMinute;
        timer = 0f;
    }


    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= realSecondsPerInGameMinute && !shopClosed)
        {
            timer -= realSecondsPerInGameMinute;
            AdvanceMinute();
            UpdateTimeText();
            // CheckEndOfTime(); // TEMP: disabled for dialogue testing
        }
    }

    void AdvanceMinute() {
        CurrentMinute++;

        if (CurrentMinute >= 60)
        {
            CurrentMinute = 0;
            CurrentHour++;
            customerSpawner.NewCustomer();

            if (CurrentHour >= 24)
            {
                CurrentHour = 0;
            }
        }
    }

    void UpdateTimeText()
    {
        if (CurrentHour == 0)
        {
            timeText.text = $"12:{CurrentMinute:00}" + " AM";
        }
        else
        {
            float timeHour = CurrentHour;
            string timeOfDay = " AM";
            if (CurrentHour >= 12)
            {
                timeHour -= 12;
                timeOfDay = " PM";
            }
            timeText.text = $"{timeHour:00}:{CurrentMinute:00}" + timeOfDay;
        }
    }

    void CheckEndOfTime()
    {
        if (CurrentHour == endHour && CurrentMinute == endMinute)
        {
            shopClosed = true;
            customerSpawner.WitchComes();
            //FindFirstObjectByType<SceneLoadingManager>().GoToNextScene();
            //perform end of game boss battle thing
        }
    }
}
