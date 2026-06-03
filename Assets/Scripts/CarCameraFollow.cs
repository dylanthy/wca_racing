using UnityEngine;

public class CarCameraFollow : MonoBehaviour
{
    private enum ForwardAxis
    {
        Forward,
        Right
    }

    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Target Orientation")]
    [SerializeField] private ForwardAxis targetForwardAxis = ForwardAxis.Right;

    [Header("Follow")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float height = 3f;
    [SerializeField] private float sideOffset = 0f;
    [SerializeField] private float lookAhead = 1.5f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.12f;
    [SerializeField] private float rotationSharpness = 10f;

    [Header("Look At")]
    [SerializeField] private float lookAtHeight = 1.2f;

    private Vector3 positionVelocity;
    private Vector3 lastPlanarForward = Vector3.forward;

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 rawForward = targetForwardAxis == ForwardAxis.Forward ? target.forward : target.right;
        Vector3 planarForward = Vector3.ProjectOnPlane(rawForward, Vector3.up);

        if (planarForward.sqrMagnitude > 0.0001f)
        {
            lastPlanarForward = planarForward.normalized;
        }

        Vector3 desiredPosition = target.position
            + Vector3.up * height
            - lastPlanarForward * distance
            + Vector3.Cross(Vector3.up, lastPlanarForward) * sideOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            Mathf.Max(0.0001f, positionSmoothTime));

        Vector3 lookPoint = target.position + Vector3.up * lookAtHeight + lastPlanarForward * lookAhead;
        Vector3 lookDirection = lookPoint - transform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, rotationSharpness) * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }
    }
}