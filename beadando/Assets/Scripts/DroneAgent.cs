using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneAgent : Agent
{
    [Header("References")]
    public Transform deliveryTarget;
    public EnvironmentManager envManager;

    [Header("Drone Physics")]
    public float maxThrust = 12f;
    public float maxTorque = .5f;
    public float maxSpeed = 5f;
    public float dragCoefficient = 0.5f;

    [Header("Reward Settings")]
    public float deliveryRadius = 2f;
    public float deliveryReward = 25.0f;
    public float collisionPenalty = -1.5f;
    public float timePenalty = -0.0002f;
    public float distanceRewardScale = 0.5f;
    public float stabilityRewardScale = 0.001f;
    public float aliveReward = 0.001f;

    [Header("Boundaries")]
    public float maxHeight = 50f;
    public float minHeight = 1f;
    public Vector3 areaBounds = new Vector3(50f, 50f, 50f);

    [Header("Auto-Stabilization")]
    [Tooltip("Mennyire segít az ágenst egyenesen tartani. 1.0 = erős segítség, 0.0 = nincs.")]
    [Range(0f, 1f)]
    public float stabilizationHelp = 0.3f;

    [Header("Difficulty (Nehézség)")]
    [Tooltip("0 = könnyű (közeli target, nincs akadály), 1 = közepes, 2 = nehéz")]
    [Range(0, 2)]
    public int difficultyLevel = 0;

    [Header("Spawn Settings")]
    public float easyMaxSpawnDistance = 15f;
    public float mediumMaxSpawnDistance = 25f;
    public float hardMaxSpawnDistance = 40f;

    private Rigidbody rb;
    private Vector3 startPos;
    private Quaternion startRot;
    private float previousDistance;
    private int stepCount;
    private int successCount = 0;
    private int episodeCount = 0;
    private float gravityCompensation;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.drag = dragCoefficient;
        rb.angularDrag = 2.0f;
        startPos = transform.localPosition;
        startRot = transform.localRotation;

        gravityCompensation = rb.mass * Physics.gravity.magnitude;

        if (MaxStep == 0)
        {
            MaxStep = 10000;
        }

        if (envManager == null)
            Debug.LogError("[DroneAgent] envManager NINCS BEÁLLÍTVA!");

        if (deliveryTarget == null)
            Debug.LogError("[DroneAgent] deliveryTarget NINCS BEÁLLÍTVA!");

        Debug.Log($"[DroneAgent] Inicializálva. Nehézség: {difficultyLevel}, " +
                  $"Stabilizáció: {stabilizationHelp}, MaxStep: {MaxStep}");
    }

    public override void OnEpisodeBegin()
    {
        
        episodeCount++;

        if (episodeCount >= 50 )
        {
            float successRate = (float)successCount / episodeCount;
            Debug.Log($"[DroneAgent] Level {difficultyLevel} - Sikerráta: {successRate:P0} ({successCount}/{episodeCount})");

            if (difficultyLevel == 0 && successRate > 0.5f)
            {
                difficultyLevel = 1;
                Debug.Log($"[DroneAgent] >>> NEHÉZSÉG NÖVELVE → Level 1!");
            }
            else if (difficultyLevel == 1 && successRate > 0.5f)
            {
                difficultyLevel = 2;
                Debug.Log($"[DroneAgent] >>> NEHÉZSÉG NÖVELVE → Level 2!");
            }

            episodeCount = 0;
            successCount = 0;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float spawnRange;
        float targetRange;

        switch (difficultyLevel)
        {
            case 0: 
                spawnRange = 5f;
                targetRange = easyMaxSpawnDistance;
                break;
            case 1:
                spawnRange = areaBounds.x * 0.25f;
                targetRange = mediumMaxSpawnDistance;
                break;
            default: 
                spawnRange = areaBounds.x * 0.4f;
                targetRange = hardMaxSpawnDistance;
                break;
        }

        transform.localPosition = new Vector3(
            Random.Range(-spawnRange, spawnRange),
            Random.Range(10f, 30f),
            Random.Range(-spawnRange, spawnRange)
        );
        transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (deliveryTarget != null)
        {
            Vector3 dronePos = transform.localPosition;

            for (int attempt = 0; attempt < 50; attempt++)
            {
                Vector2 randomDir2D = Random.insideUnitCircle.normalized;
                float dist = Random.Range(5f, targetRange);

                Vector3 candidatePos = new Vector3(
                    Mathf.Clamp(dronePos.x + randomDir2D.x * dist, -areaBounds.x * 0.4f, areaBounds.x * 0.4f),
                    Mathf.Clamp(dronePos.y + Random.Range(-10f, 10f), 5f, maxHeight * 0.8f),
                    Mathf.Clamp(dronePos.z + randomDir2D.y * dist, -areaBounds.z * 0.4f, areaBounds.z * 0.4f)
                );

                Vector3 worldPos = transform.parent != null
                    ? transform.parent.TransformPoint(candidatePos)
                    : candidatePos;

                float candidateDist = Vector3.Distance(dronePos, candidatePos);
                if (!Physics.CheckSphere(worldPos, deliveryRadius * 0.5f) && candidateDist > 10f)
                {
                    deliveryTarget.localPosition = candidatePos;
                    break;
                }

                if (attempt == 49)
                {
                    Vector2 fallbackDir = Random.insideUnitCircle.normalized;
                    deliveryTarget.localPosition = new Vector3(
                        dronePos.x + fallbackDir.x * 10f,
                        Mathf.Max(dronePos.y + 5f, 25f),
                        dronePos.z + fallbackDir.y * 10f
                    );
                }
            }
        }

        if (envManager != null)
        {
            switch (difficultyLevel)
            {
                case 0:
                    envManager.minObstacles = 0;
                    envManager.maxObstacles = 1;
                    envManager.enableWind = false;
                    break;
                case 1:
                    envManager.minObstacles = 1;
                    envManager.maxObstacles = 2;
                    envManager.enableWind = true;
                    envManager.maxWindForce = .2f;
                    break;
                default:
                    envManager.minObstacles = 2;
                    envManager.maxObstacles = 3;
                    envManager.enableWind = true;
                    envManager.maxWindForce = .5f;
                    break;
            }
            envManager.ResetEnvironment();
        }

        previousDistance = Vector3.Distance(transform.localPosition, deliveryTarget.localPosition);
        stepCount = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 normalizedPos = new Vector3(
            transform.localPosition.x / areaBounds.x,
            transform.localPosition.y / maxHeight,
            transform.localPosition.z / areaBounds.z
        );
        sensor.AddObservation(normalizedPos); 

        sensor.AddObservation(rb.velocity / maxSpeed); 
        sensor.AddObservation(transform.forward); 
        sensor.AddObservation(rb.angularVelocity / maxTorque); 

        Vector3 toTarget = deliveryTarget.localPosition - transform.localPosition;
        float distance = toTarget.magnitude;
        sensor.AddObservation(toTarget.normalized); 

        float maxDist = areaBounds.magnitude;
        sensor.AddObservation(distance / maxDist); 
        sensor.AddObservation(transform.localPosition.y / maxHeight); 
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount++;
        var ca = actions.ContinuousActions;

        float thrust = Mathf.Clamp(ca[0], -1f, 1f);
        float pitch = Mathf.Clamp(ca[1], -1f, 1f);
        float yaw = Mathf.Clamp(ca[2], -1f, 1f);
        float roll = Mathf.Clamp(ca[3], -1f, 1f);

        float baseThrust = gravityCompensation;
        float extraThrust = thrust * maxThrust * 0.05f;
        rb.AddForce(Vector3.up * (baseThrust + extraThrust), ForceMode.Force);

        rb.AddForce(transform.forward * -pitch * maxThrust * 0.05f, ForceMode.Force);
        rb.AddForce(transform.right * roll * maxThrust * 0.05f, ForceMode.Force);

        Vector3 torque = new Vector3(pitch, yaw, roll) * maxTorque;
        rb.AddRelativeTorque(torque, ForceMode.Force);

        if (stabilizationHelp > 0f)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            Quaternion correction = targetRotation * Quaternion.Inverse(transform.rotation);
            correction.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            rb.AddTorque(axis * angle * stabilizationHelp * 2f, ForceMode.Force);
        }

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        float currentDistance = Vector3.Distance(transform.localPosition, deliveryTarget.localPosition);
        

        float distanceDelta = previousDistance - currentDistance;
        AddReward(distanceDelta * distanceRewardScale);

        float uprightness = Vector3.Dot(transform.up, Vector3.up);
        if (uprightness > 0.8f)
            AddReward(stabilityRewardScale);
        else if (uprightness < 0.3f)
            AddReward(-stabilityRewardScale * 2f);

        AddReward(aliveReward);

        float maxDist = areaBounds.magnitude;
        float normalizedDist = currentDistance / maxDist;
        if (normalizedDist < 0.05f) AddReward(0.02f);
        else if (normalizedDist < 0.1f) AddReward(0.01f);
        else if (normalizedDist < 0.2f) AddReward(0.005f);
        else if (normalizedDist < 0.3f) AddReward(0.002f);

        AddReward(timePenalty);

        if (currentDistance < deliveryRadius && stepCount > 50)
        {
            float timeBonus = Mathf.Max(0f, 1f - (float)stepCount / MaxStep) * 5f;
            AddReward(deliveryReward + timeBonus);
            successCount++;
            Debug.Log($"[DroneAgent] Kézbesítve! Steps: {stepCount}, Level: {difficultyLevel}, " +
                      $"Sikerek: {successCount}/{episodeCount}");
            EndEpisode();
            return;
        }

        if (IsOutOfBounds())
        {
            AddReward(collisionPenalty);
            EndEpisode();
            return;
        }

        previousDistance = currentDistance;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = 0f;
        if (Input.GetKey(KeyCode.Space)) ca[0] = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) ca[0] = -1f;
        ca[1] = 0f;
        if (Input.GetKey(KeyCode.W)) ca[1] = -1f;
        if (Input.GetKey(KeyCode.S)) ca[1] = 1f;
        ca[2] = 0f;
        if (Input.GetKey(KeyCode.E)) ca[2] = 1f;
        if (Input.GetKey(KeyCode.Q)) ca[2] = -1f;
        ca[3] = 0f;
        if (Input.GetKey(KeyCode.A)) ca[3] = -1f;
        if (Input.GetKey(KeyCode.D)) ca[3] = 1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") ||
            collision.gameObject.CompareTag("Building"))
        {
            AddReward(collisionPenalty);
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            AddReward(collisionPenalty * 0.5f);
            EndEpisode();
        }
    }

    private bool IsOutOfBounds()
    {
        Vector3 pos = transform.localPosition;
        return Mathf.Abs(pos.x) > areaBounds.x ||
               pos.y > maxHeight || pos.y < 0f ||
               Mathf.Abs(pos.z) > areaBounds.z;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.parent ? transform.parent.position : Vector3.zero,
                            areaBounds * 2f);
        if (deliveryTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(deliveryTarget.position, deliveryRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, deliveryTarget.position);
        }
    }
}