using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLineTrigger : MonoBehaviour
{
    private enum GateType
    {
        Mid,
        Finish
    }

    [SerializeField] private GateType gateType = GateType.Finish;
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

        if (gateType == GateType.Mid)
        {
            raceLapTimer.NotifyMidCrossed(driveCar);
        }
        else
        {
            raceLapTimer.NotifyFinishCrossed(driveCar);
        }
    }
}
