using UnityEngine;

public class PropellerSpin : MonoBehaviour
{
    public Transform[] propellers;
    public float spinSpeed = 3000f;

    void Update()
    {
        foreach (var prop in propellers)
        {
            if (prop != null)
                prop.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
        }
    }
}