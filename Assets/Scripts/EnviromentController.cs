using UnityEngine;
using System.Collections.Generic;

public class EnviromentController : MonoBehaviour
{
    public GameObject meetingPoint;
    public GameObject rover;
    public GameObject drone; // Reference to the drone GameObject

    public Collider terrainCollider;

    public float margin = 50f;
    public float roverClearance = 1.5f;
    public float meetingPointClearance = 5f;     // cylinderns radie + marginal
    public float minRoverMeetingDistance = 20f;

    // --- slumpade hinder ---
    [Header("Slumpade hinder")]
    public bool randomizeObstacles = true;       // av/på för att slumpa hindrens positioner varje episod
    public float obstacleGap = 6f;               // minsta fria avstånd mellan två hinder (så rovern kan köra mellan)
    [Range(0f, 1f)]
    public float rockActiveChance = 0.7f;        // sannolikhet att en sten är med i en episod -> varierande antal stenar

    private Rigidbody droneRb;
    private Rigidbody roverRb;

    private Vector3 originalRoverPosition;
    private Vector3 originalMeetingPointPosition;
    private Vector3 originalDronePosition;

    private Quaternion originalRoverRotation;
    private Quaternion originalDroneRotation;

    private float maxMeetingDistance = 200f;
    private const int MaxTries = 100;

    private Bounds[] obstacleBounds = new Bounds[0];
    private Transform[] obstacles = new Transform[0];
    private float[] obstacleRadius;                     // ungefärlig radie för varje hinder i xz-planet (används för överlapp)
    private float[] obstacleHeightOffset;               // hur högt över marken hindret stod från början
    private Quaternion[] obstacleOriginalRotation;      // hindrets ursprungliga rotation (så lutning/form behålls)

    // Sparar rigidbodies och startpositioner, samt hittar miljöns hinder en gång vid start
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

        CollectObstacles();
    }

    // Hittar alla hinder (Building/Rock) och sparar storlek och höjd över marken genom att gå igenom alla child objects
    void CollectObstacles()
    {
        List<Transform> list = new List<Transform>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (!IsObstacleTag(t)) continue;
            if (t.parent != null && HasTaggedAncestor(t.parent)) continue;  // barn till ett hinder hoppas över, annars flyttas det två gånger
            list.Add(t);
        }
        obstacles = list.ToArray();

        obstacleRadius = new float[obstacles.Length];
        obstacleHeightOffset = new float[obstacles.Length];
        obstacleOriginalRotation = new Quaternion[obstacles.Length];

        // radie i xz-planet, ursprunglig rotation och pivotens höjd över marken för varje hinder
        for (int i = 0; i < obstacles.Length; i++)
        {
            Bounds b = CombinedBounds(obstacles[i]);
            obstacleRadius[i] = Mathf.Max(b.extents.x, b.extents.z);
            obstacleOriginalRotation[i] = obstacles[i].rotation;

            float ground = GroundHeight(obstacles[i].position);
            obstacleHeightOffset[i] = obstacles[i].position.y - ground;
        }
    }

    //kollar ifall objektet är ett hinder eller ej
    bool IsObstacleTag(Transform t)
    {
        return t.CompareTag("Building") || t.CompareTag("Rock");
    }

    // börjar från barnobjekt och kollar ifall objektet har tag, annars upp till parent objektet och gör samma check
    bool HasTaggedAncestor(Transform t)
    {
        while (t != null && t != transform)
        {
            if (IsObstacleTag(t)) return true;
            t = t.parent;
        }
        return false;
    }

    // En ruta som täcker alla colliders i hindret (liten standardruta om det saknas colliders)
    Bounds CombinedBounds(Transform t)
    {
        Collider[] cols = t.GetComponentsInChildren<Collider>(true);
        if (cols.Length == 0) return new Bounds(t.position, Vector3.one);
        Bounds b = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
        return b;
    }

    // Marknivå under en punkt (raycast nedåt mot terrängen)
    // missar strålen terrängen används terrängens lägsta punkt som reserv
    float GroundHeight(Vector3 p)
    {
        Bounds tb = terrainCollider.bounds;
        Ray ray = new Ray(new Vector3(p.x, tb.max.y + 50f, p.z), Vector3.down);
        if (terrainCollider.Raycast(ray, out RaycastHit hit, tb.size.y + 100f))
            return hit.point.y;
        return tb.min.y;
    }


    //function för all randomizelogik: slumpa hinder, spara deras bounds och placera rover och meeting point utanför hindren
    public void ResetEpisode()
    {
        if (randomizeObstacles) RandomizeObstacles();
        BuildObstacleBounds();
        ResetRoverPosition();
    }

    // Slumpar vilka stenar som är med (byggnader är alltid med) och ger varje aktivt hinder
    // en ny position som inte överlappar redan placerade hinder, samt en slumpad vridning runt Y-axeln
    void RandomizeObstacles()
    {
        List<Vector3> placedPos = new List<Vector3>();
        List<float> placedRadius = new List<float>();

        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i].CompareTag("Rock"))
                obstacles[i].gameObject.SetActive(Random.value < rockActiveChance);
            if (!obstacles[i].gameObject.activeSelf) continue;

            float r = obstacleRadius[i];
            Vector3 p = RandomPosition();
            int tries = 1;
            while (OverlapsPlaced(p, r, placedPos, placedRadius) && tries < MaxTries)
            {
                p = RandomPosition();
                tries++;
            }

            p.y = GroundHeight(p) + obstacleHeightOffset[i];                 // ställ hindret på marken med samma höjd som från början
            obstacles[i].position = p;
            obstacles[i].rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * obstacleOriginalRotation[i];

            placedPos.Add(p);
            placedRadius.Add(r);
        }
    }

    // Sant om p ligger för nära ett redan placerat hinder (båda radierna + obstacleGap, mätt i xz-planet)
    bool OverlapsPlaced(Vector3 p, float r, List<Vector3> pos, List<float> radius)
    {
        for (int j = 0; j < pos.Count; j++)
        {
            float dx = p.x - pos[j].x;
            float dz = p.z - pos[j].z;
            float minDist = r + radius[j] + obstacleGap;
            if (dx * dx + dz * dz < minDist * minDist) return true;
        }
        return false;
    }

    // Sparar bounds för den här miljöns aktiva hinder, avstängda stenar ska inte blockera rover/mål
    void BuildObstacleBounds()
    {
        Physics.SyncTransforms();   // så bounds stämmer även om hindren just flyttats

        List<Bounds> list = new List<Bounds>();
        foreach (Transform t in obstacles)
        {
            if (!t.gameObject.activeSelf) continue;
            foreach (Collider c in t.GetComponentsInChildren<Collider>())
                list.Add(c.bounds);
        }

        obstacleBounds = list.ToArray();
    }
    // Sant om punkten ligger inom något hinders ruta, utökad med radius
    bool IsBlocked(Vector3 p, float radius)
    {
        foreach (Bounds b in obstacleBounds)
        {
            if (p.x > b.min.x - radius && p.x < b.max.x + radius &&
                p.z > b.min.z - radius && p.z < b.max.z + radius)
                return true;
        }
        return false;
    }
    // Slumpar en position som inte är blockerad av ett hinder (högst MaxTries försök)
    Vector3 FreePosition(float radius)
    {
        Vector3 p = RandomPosition();
        int tries = 1;
        while (IsBlocked(p, radius) && tries < MaxTries)
        {
            p = RandomPosition();
            tries++;
        }
        return p;
    }

    // Slumpad punkt på terrängen (världskoordinater, 500x500) innanför margin, med höjd ovanför terrängen
    Vector3 RandomPosition()
    {
        Bounds b = terrainCollider.bounds;
        float x = Random.Range(b.min.x + margin, b.max.x - margin);
        float z = Random.Range(b.min.z + margin, b.max.z - margin);
        return new Vector3(x, b.max.y + 5f, z);
    }


    // funktion som användes endast i träning, så man kunde öka maximala avståndet från meetingpoint till target.
    // slumpar en punkt runt rovern och håller den innanför terrängens gränser (samma som rovern)
    Vector3 MeetingPointRandomPosition(Vector3 roverPosition, Vector3 meetingPointPosition)
    {
        float x = roverPosition.x + Random.Range(-maxMeetingDistance, maxMeetingDistance);
        float z = roverPosition.z + Random.Range(-maxMeetingDistance, maxMeetingDistance);

        Bounds b = terrainCollider.bounds;
        x = Mathf.Clamp(x, b.min.x + margin, b.max.x - margin);
        z = Mathf.Clamp(z, b.min.z + margin, b.max.z - margin);

        return new Vector3(x, meetingPointPosition.y, z);
    }


    // Nollar roverns hastighet och placerar den på en fri plats med slumpad riktning,
    // sedan placeras mötespunkten på en fri plats minst minRoverMeetingDistance bort
    public void ResetRoverPosition()
    {
        roverRb.linearVelocity = Vector3.zero;
        roverRb.angularVelocity = Vector3.zero;

        Vector3 roverPos = FreePosition(roverClearance);
        rover.transform.position = roverPos;
        rover.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        Vector3 mpPos = FreePosition(meetingPointClearance);
        int tries = 1;
        while (Vector3.Distance(mpPos, roverPos) < minRoverMeetingDistance && tries < MaxTries)
        {
            mpPos = FreePosition(meetingPointClearance);
            tries++;
        }
        meetingPoint.transform.position = mpPos;
    }

    // Nollar drönarens hastighet och flyttar tillbaka den till startpositionen
    public void ResetDronePosition()
    {
        if (drone == null) return;
        droneRb.linearVelocity = Vector3.zero;
        droneRb.angularVelocity = Vector3.zero;

        drone.transform.localPosition = originalDronePosition;
        drone.transform.localRotation = originalDroneRotation;
    }
}
