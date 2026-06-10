using TMPro;
using UnityEngine;

public class RaceLapTimer : MonoBehaviour, IRaceLapTimer
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI lapTimerText;
    [SerializeField] private string labelPrefix = "Lap: ";

    [Header("Finish Line")]
    [SerializeField, Tooltip("Minimum seconds between finish-line triggers to avoid duplicate hits.")]
    private float finishTriggerCooldown = 1f;

    public int CompletedLaps => completedLaps;

    private bool raceStarted;
    private float lapStartTime;
    private int completedLaps;
    private float lastFinishTriggerTime = -999f;
    private DriveCar[] trackedDrives = new DriveCar[0];

    void Awake()
    {
        UpdateDisplay(0f);
    }

    void Update()
    {
        if (!raceStarted)
        {
            return;
        }

        float lapElapsed = Time.time - lapStartTime;
        UpdateDisplay(lapElapsed);
    }

    public void BeginRace(GameObject playerCarRoot)
    {
        if (playerCarRoot != null)
        {
            trackedDrives = playerCarRoot.GetComponentsInChildren<DriveCar>(true);
        }

        completedLaps = 0;
        lastFinishTriggerTime = -999f;
        lapStartTime = Time.time;
        raceStarted = true;
        UpdateDisplay(0f);
    }

    public void ResetRace()
    {
        raceStarted = false;
        completedLaps = 0;
        lastFinishTriggerTime = -999f;
        trackedDrives = new DriveCar[0];
        UpdateDisplay(0f);
    }

    public void NotifyFinishCrossed(DriveCar driveCar)
    {
        if (!raceStarted || driveCar == null)
        {
            return;
        }

        if (!IsTrackedDriveCar(driveCar))
        {
            return;
        }

        if (Time.time - lastFinishTriggerTime < Mathf.Max(0.05f, finishTriggerCooldown))
        {
            return;
        }

        completedLaps++;
        lastFinishTriggerTime = Time.time;
        lapStartTime = Time.time;
        UpdateDisplay(0f);
    }

    private bool IsTrackedDriveCar(DriveCar driveCar)
    {
        for (int i = 0; i < trackedDrives.Length; i++)
        {
            if (trackedDrives[i] == driveCar)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateDisplay(float lapSeconds)
    {
        if (lapTimerText == null)
        {
            return;
        }

        lapSeconds = Mathf.Max(0f, lapSeconds);
        int totalCentiseconds = Mathf.FloorToInt(lapSeconds * 100f);
        int minutes = totalCentiseconds / 6000;
        int seconds = (totalCentiseconds / 100) % 60;
        int centiseconds = totalCentiseconds % 100;

        lapTimerText.text = labelPrefix + minutes.ToString("00") + ":" + seconds.ToString("00") + ":" + centiseconds.ToString("00");
    }
}
