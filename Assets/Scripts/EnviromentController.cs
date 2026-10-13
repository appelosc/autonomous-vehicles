
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
    public Collider terrainCollider;
    public float margin = 50f;

    private float maxMeetingDistance = 200f;



    void Awake()
    {
        if (drone != null)
        {
            droneRb = drone.GetComponent<Rigidbody>();
            originalDronePosition = drone.transform.localPosition;
            originalDroneRotation = drone.transform.localRotation;
        }
        roverRb = rover.GetComponent<Rigidbody>();
        

        originalRoverPosition = rover.transform.localPosition;
        originalRoverRotation = rover.transform.localRotation;
        originalMeetingPointPosition = meetingPoint.transform.localPosition;
        
    }

    Vector3 RandomPosition()
    {
        Bounds b = terrainCollider.bounds;          // världskoordinater, 500x500
        float x = Random.Range(b.min.x + margin, b.max.x - margin);
        float z = Random.Range(b.min.z + margin, b.max.z - margin);
        return new Vector3(x, b.max.y + 5f, z);     // börja ovanför terrängen
    }


    // funktion som användes endast i träning, så man kunde öka maximala avståndet från meetingpoint till target.
    Vector3 MeetingPointRandomPosition(Vector3 roverPosition, Vector3 meetingPointPosition)
    {
    // 1. Slumpa en punkt runt rovern, åt vilket håll som helst
    float x = roverPosition.x + Random.Range(-maxMeetingDistance, maxMeetingDistance);
    float z = roverPosition.z + Random.Range(-maxMeetingDistance, maxMeetingDistance);

    // 2. Se till att punkten inte hamnar utanför terrängen (samma gränser som rovern)
    Bounds b = terrainCollider.bounds;
    x = Mathf.Clamp(x, b.min.x + margin, b.max.x - margin);
    z = Mathf.Clamp(z, b.min.z + margin, b.max.z - margin);

    return new Vector3(x, meetingPointPosition.y, z);
    }

    public void ResetRoverPosition()
    {
        
        
        roverRb.linearVelocity = Vector3.zero;
        roverRb.angularVelocity = Vector3.zero;
        Vector3 randomPosition = RandomPosition();

        rover.transform.position = randomPosition;
        rover.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        Vector3 mpRandomPosition = RandomPosition();
        while (Vector3.Distance(mpRandomPosition, randomPosition) < 20f) mpRandomPosition = RandomPosition();
        meetingPoint.transform.position = mpRandomPosition;
    }

    public void ResetDronePosition()
    {
        if (drone == null) return; 
        droneRb.linearVelocity = Vector3.zero;
        droneRb.angularVelocity = Vector3.zero;

        drone.transform.localPosition = originalDronePosition;
        drone.transform.localRotation = originalDroneRotation;
    }
}