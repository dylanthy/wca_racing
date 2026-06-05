using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class DriveCar : MonoBehaviour
{
    [SerializeField] private float maxForwardSpeed = 8f;
    [SerializeField] private float maxReverseSpeed = 4f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float friction = 4f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float turnMagnitude = 1.2f;
    [SerializeField, Range(0f, 1f)] private float highSpeedTurnMultiplier = 0.25f;
    [SerializeField] private float minimumSteerSpeed = 0.05f;

    [Header("Recovery")]
    [SerializeField, Tooltip("Dot product of car-up vs world-up below which the car is considered flipped.")]
    private float flippedThreshold = 0.1f;
    [SerializeField, Tooltip("Height added above ground when righting the car, to prevent clipping.")]
    private float flipRightingNudge = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckPadding = 0.15f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Airborne")]
    [SerializeField, Tooltip("How quickly stored drive speed is lost while the car is airborne.")]
    private float airborneSpeedDecay = 8f;
    [SerializeField, Tooltip("Yaw control strength while airborne.")]
    private float airborneYawTorque = 25f;
    [SerializeField, Tooltip("Pitch control strength while airborne. W pitches nose down, S pitches nose up.")]
    private float airbornePitchTorque = 20f;
    [SerializeField, Tooltip("Roll control strength while airborne. A rolls left, D rolls right.")]
    private float airborneRollTorque = 15f;
    [SerializeField, Tooltip("How quickly airborne spin settles. Lower values keep momentum longer.")]
    private float airborneAngularDamping = 0.35f;
    [SerializeField, Tooltip("How much on-ground steering spin is preserved when taking off.")]
    private float takeoffSpinTransfer = 0.9f;
    [SerializeField, Tooltip("If enabled, player can steer/tilt while airborne. Leave off to ignore input in air.")]
    private bool allowAirControl;

    private float currentSpeed;
    private float throttleInput;
    private float turnInput;
    private bool flipRequested;

    private Collider carCollider;
    private Rigidbody carRigidbody;
    private float defaultAngularDamping;
    private bool wasGrounded;
    private float lastGroundYawRateDeg;

    void Awake()
    {
        carCollider = GetComponent<Collider>();
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        defaultAngularDamping = carRigidbody.angularDamping;
    }

    void Start()
    {
        ApplySelectedCarStats();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        throttleInput = 0f;
        turnInput = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            throttleInput += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            throttleInput -= 1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            turnInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            turnInput += 1f;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            flipRequested = true;
        }
    }

    void FixedUpdate()
    {
        bool isGrounded = TryGetGroundHit(out RaycastHit groundHit);
        bool isFlipped = Vector3.Dot(transform.up, Vector3.up) < flippedThreshold;

        if (flipRequested)
        {
            flipRequested = false;

            if (isGrounded && isFlipped)
            {
                float yaw = transform.eulerAngles.y;
                carRigidbody.position += Vector3.up * flipRightingNudge;
                carRigidbody.rotation = Quaternion.Euler(0f, yaw, 0f);
                carRigidbody.linearVelocity = Vector3.zero;
                carRigidbody.angularVelocity = Vector3.zero;
                currentSpeed = 0f;
                return;
            }
        }

        if (!isGrounded)
        {
            if (wasGrounded)
            {
                float takeoffYawRateRad = lastGroundYawRateDeg * Mathf.Deg2Rad * takeoffSpinTransfer;
                carRigidbody.angularVelocity += transform.up * takeoffYawRateRad;

                // Drop all translational momentum at takeoff while keeping rotational motion in the air.
                currentSpeed = 0f;
                carRigidbody.linearVelocity = Vector3.zero;
            }

            carRigidbody.angularDamping = airborneAngularDamping;
            if (allowAirControl)
            {
                ApplyAirborneRotationControl();
            }
            wasGrounded = false;
            return;
        }

        wasGrounded = true;
        carRigidbody.angularDamping = defaultAngularDamping;

        if (throttleInput != 0f)
        {
            currentSpeed += acceleration * throttleInput * Time.fixedDeltaTime;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, friction * Time.fixedDeltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxReverseSpeed, maxForwardSpeed);

        float speedAbs = Mathf.Abs(currentSpeed);
        float directionalMaxSpeed = currentSpeed >= 0f ? maxForwardSpeed : maxReverseSpeed;
        float normalizedSpeed = directionalMaxSpeed > 0f ? Mathf.Clamp01(speedAbs / directionalMaxSpeed) : 0f;
        float speedScaledTurnMagnitude = speedAbs < minimumSteerSpeed
            ? 0f
            : Mathf.Lerp(turnMagnitude, turnMagnitude * highSpeedTurnMultiplier, normalizedSpeed);
        lastGroundYawRateDeg = turnInput * turnSpeed * speedScaledTurnMagnitude;

        float yawDelta = turnInput * turnSpeed * speedScaledTurnMagnitude * Time.fixedDeltaTime;
        Quaternion targetRotation = carRigidbody.rotation;

        if (!Mathf.Approximately(yawDelta, 0f))
        {
            targetRotation = carRigidbody.rotation * Quaternion.Euler(0f, yawDelta, 0f);
            carRigidbody.MoveRotation(targetRotation);
        }

        Vector3 driveDirection = Vector3.ProjectOnPlane(targetRotation * Vector3.right, groundHit.normal);
        if (driveDirection.sqrMagnitude < 0.0001f)
        {
            driveDirection = Vector3.ProjectOnPlane(targetRotation * Vector3.right, Vector3.up);
        }

        driveDirection.Normalize();

        Vector3 groundAlignedVelocity = driveDirection * currentSpeed;
        Vector3 groundNormalVelocity = Vector3.Project(carRigidbody.linearVelocity, groundHit.normal);
        carRigidbody.linearVelocity = groundAlignedVelocity + groundNormalVelocity;
    }

    private void ApplyAirborneRotationControl()
    {
        float yawControl = turnInput * airborneYawTorque;
        float pitchControl = -throttleInput * airbornePitchTorque;
        float rollControl = turnInput * airborneRollTorque;

        Vector3 controlTorque =
            transform.up * yawControl +
            transform.right * pitchControl +
            transform.forward * rollControl;

        carRigidbody.AddTorque(controlTorque, ForceMode.Acceleration);
    }

    private bool TryGetGroundHit(out RaycastHit hit)
    {
        Bounds bounds = carCollider.bounds;
        return Physics.Raycast(
            bounds.center,
            Vector3.down,
            out hit,
            bounds.extents.y + groundCheckPadding,
            groundLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void ApplySelectedCarStats()
    {
        CarDefinition selectedCar = CarSelectionRuntime.SelectedCar;
        if (selectedCar == null || selectedCar.driveStats == null)
        {
            return;
        }

        CarDriveStats stats = selectedCar.driveStats;
        maxForwardSpeed = stats.maxForwardSpeed;
        maxReverseSpeed = stats.maxReverseSpeed;
        acceleration = stats.acceleration;
        friction = stats.friction;
        turnSpeed = stats.turnSpeed;
        turnMagnitude = stats.turnMagnitude;
        highSpeedTurnMultiplier = stats.highSpeedTurnMultiplier;
        minimumSteerSpeed = stats.minimumSteerSpeed;
    }
}
