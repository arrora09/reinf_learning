using UnityEngine;
using System.Collections.Generic;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Environment Area")]
    public Vector3 areaBounds = new Vector3(50f, 50f, 50f);

    [Header("Delivery Target")]
    public Transform deliveryTarget;
    public float targetMinHeight = 1f;
    public float targetMaxHeight = 15f;

    [Header("Moving Obstacles")]
    public GameObject movingObstaclePrefab;
    public int minObstacles = 1;
    public int maxObstacles = 4;
    public Transform obstacleParent;

    [Header("Static Buildings")]
    public GameObject[] buildingPrefabs;
    public int minBuildings = 4;
    public int maxBuildings = 10;
    public Transform buildingParent;

    [Header("Wind")]
    public bool enableWind = true;
    public float maxWindForce = .5f;
    public float windChangeInterval = 10f;

    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<GameObject> activeBuildings = new List<GameObject>();
    private Vector3 currentWindDirection;
    private float currentWindStrength;
    private float windTimer;

    private void Start()
    {
        Debug.Log("[EnvironmentManager]:Környezet inicializálása...");

        if (movingObstaclePrefab == null)
            Debug.LogWarning("[EnvironmentManager]: movingObstaclePrefab NINCS BEÁLLÍTVA!");

        if (deliveryTarget == null)
            Debug.LogWarning("[EnvironmentManager]: deliveryTarget NINCS BEÁLLÍTVA!");

        if (obstacleParent == null)
            Debug.LogWarning("[EnvironmentManager]: obstacleParent NINCS BEÁLLÍTVA!");

        ResetEnvironment();
    }

    public Vector3 GetWindForce()
    {
        return currentWindDirection * currentWindStrength;
    }

    public void ResetEnvironment()
    {
        RespawnObstacles();
        RespawnBuildings();
        RandomizeWind();
    }

    private void Update()
    {
        if (enableWind)
        {
            windTimer -= Time.deltaTime;
            if (windTimer <= 0f)
            {
                RandomizeWind();
            }
        }
    }

    private void PlaceTarget()
    {
        if (deliveryTarget == null) return;

        deliveryTarget.localPosition = new Vector3(
            Random.Range(-areaBounds.x * 0.4f, areaBounds.x * 0.4f),
            Random.Range(targetMinHeight, targetMaxHeight),
            Random.Range(-areaBounds.z * 0.4f, areaBounds.z * 0.4f)
        );
    }

    private void RespawnObstacles()
    {
        foreach (var obs in activeObstacles)
        {
            if (obs != null) Destroy(obs);
        }
        activeObstacles.Clear();

        if (movingObstaclePrefab == null)
        {
            Debug.LogError("[EnvironmentManager] RespawnObstacles: movingObstaclePrefab NULL!");
            return;
        }

        if (obstacleParent == null)
        {
            Debug.LogWarning("[EnvironmentManager] obstacleParent NULL.");
        }

        int count = Random.Range(minObstacles, maxObstacles + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(-areaBounds.x * 0.4f, areaBounds.x * 0.4f),
                Random.Range(5f, areaBounds.y * 0.8f),
                Random.Range(-areaBounds.z * 0.4f, areaBounds.z * 0.4f)
            );

            GameObject obs = Instantiate(movingObstaclePrefab, obstacleParent != null ? obstacleParent : transform);
            obs.transform.localPosition = spawnPos;
            obs.tag = "Obstacle";
            obs.SetActive(true);

            MovingObstacle mover = obs.GetComponent<MovingObstacle>();
            if (mover != null)
            {
                mover.Initialize(areaBounds);
            }
            else
            {
                Debug.LogWarning($"[EnvironmentManager] A prefab-on NINCS MovingObstacle script!");
            }

            activeObstacles.Add(obs);
        }

    }

    private void RespawnBuildings()
    {
        foreach (var building in activeBuildings)
        {
            if (building != null) Destroy(building);
        }
        activeBuildings.Clear();

        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        int count = Random.Range(minBuildings, maxBuildings + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(-areaBounds.x * 0.35f, areaBounds.x * 0.35f),
                0f,
                Random.Range(-areaBounds.z * 0.35f, areaBounds.z * 0.35f)
            );

            int prefabIdx = Random.Range(0, buildingPrefabs.Length);
            GameObject building = Instantiate(buildingPrefabs[prefabIdx], buildingParent ?? transform);
            building.transform.localPosition = spawnPos;

            float scaleX = Random.Range(3f, 8f);
            float scaleY = Random.Range(5f, 25f);
            float scaleZ = Random.Range(3f, 8f);
            building.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            building.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            building.tag = "Building";
            activeBuildings.Add(building);
        }
    }

    private void RandomizeWind()
    {
        currentWindDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.2f, 0.2f),
            Random.Range(-1f, 1f)
        ).normalized;

        currentWindStrength = Random.Range(0f, maxWindForce);
        windTimer = windChangeInterval + Random.Range(-1f, 1f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawWireCube(transform.position, areaBounds * 2f);

        if (Application.isPlaying && enableWind)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 30f,
                          currentWindDirection * currentWindStrength * 3f);
        }
    }
}