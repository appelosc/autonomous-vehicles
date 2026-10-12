using UnityEngine;

public class EnviromentController : MonoBehaviour
{
    public GameObject meetingPoint;
    public GameObject rover;

    

    private Rigidbody roverRb;

    private Vector3 originalRoverPosition;
    private Quaternion originalRoverRotation;
    private Vector3 originalMeetingPointPosition;

    void Awake()
    {
        roverRb = rover.GetComponent<Rigidbody>();

        Debug.Log("Avstånd mellan rover och mötespunkt: " + Vector3.Distance(rover.transform.position, meetingPoint.transform.position));
        originalRoverPosition = rover.transform.localPosition;
        originalRoverRotation = rover.transform.localRotation;
        originalMeetingPointPosition = meetingPoint.transform.localPosition;
    }

    public void ResetRoverPosition()
    {
        
        roverRb.linearVelocity = Vector3.zero;
        roverRb.angularVelocity = Vector3.zero;

        rover.transform.localPosition = originalRoverPosition;
        rover.transform.localRotation = originalRoverRotation;
        meetingPoint.transform.localPosition = originalMeetingPointPosition;
    }
}