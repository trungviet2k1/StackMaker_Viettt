using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Vector3 offset = new Vector3();

    private Transform currentTarget;

    void Start()
    {
        currentTarget = player;
    }

    void LateUpdate()
    {
        if (currentTarget != null)
        {
            transform.position = currentTarget.position + offset;
            transform.LookAt(currentTarget);
        }
    }
}