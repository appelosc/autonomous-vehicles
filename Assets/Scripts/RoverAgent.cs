using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;


public class RoverAgent : Agent
{
    public float enginePower = 2000.0f; // Engine power

    public float maxDistance = 150f; // between meeting point and rover in stage 1

    public float stopRadius = 10f; // radius for stopping
    public float stopSpeed = 0.5f; // speed for stopping
    public float slowRadius = 25f; // radie där rovern ska börja bromsa in

    public float maxSpeed = 25f;          // framåt, m/s
    public float maxReverseSpeed = 5f;    // bakåt, m/s
    public float maxAngularSpeed = 3f;    // rad/s, används för att normalisera rotationshastigheten

    [SerializeField] bool demoMode = false;
    bool arrived;
    float prevDist;

    public float brakePower = 3000f; // brakePower
    public float turnSpeed = 25.0f; // Maximum turn speed
    public float turnSmoothness = 5.0f; // Turn smoothness
    public Transform[] wheels; // Array of Wheel Collider components
    public Transform[] wheelMeshes; // Array of wheel meshes
    public Transform centerOfMass; // Center of Mass point
    public GameObject steeringWheel;

    public GameObject drone; // Reference to the drone GameObject



    private Rigidbody rb; // Car's Rigidbody component

    private Transform target;
    private float currentTurnAngle = 0.0f;
    private float horizontalInput = 0.0f;
    private float throttle = 0.0f;

    [SerializeField] float holdTime = 2f;   // sekunder
    float holdTimer;

    [SerializeField] float landingTime = 2f;   // sekunder
    float landingTimer;

    bool droneOnTruck;

    EnviromentController env;
    
    WheelCollider[] wheelColliders;

    public override void Initialize()
    {
        env = GetComponentInParent<EnviromentController>();
        
        target = env.meetingPoint.transform;
        
        rb = GetComponent<Rigidbody>();
        
        rb.centerOfMass = centerOfMass.localPosition;
        
        wheelColliders = new WheelCollider[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            wheelColliders[i] = wheels[i].GetComponent<WheelCollider>();
        }
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
       
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0; // 0 stilla, 1 framåt, 2 bakåt.
        discreteActionsOut[1] = 0; // 0 stilla,1 vänster, 2 höger

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[1] = 1;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 2;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {   // kollar ifall rovern har vält eller om den är för långt ner under mötespunkten, då avslutas episoden med straff
        if (transform.up.y < 0.3f || transform.position.y < env.transform.position.y - 5f)
        {
            AddReward(-1f);
            EndEpisode();
            return;
        }

        if (arrived)
        {
            throttle = 0f;
            horizontalInput = 0f;
            return;   // hoppa över resten: policyns actions och belöningar
        }
        int movementAction = actions.DiscreteActions[0]; // 0 = stilla 1= framåt 2= bakåt
        int rotationAction = actions.DiscreteActions[1]; // 0 = stilla, 1 = vänster, 2= höger
        horizontalInput = 0.0f;
        throttle = 0.0f;

        if (rotationAction == 1) horizontalInput = -1.0f;
        if (rotationAction == 2) horizontalInput = 1.0f;

        if (movementAction == 1) throttle = 1.0f;
        if (movementAction == 2) throttle = -1.0f;

        if (MaxStep > 0) AddReward(-1f / MaxStep);

        float dist = DistanceToTarget();          // meter
        float speed = rb.linearVelocity.magnitude; // m/s

        AddReward((prevDist - dist) / maxDistance);
        prevDist = dist;
        // lyckat: stått still i målet i holdTime sekunder
        if (dist < stopRadius && speed < stopSpeed)
        {
            holdTimer += Time.fixedDeltaTime;
            if (holdTimer >= holdTime)
            {
                AddReward(2f);
                if (demoMode) arrived = true;
                else EndEpisode();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }



    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject == drone.gameObject)
            droneOnTruck = true;    // kontakten börjar
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.gameObject == drone.gameObject)
        {
            droneOnTruck = false;   // drönaren lyfte eller studsade av
            landingTimer = 0f;
        }
    }
    // unitys (sleep funktion) vill att spelet tar slut 2 sek efter jag landat med drönaren


    public override void OnEpisodeBegin()
    {
        env.ResetRoverPosition();
        throttle = 0f;
        horizontalInput = 0f;
        currentTurnAngle = 0f;
        holdTimer = 0f;
        landingTimer = 0f;
        droneOnTruck = false;
        arrived = false;
        prevDist = DistanceToTarget();
        
        StopAllCoroutines();
    }

    // Hjälpfunktioner för belöningssytem samt observationern
    float DistanceToTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        return toTarget.magnitude;
    }

    Vector3 LocalVelocity()
    {
        return transform.InverseTransformDirection(rb.linearVelocity);
    }

    Vector3 LocalDirection()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        return transform.InverseTransformDirection(toTarget.normalized);
    }

    float AngleSpeed()
    {
        // angularVelocity är i rad/s, så vi normaliserar med maxAngularSpeed (inte turnSpeed som är styrvinkel i grader)
        return rb.angularVelocity.y / maxAngularSpeed;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Distanse mellan rover och mötespunkt
        sensor.AddObservation(Mathf.Clamp01(DistanceToTarget() / maxDistance));

        //Roverns lokala hastighet
        Vector3 localVelocity = LocalVelocity();
        sensor.AddObservation(Mathf.Clamp(localVelocity.z / maxSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localVelocity.x / maxSpeed, -1f, 1f));

        //Roverns lokala riktning
        Vector3 localDirection = LocalDirection();
        sensor.AddObservation(localDirection.x);
        sensor.AddObservation(localDirection.z);

        //Roverns rotationshastighet
        sensor.AddObservation(Mathf.Clamp(AngleSpeed(), -1f, 1f));

        // Roverns lutning
        sensor.AddObservation(transform.right.y);
        sensor.AddObservation(transform.forward.y);
    }

    void FixedUpdate()
    {

        if (droneOnTruck && arrived)
        {
            landingTimer += Time.fixedDeltaTime;
            if (landingTimer >= landingTime)
            {
                landingTimer = 0f;
                droneOnTruck = false;
                EndEpisode();
            }
        }
        // Calculate turn angle
        float targetTurnAngle = horizontalInput * turnSpeed;
        currentTurnAngle = Mathf.Lerp(currentTurnAngle, targetTurnAngle, Time.deltaTime * turnSmoothness);

        steeringWheel.transform.localEulerAngles = new Vector3(-64, 0, currentTurnAngle * 3);

        // Begränsa toppfarten: motorn slutar dra när maxfarten är nådd.
        // Fart längs roverns körriktning: positiv framåt, negativ bakåt
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float motor = throttle * enginePower;

        if (throttle > 0f && forwardSpeed >= maxSpeed) motor = 0f;          // max framåt nådd, sluta gasa
        if (throttle < 0f && forwardSpeed <= -maxReverseSpeed) motor = 0f;  // max bakåt nådd, sluta backa
        // Obs: bromsen slår inte till här eftersom throttle fortfarande != 0, rovern rullar jämnt i toppfart

        // Wheels rotation and steering
        for (int i = 0; i < wheels.Length; i++)
        {
            WheelCollider wheelCollider = wheelColliders[i];

            if (i < 2) // First two wheels steer
            {
                wheelCollider.steerAngle = currentTurnAngle;
            }
            else
            {
                wheelCollider.steerAngle = 0f; // Other two wheels remain straight
            }

            // Wheels driving (forward/backward movement)
            wheelCollider.motorTorque = motor; // begränsat motorvridmoment från ovan
            wheelCollider.brakeTorque = (throttle == 0f) ? brakePower : 0f; // braking when throttle is zero

            // Rotate wheel meshes
            Quaternion wheelRotation;
            Vector3 wheelPosition;
            wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);
            wheelMeshes[i].position = wheelPosition;
            wheelMeshes[i].rotation = wheelRotation;
        }
    }
}