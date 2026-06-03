using UnityEngine;
using UnityEngine.SceneManagement;

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

    private float currentSpeed;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        bool isFlipped = Vector3.Dot(transform.up, Vector3.up) < flippedThreshold;

        if (isFlipped && Input.GetKeyDown(KeyCode.Q))
        {
            float yaw = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.position += Vector3.up * flipRightingNudge;
            currentSpeed = 0f;
            return;
        }

        float throttleInput = 0f;
        float turnInput = 0f;

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

        if (throttleInput > 0f)
        {
            currentSpeed += acceleration * throttleInput * Time.deltaTime;
        }
        else if (throttleInput < 0f)
        {
            currentSpeed += acceleration * throttleInput * Time.deltaTime;
        }
        else
        {
            // Apply friction when no throttle is pressed so the car coasts, then slows to a stop.
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, friction * Time.deltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxReverseSpeed, maxForwardSpeed);

        float speedAbs = Mathf.Abs(currentSpeed);
        float directionalMaxSpeed = currentSpeed >= 0f ? maxForwardSpeed : maxReverseSpeed;
        float normalizedSpeed = directionalMaxSpeed > 0f ? Mathf.Clamp01(speedAbs / directionalMaxSpeed) : 0f;
        float speedScaledTurnMagnitude = speedAbs < minimumSteerSpeed
            ? 0f
            : Mathf.Lerp(turnMagnitude, turnMagnitude * highSpeedTurnMultiplier, normalizedSpeed);

        transform.Rotate(0f, turnInput * turnSpeed * speedScaledTurnMagnitude * Time.deltaTime, 0f);
        transform.Translate(Vector3.right * currentSpeed * Time.deltaTime, Space.Self);
    }
}
