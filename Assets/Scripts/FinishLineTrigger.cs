using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLineTrigger : MonoBehaviour
{
    [SerializeField] private RaceLapTimer raceLapTimer;

    void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceLapTimer == null)
        {
            return;
        }

        DriveCar driveCar = other.GetComponentInParent<DriveCar>();
        if (driveCar == null)
        {
            return;
        }

        raceLapTimer.NotifyFinishCrossed(driveCar);
    }
}
