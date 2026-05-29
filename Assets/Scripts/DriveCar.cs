using UnityEngine;

public class DriveCar : MonoBehaviour
{
    [SerializeField] private float maxForwardSpeed = 8f;
    [SerializeField] private float maxReverseSpeed = 4f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float friction = 4f;
    [SerializeField] private float turnSpeed = 120f;

    private float currentSpeed;

    void Update()
    {
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

        transform.Rotate(0f, turnInput * turnSpeed * Time.deltaTime, 0f);
        transform.Translate(Vector3.right * currentSpeed * Time.deltaTime, Space.Self);
    }
}
