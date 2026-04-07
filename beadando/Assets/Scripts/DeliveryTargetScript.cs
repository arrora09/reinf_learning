using UnityEngine;

public class DeliveryTargetScript : MonoBehaviour
{
    [Header("Visual")]
    public float rotationSpeed = 30f;
    public float pulseSpeed = 2f;
    public float pulseMinScale = 0.8f;
    public float pulseMaxScale = 1.2f;

    [Header("Optional Movement")]
    public bool isMoving = false;
    public float moveSpeed = 1f;
    public float moveRadius = 3f;

    private Vector3 basePosition;
    private float moveAngle;
    private Light spotLight;

    private void Start()
    {
        basePosition = transform.localPosition;
        spotLight = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale,
                                  (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        transform.localScale = Vector3.one * scale;

        if (isMoving)
        {
            moveAngle += moveSpeed * Time.deltaTime;
            Vector3 offset = new Vector3(
                Mathf.Cos(moveAngle) * moveRadius,
                Mathf.Sin(moveAngle * 0.7f) * moveRadius * 0.3f,
                Mathf.Sin(moveAngle) * moveRadius
            );
            transform.localPosition = basePosition + offset;
        }

        if (spotLight != null)
        {
            spotLight.intensity = Mathf.Lerp(1f, 3f,
                (Mathf.Sin(Time.time * pulseSpeed * 2f) + 1f) * 0.5f);
        }
    }

    public void UpdateBasePosition()
    {
        basePosition = transform.localPosition;
    }
}
