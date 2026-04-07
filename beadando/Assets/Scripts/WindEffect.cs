using UnityEngine;

public class WindEffect : MonoBehaviour
{
    [Header("References")]
    public EnvironmentManager envManager;

    [Header("Wind Settings")]
    public float windMultiplier = 1.0f;
    public bool showParticles = true;

    [Header("Turbulence")]
    public float turbulenceStrength = 0.5f;
    public float turbulenceFrequency = 1.0f;

    private Rigidbody rb;
    private ParticleSystem windParticles;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (showParticles)
        {
            windParticles = GetComponentInChildren<ParticleSystem>();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || envManager == null) return;

        if (!envManager.enableWind) return;

        Vector3 windForce = envManager.GetWindForce() * windMultiplier;

        float time = Time.time * turbulenceFrequency;
        Vector3 turbulence = new Vector3(
            Mathf.PerlinNoise(time, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, time) - 0.5f,
            Mathf.PerlinNoise(time, time) - 0.5f
        ) * turbulenceStrength;

        rb.AddForce(windForce + turbulence, ForceMode.Force);

        if (windParticles != null)
        {
            var velocityModule = windParticles.velocityOverLifetime;
            velocityModule.x = windForce.x;
            velocityModule.y = windForce.y;
            velocityModule.z = windForce.z;
        }
    }
}
