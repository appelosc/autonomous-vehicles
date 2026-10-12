using UnityEngine;

public class EnviromentController : MonoBehaviour
{
    public GameObject meetingPoint;
    public GameObject rover;

    public GameObject drone; // Reference to the drone GameObject

    private Rigidbody droneRb; // Car's Rigidbody component

    

    private Rigidbody roverRb;
    private Vector3 originalRoverPosition;
    private Quaternion originalRoverRotation;
    private Vector3 originalMeetingPointPosition;
    private Vector3 originalDronePosition;
    private Quaternion originalDroneRotation;

    void Awake()
    {
        roverRb = rover.GetComponent<Rigidbody>();
        droneRb = drone.GetComponent<Rigidbody>();

        originalRoverPosition = rover.transform.localPosition;
        originalRoverRotation = rover.transform.localRotation;
        originalMeetingPointPosition = meetingPoint.transform.localPosition;
        originalDronePosition = drone.transform.localPosition;
        originalDroneRotation = drone.transform.localRotation;
    }

    public void ResetRoverPosition()
    {
        
        roverRb.linearVelocity = Vector3.zero;
        roverRb.angularVelocity = Vector3.zero;

        rover.transform.localPosition = originalRoverPosition;
        rover.transform.localRotation = originalRoverRotation;
        meetingPoint.transform.localPosition = originalMeetingPointPosition;
    }

    public void ResetDronePosition()
    {
        droneRb.linearVelocity = Vector3.zero;
        droneRb.angularVelocity = Vector3.zero;

        drone.transform.localPosition = originalDronePosition;
        drone.transform.localRotation = originalDroneRotation;
    }
}