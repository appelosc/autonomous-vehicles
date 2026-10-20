using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    public float thrustChangeRate = 10.0f;
    public float maximumThrustOffset = 20.0f;
    public float thrustReturnRate = 10.0f;
    public float tiltChangeRate = 30.0f;
    public float tiltReturnRate = 60.0f;        // hur snabbt lutningen går tillbaka till 0 när W/S/A/D släpps
    public float tiltStabilizationTorque = 20.0f;
    public float angularDamping = 5.0f;
    public float maximumTiltAngle = 40.0f;
    public float minimumCosine = 0.1f;

    [Header("Inbromsning när inga knappar hålls in")]
    public bool brakeWhenIdle = true;
    public float horizontalBrake = 2.0f;        // högre = stannar snabbare i sidled
    public float verticalBrake = 3.0f;          // högre = slutar stiga/sjunka snabbare

    [Header("Vridning (Q/E)")]
    public float yawSpeed = 90.0f;              // grader per sekund drönaren vrider sig när Q/E hålls in

    private Rigidbody rb;
    private float thrustOffset;
    private float targetPitch;
    private float targetRoll;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationY;
    }

    private void FixedUpdate()
    {
        ApplyYaw();                 // vrid drönaren med Q/E (kameran följer med)
        UpdateThrustOffset();
        UpdateTargetTilt();
        ApplyThrust();
        ApplyTiltTorque();
        LimitTiltAngle();
        ApplyIdleBrake();
    }

    // vrider drönaren runt världens Y-axel med Q (vänster) och E (höger)
    private void ApplyYaw()
    {
        float yawInput = 0.0f;                                   // ingen vridning som standard
        if (Input.GetKey(KeyCode.Q)) yawInput -= 1.0f;           // Q = vrid åt vänster
        if (Input.GetKey(KeyCode.E)) yawInput += 1.0f;           // E = vrid åt höger
        if (yawInput == 0.0f) return;                            // ingen knapp -> gör ingenting

        float angle = yawInput * yawSpeed * Time.fixedDeltaTime; // hur många grader detta steg
        rb.rotation = Quaternion.AngleAxis(angle, Vector3.up) * rb.rotation;   // vrid runt världens upp-axel (lutningen behålls)
    }

    private void UpdateThrustOffset()
    {
        if (Input.GetKey(KeyCode.R))
        {
            thrustOffset += thrustChangeRate * Time.fixedDeltaTime;
        }
        else if (Input.GetKey(KeyCode.F))
        {
            thrustOffset -= thrustChangeRate * Time.fixedDeltaTime;
        }
        else
        {
            thrustOffset = Mathf.MoveTowards(
                thrustOffset,
                0.0f,
                thrustReturnRate * Time.fixedDeltaTime);
        }

        thrustOffset = Mathf.Clamp(
            thrustOffset,
            -maximumThrustOffset,
            maximumThrustOffset);
    }

    private void ApplyThrust()
    {
        float cosine = Vector3.Dot(transform.up.normalized, Vector3.up);
        cosine = Mathf.Max(cosine, minimumCosine);

        float hoverThrust = (rb.mass * Physics.gravity.magnitude) / cosine;
        float thrustForce = hoverThrust + thrustOffset;

        rb.AddForce(transform.up * thrustForce, ForceMode.Force);
    }

    private void ApplyTiltTorque()
    {
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 localAngles = GetLocalTiltAngles();

        Vector3 localTorque = new Vector3(
            (targetPitch - localAngles.x) * tiltStabilizationTorque -
            localAngularVelocity.x * angularDamping,
            0.0f,
            (targetRoll - localAngles.z) * tiltStabilizationTorque -
            localAngularVelocity.z * angularDamping);

        rb.AddRelativeTorque(localTorque, ForceMode.Force);
    }

    private void UpdateTargetTilt()
    {
        bool pitchInput = false;
        bool rollInput = false;

        if (Input.GetKey(KeyCode.W))
        {
            targetPitch += tiltChangeRate * Time.fixedDeltaTime;
            pitchInput = true;
        }
        if (Input.GetKey(KeyCode.S))
        {
            targetPitch -= tiltChangeRate * Time.fixedDeltaTime;
            pitchInput = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            targetRoll -= tiltChangeRate * Time.fixedDeltaTime;
            rollInput = true;
        }
        if (Input.GetKey(KeyCode.A))
        {
            targetRoll += tiltChangeRate * Time.fixedDeltaTime;
            rollInput = true;
        }

        // ingen knapp: lutningen går tillbaka mot plant läge (sväva)
        if (!pitchInput)
            targetPitch = Mathf.MoveTowards(targetPitch, 0.0f, tiltReturnRate * Time.fixedDeltaTime);
        if (!rollInput)
            targetRoll = Mathf.MoveTowards(targetRoll, 0.0f, tiltReturnRate * Time.fixedDeltaTime);

        targetPitch = Mathf.Clamp(targetPitch, -maximumTiltAngle, maximumTiltAngle);
        targetRoll = Mathf.Clamp(targetRoll, -maximumTiltAngle, maximumTiltAngle);
    }

    // bromsar farten mjukt när inga knappar hålls in, så att drönaren stannar och svävar på stället
    private void ApplyIdleBrake()
    {
        if (!brakeWhenIdle) return;

        bool horizontalInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
                               Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
        bool verticalInput = Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.F);

        Vector3 v = rb.linearVelocity;

        if (!horizontalInput)
            rb.AddForce(new Vector3(-v.x, 0f, -v.z) * horizontalBrake, ForceMode.Acceleration);

        if (!verticalInput)
            rb.AddForce(new Vector3(0f, -v.y, 0f) * verticalBrake, ForceMode.Acceleration);
    }

    private Vector3 GetLocalTiltAngles()
    {
        Quaternion localRotation = transform.parent == null
            ? rb.rotation
            : Quaternion.Inverse(transform.parent.rotation) * rb.rotation;

        Vector3 localAngles = localRotation.eulerAngles;
        localAngles.x = NormalizeAngle(localAngles.x);
        localAngles.z = NormalizeAngle(localAngles.z);
        return localAngles;
    }

    private void LimitTiltAngle()
    {
        Quaternion localRotation = transform.parent == null
            ? rb.rotation
            : Quaternion.Inverse(transform.parent.rotation) * rb.rotation;

        Vector3 localAngles = localRotation.eulerAngles;
        localAngles.x = NormalizeAngle(localAngles.x);
        localAngles.z = NormalizeAngle(localAngles.z);

        float limitedX = Mathf.Clamp(localAngles.x, -maximumTiltAngle, maximumTiltAngle);
        float limitedZ = Mathf.Clamp(localAngles.z, -maximumTiltAngle, maximumTiltAngle);

        if (Mathf.Approximately(localAngles.x, limitedX) &&
            Mathf.Approximately(localAngles.z, limitedZ))
        {
            return;
        }

        Quaternion limitedLocalRotation = Quaternion.Euler(
            limitedX,
            localRotation.eulerAngles.y,
            limitedZ);

        rb.MoveRotation(transform.parent == null
            ? limitedLocalRotation
            : transform.parent.rotation * limitedLocalRotation);
    }

    private float NormalizeAngle(float angle)
    {
        return angle > 180.0f ? angle - 360.0f : angle;
    }
}