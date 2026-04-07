using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    public enum MovementPattern
    {
        Linear,
        Circular,
        Sinusoidal,
        Random
    }

    [Header("Movement")]
    public float speed = .2f;
    public float rotationSpeed = .2f;
    public MovementPattern pattern = MovementPattern.Linear;

    private Vector3 areaBounds;
    private Vector3 moveDirection;
    private Vector3 centerPoint;
    private float circleAngle;
    private float circleRadius;
    private float sinTime;
    private float directionChangeTimer;

    public void Initialize(Vector3 bounds)
    {
        areaBounds = bounds;
        speed = UnityEngine.Random.Range(0.1f, 0.5f);
        centerPoint = transform.localPosition;

        pattern = (MovementPattern)UnityEngine.Random.Range(0, 4);

        moveDirection = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-0.3f, 0.3f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;

        circleRadius = UnityEngine.Random.Range(5f, 15f);
        circleAngle = UnityEngine.Random.Range(0f, 360f);
        directionChangeTimer = UnityEngine.Random.Range(2f, 5f);
    }

    private void Update()
    {
        switch (pattern)
        {
            case MovementPattern.Linear:
                MoveLinear();
                break;
            case MovementPattern.Circular:
                MoveCircular();
                break;
            case MovementPattern.Sinusoidal:
                MoveSinusoidal();
                break;
            case MovementPattern.Random:
                MoveRandom();
                break;
        }

        ClampPosition();
    }

    private void MoveLinear()
    {
        transform.localPosition += moveDirection * speed * Time.deltaTime;

        Vector3 pos = transform.localPosition;
        if (Mathf.Abs(pos.x) > areaBounds.x * 0.4f)
            moveDirection.x *= -1f;
        if (pos.y > areaBounds.y * 0.8f || pos.y < 3f)
            moveDirection.y *= -1f;
        if (Mathf.Abs(pos.z) > areaBounds.z * 0.4f)
            moveDirection.z *= -1f;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void MoveCircular()
    {
        circleAngle += speed * 10f * Time.deltaTime;
        float rad = circleAngle * Mathf.Deg2Rad;

        Vector3 newPos = centerPoint + new Vector3(
            Mathf.Cos(rad) * circleRadius,
            Mathf.Sin(rad * 0.5f) * 3f,
            Mathf.Sin(rad) * circleRadius
        );

        transform.localPosition = newPos;

        Vector3 tangent = new Vector3(
            -Mathf.Sin(rad),
            Mathf.Cos(rad * 0.5f) * 0.5f,
            Mathf.Cos(rad)
        ).normalized;

        if (tangent != Vector3.zero)
        {
            transform.localRotation = Quaternion.LookRotation(tangent);
        }
    }

    private void MoveSinusoidal()
    {
        sinTime += Time.deltaTime;
        Vector3 offset = new Vector3(
            Mathf.Sin(sinTime * speed * 0.5f) * 10f,
            Mathf.Cos(sinTime * speed * 0.3f) * 3f,
            sinTime * speed
        );

        transform.localPosition = centerPoint + offset;

        if (Mathf.Abs(transform.localPosition.z - centerPoint.z) > areaBounds.z * 0.4f)
        {
            sinTime = 0f;
            speed *= -1f;
        }
    }

    private void MoveRandom()
    {
        directionChangeTimer -= Time.deltaTime;
        if (directionChangeTimer <= 0f)
        {
            moveDirection = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-0.3f, 0.3f),
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized;

            speed = UnityEngine.Random.Range(0.1f, 0.5f);
            directionChangeTimer = UnityEngine.Random.Range(1f, 4f);
        }

        transform.localPosition += moveDirection * speed * Time.deltaTime;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Clamp(pos.x, -areaBounds.x * 0.45f, areaBounds.x * 0.45f);
        pos.y = Mathf.Clamp(pos.y, 2f, areaBounds.y * 0.9f);
        pos.z = Mathf.Clamp(pos.z, -areaBounds.z * 0.45f, areaBounds.z * 0.45f);
        transform.localPosition = pos;
    }
}
