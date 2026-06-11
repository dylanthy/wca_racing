using TMPro;
using UnityEngine;
using System.Collections;

public class RaceLapTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI lapTimerText;
    [SerializeField] private string labelPrefix = "Lap: ";

    [Header("Best Time UI")]
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private GameObject newRecordPanel;
    [SerializeField] private TextMeshProUGUI newRecordPanelText;
    [SerializeField] private float recordFlashDisplayDuration = 5f;
    [SerializeField] private float recordFadeOutDuration = 3f;
    [SerializeField] private float bestTimeFlashInterval = 0.15f;
    [SerializeField] private int bestTimeFlashCount = 6;

    [Header("Finish Line")]
    [SerializeField, Tooltip("Minimum seconds between finish-line triggers to avoid duplicate hits.")]
    private float finishTriggerCooldown = 1f;

    public int CompletedLaps => completedLaps;

    private bool raceStarted;
    private float lapStartTime;
    private int completedLaps;
    private float lastFinishTriggerTime = -999f;
    private DriveCar[] trackedDrives = new DriveCar[0];
    private bool hasCrossedMidGateThisLap;
    public float lapElapsed;

    private float bestLapTime = float.MaxValue;
    private Coroutine recordPanelRoutine;
    private Coroutine bestTimeFlashRoutine;

    void Awake()
    {
        // Restore best time from session data (survives scene reloads)
        bestLapTime = RaceSessionData.BestLapTime;

        UpdateDisplay(0f);
        UpdateBestTimeDisplay(bestLapTime);

        if (newRecordPanel != null)
        {
            newRecordPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!raceStarted)
        {
            return;
        }

        lapElapsed = Time.time - lapStartTime;
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
        hasCrossedMidGateThisLap = false;
        raceStarted = true;
        UpdateDisplay(0f);
    }

    public void ResetRace()
    {
        raceStarted = false;
        completedLaps = 0;
        lastFinishTriggerTime = -999f;
        trackedDrives = new DriveCar[0];
        hasCrossedMidGateThisLap = false;
        lapElapsed = 0f;
        UpdateDisplay(0f);
    }

    public void NotifyMidCrossed(DriveCar driveCar)
    {
        if (!raceStarted || driveCar == null)
        {
            return;
        }

        if (!IsTrackedDriveCar(driveCar))
        {
            return;
        }

        hasCrossedMidGateThisLap = true;
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

        if (!hasCrossedMidGateThisLap)
        {
            return;
        }

        if (Time.time - lastFinishTriggerTime < Mathf.Max(0.05f, finishTriggerCooldown))
        {
            return;
        }

        float finishedLapTime = Time.time - lapStartTime;

        completedLaps++;
        lastFinishTriggerTime = Time.time;
        lapStartTime = Time.time;
        hasCrossedMidGateThisLap = false;
        UpdateDisplay(0f);

        CheckAndUpdateBestTime(finishedLapTime);
    }

    private void CheckAndUpdateBestTime(float lapTime)
    {
        bool isNewRecord = lapTime < bestLapTime;

        if (isNewRecord)
        {
            bestLapTime = lapTime;
            RaceSessionData.SubmitLapTime(lapTime); // persist it
            UpdateBestTimeDisplay(bestLapTime);

            if (bestTimeFlashRoutine != null)
            {
                StopCoroutine(bestTimeFlashRoutine);
            }
            bestTimeFlashRoutine = StartCoroutine(FlashBestTimeNumber());

            if (recordPanelRoutine != null)
            {
                StopCoroutine(recordPanelRoutine);
            }
            recordPanelRoutine = StartCoroutine(ShowNewRecordPanel(lapTime));
        }
    }

    private IEnumerator FlashBestTimeNumber()
    {
        if (bestTimeText == null) yield break;

        for (int i = 0; i < bestTimeFlashCount; i++)
        {
            // Show only the time portion (no label) briefly hidden
            SetBestTimeNumberVisible(false);
            yield return new WaitForSeconds(bestTimeFlashInterval);
            SetBestTimeNumberVisible(true);
            yield return new WaitForSeconds(bestTimeFlashInterval);
        }

        SetBestTimeNumberVisible(true);
        bestTimeFlashRoutine = null;
    }

    private void SetBestTimeNumberVisible(bool visible)
    {
        if (bestTimeText == null) return;

        // We rebuild the text each time so only the time value flashes,
        // not the "Showcase\nBest Time:\n" label.
        string timeStr = visible ? FormatTime(bestLapTime) : "        ";
        bestTimeText.text = "Showcase\nBest Time:\n" + timeStr;
    }

    private IEnumerator ShowNewRecordPanel(float lapTime)
    {
        if (newRecordPanel == null) yield break;

        if (newRecordPanelText != null)
        {
            newRecordPanelText.text = "New Best!\n" + FormatTime(lapTime);
        }

        // Reset alpha
        CanvasGroup cg = newRecordPanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = newRecordPanel.AddComponent<CanvasGroup>();
        }
        cg.alpha = 1f;
        newRecordPanel.SetActive(true);

        yield return new WaitForSeconds(recordFlashDisplayDuration);

        // Fade out
        float elapsed = 0f;
        while (elapsed < recordFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / recordFadeOutDuration);
            yield return null;
        }

        cg.alpha = 0f;
        newRecordPanel.SetActive(false);
        recordPanelRoutine = null;
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
        lapSeconds = Mathf.Max(0f, lapSeconds);
        lapTimerText.text = labelPrefix + FormatTime(lapSeconds);
    }

    private void UpdateBestTimeDisplay(float lapSeconds)
    {
        if (bestTimeText == null) return;

        string timeStr = lapSeconds >= float.MaxValue ? "00:00:00" : FormatTime(lapSeconds);
        bestTimeText.text = "Showcase\nBest Time:\n" + timeStr;
    }

    private string FormatTime(float lapSeconds)
    {
        if (lapSeconds >= float.MaxValue) return "00:00:00";

        lapSeconds = Mathf.Max(0f, lapSeconds);
        int totalCentiseconds = Mathf.FloorToInt(lapSeconds * 100f);
        int minutes = totalCentiseconds / 6000;
        int seconds = (totalCentiseconds / 100) % 60;
        int centiseconds = totalCentiseconds % 100;

        return minutes.ToString("00") + ":" + seconds.ToString("00") + ":" + centiseconds.ToString("00");
    }
}
