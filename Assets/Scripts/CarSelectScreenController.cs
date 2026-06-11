using System.Collections.Generic;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarSelectScreenController : MonoBehaviour
{
    [Header("Cars")]
    [SerializeField] private List<CarDefinition> cars = new List<CarDefinition>();
    [SerializeField] private int startingIndex;

    [Header("Preview")]
    [SerializeField] private Transform previewAnchor;

    [Header("Canvas State")]
    [SerializeField] private Canvas carSelectCanvas;
    [SerializeField] private Canvas racingCanvas;

    [Header("Selection Camera Orbit")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float orbitRadius = 8f;
    [SerializeField] private float orbitHeight = 3f;
    [SerializeField] private float orbitSpeed = 30f;
    [SerializeField] private float orbitLookAtHeight = 1.2f;
    [SerializeField] private float orbitPositionLerp = 8f;
    [SerializeField] private float orbitRotationLerp = 10f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI carNameText;
    [SerializeField] private TextMeshProUGUI creatorText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI handlingText;
    [SerializeField] private TextMeshProUGUI driftText;
    [SerializeField] private TextMeshProUGUI frictionText;

    [Header("Gameplay Transition")]
    [SerializeField] private CarCameraFollow gameplayCameraFollow;
    [SerializeField] private float startTransitionDuration = 1f;

    [Header("Countdown")]
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Race Timer")]
    [SerializeField] private RaceLapTimer raceLapTimer;

    [Header("Audio")]
    [SerializeField] private AudioSource gameStartAudioSource;

    private int currentIndex;
    private GameObject previewInstance;
    private float orbitAngle;
    private bool gameStarted;
    private bool raceTimerStarted;
    private Coroutine transitionRoutine;
    private Coroutine countdownRoutine;

    private void OnEnable()
    {
        DriveCar.FellOutOfBounds += HandleCarFellOutOfBounds;
    }

    private void OnDisable()
    {
        DriveCar.FellOutOfBounds -= HandleCarFellOutOfBounds;
    }

    void Start()
    {
        if (carSelectCanvas == null)
        {
            carSelectCanvas = GetComponentInParent<Canvas>();
        }

        UpdateCanvasState(false);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (gameplayCameraFollow == null && cameraTransform != null)
        {
            gameplayCameraFollow = cameraTransform.GetComponent<CarCameraFollow>();
        }

        if (gameplayCameraFollow != null)
        {
            gameplayCameraFollow.enabled = false;
        }

        if (cars.Count == 0)
        {
            UpdateUiForNoCars();
            return;
        }

        if (raceLapTimer == null)
        {
            raceLapTimer = FindAnyObjectByType<RaceLapTimer>();
        }

        if (raceLapTimer != null)
        {
            raceLapTimer.ResetRace();
        }

        currentIndex = WrapIndex(startingIndex, cars.Count);
        ShowCurrentCar();
    }

    void Update()
    {
        if (!gameStarted)
        {
            UpdateSelectionCameraOrbit();
        }
    }

    public void SelectNextCar()
    {
        if (cars.Count == 0)
        {
            return;
        }

        currentIndex = WrapIndex(currentIndex + 1, cars.Count);
        ShowCurrentCar();
    }

    public void SelectPreviousCar()
    {
        if (cars.Count == 0)
        {
            return;
        }

        currentIndex = WrapIndex(currentIndex - 1, cars.Count);
        ShowCurrentCar();
    }

    public void StartGame()
    {
        if (gameStarted || cars.Count == 0)
        {
            return;
        }

        gameStarted = true;
        raceTimerStarted = false;
        UpdateCanvasState(true);
        CarDefinition currentCar = cars[currentIndex];
        CarSelectionRuntime.SetSelection(currentCar, currentIndex);

        if (gameStartAudioSource != null)
        {
            gameStartAudioSource.Play();
        }

        SetDriveScriptsEnabled(previewInstance, true);
        SetCarsDrivable(previewInstance, false);
        StartOrRestartCountdown();
        
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }
        transitionRoutine = StartCoroutine(TransitionToGameplayCamera());
    }

    private void UpdateCanvasState(bool isRacing)
    {
        if (carSelectCanvas != null)
        {
            carSelectCanvas.enabled = !isRacing;
            GraphicRaycaster carSelectRaycaster = carSelectCanvas.GetComponent<GraphicRaycaster>();
            if (carSelectRaycaster != null)
            {
                carSelectRaycaster.enabled = !isRacing;
            }
        }

        if (racingCanvas != null)
        {
            racingCanvas.enabled = isRacing;
            GraphicRaycaster racingRaycaster = racingCanvas.GetComponent<GraphicRaycaster>();
            if (racingRaycaster != null)
            {
                racingRaycaster.enabled = isRacing;
            }
        }
    }

    private IEnumerator TransitionToGameplayCamera()
    {
        if (cameraTransform == null || gameplayCameraFollow == null || previewInstance == null)
        {
            yield break;
        }

        gameplayCameraFollow.SetTarget(previewInstance.transform);

        if (!gameplayCameraFollow.TryGetDesiredPose(out Vector3 targetPosition, out Quaternion targetRotation))
        {
            gameplayCameraFollow.enabled = true;
            yield break;
        }

        Vector3 initialPosition = cameraTransform.position;
        Quaternion initialRotation = cameraTransform.rotation;
        float duration = Mathf.Max(0.01f, startTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t);
            cameraTransform.position = Vector3.Lerp(initialPosition, targetPosition, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(initialRotation, targetRotation, smoothT);
            yield return null;
        }

        cameraTransform.position = targetPosition;
        cameraTransform.rotation = targetRotation;
        gameplayCameraFollow.enabled = true;
    }

    private void UpdateSelectionCameraOrbit()
    {
        if (cameraTransform == null || previewInstance == null)
        {
            return;
        }

        orbitAngle += orbitSpeed * Time.deltaTime;

        Vector3 orbitCenter = previewInstance.transform.position + Vector3.up * orbitLookAtHeight;
        Vector3 orbitOffset = Quaternion.Euler(0f, orbitAngle, 0f) * new Vector3(0f, orbitHeight, -orbitRadius);
        Vector3 desiredPosition = orbitCenter + orbitOffset;
        Vector3 lookDirection = orbitCenter - desiredPosition;

        float posT = 1f - Mathf.Exp(-Mathf.Max(0f, orbitPositionLerp) * Time.deltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, posT);

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            float rotT = 1f - Mathf.Exp(-Mathf.Max(0f, orbitRotationLerp) * Time.deltaTime);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, rotT);
        }
    }

    private void SetDriveScriptsEnabled(GameObject carInstance, bool enabledState)
    {
        if (carInstance == null)
        {
            return;
        }

        DriveCar[] drives = carInstance.GetComponentsInChildren<DriveCar>(true);
        for (int i = 0; i < drives.Length; i++)
        {
            drives[i].enabled = enabledState;
        }
    }

    private void SetCarsDrivable(GameObject carInstance, bool drivable)
    {
        if (carInstance == null)
        {
            return;
        }

        DriveCar[] drives = carInstance.GetComponentsInChildren<DriveCar>(true);
        for (int i = 0; i < drives.Length; i++)
        {
            drives[i].SetDrivable(drivable);
        }
    }

    private void StartOrRestartCountdown()
    {
        SetCarsDrivable(previewInstance, false);

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }

        countdownRoutine = StartCoroutine(CountdownAndEnableDrive());
    }

    private IEnumerator CountdownAndEnableDrive()
    {
        int count = Mathf.Max(1, countdownSeconds);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        for (int value = count; value >= 1; value--)
        {
            if (countdownText != null)
            {
                countdownText.text = value.ToString();
            }

            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null)
        {
            countdownText.text = "GO!";
        }

        SetCarsDrivable(previewInstance, true);

        if (!raceTimerStarted && raceLapTimer != null)
        {
            raceLapTimer.BeginRace(previewInstance);
            raceTimerStarted = true;
        }

        yield return new WaitForSeconds(0.5f);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        countdownRoutine = null;
    }

    private void HandleCarFellOutOfBounds(DriveCar driveCar)
    {
        if (!gameStarted || driveCar == null || previewInstance == null)
        {
            return;
        }

        if (!driveCar.transform.IsChildOf(previewInstance.transform))
        {
            return;
        }

        if (gameStartAudioSource != null)
        {
            gameStartAudioSource.Stop();
            gameStartAudioSource.Play();
        }

        StartOrRestartCountdown();
    }

    private void OnDestroy()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
    }

    private void ShowCurrentCar()
    {
        if (gameStarted)
        {
            return;
        }

        CarDefinition currentCar = cars[currentIndex];
        UpdateUi(currentCar);
        RebuildPreview(currentCar);
    }

    private void UpdateUi(CarDefinition car)
    {
        carNameText.text = car != null ? car.carName : "No Car";
        creatorText.text = car != null ? "Creator: " + car.creator : string.Empty;
        speedText.text = car != null ? "Top Speed: " + car.driveStats.maxForwardSpeed.ToString("0.0") : string.Empty;
        handlingText.text = car != null ? "Handling: "+car.driveStats.turnMagnitude.ToString("0.0") : string.Empty;
        driftText.text = car != null ? "Drifting: "+car.driveStats.highSpeedTurnMultiplier.ToString("0.0") : string.Empty;
        frictionText.text = car != null ? "Friction: "+car.driveStats.friction.ToString("0.0") : string.Empty;
    }

    private string BuildStatsText(CarDefinition car)
    {
        if (car == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Top Speed: " + car.driveStats.maxForwardSpeed.ToString("0.0"));
        builder.AppendLine("Reverse: " + car.driveStats.maxReverseSpeed.ToString("0.0"));
        builder.AppendLine("Acceleration: " + car.driveStats.acceleration.ToString("0.0"));
        builder.AppendLine("Handling: " + car.driveStats.turnSpeed.ToString("0.0"));

        for (int i = 0; i < car.customStats.Count; i++)
        {
            CarStatLine stat = car.customStats[i];
            if (stat == null || string.IsNullOrWhiteSpace(stat.label))
            {
                continue;
            }

            builder.AppendLine(stat.label + ": " + stat.value);
        }

        return builder.ToString().TrimEnd();
    }

    private void RebuildPreview(CarDefinition car)
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }

        if (car == null || car.previewPrefab == null)
        {
            return;
        }

        Transform anchor = previewAnchor != null ? previewAnchor : transform;
        previewInstance = Instantiate(car.previewPrefab, anchor);
        previewInstance.transform.localPosition = Vector3.zero;
        //previewInstance.transform.localRotation = Quaternion.identity;
        //previewInstance.transform.localScale = Vector3.one;

        SetDriveScriptsEnabled(previewInstance, false);
    }

    private void UpdateUiForNoCars()
    {
        if (carNameText != null)
        {
            carNameText.text = "No Cars Configured";
        }

        if (creatorText != null)
        {
            creatorText.text = string.Empty;
        }
    }

    private static int WrapIndex(int value, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int wrapped = value % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }

}
