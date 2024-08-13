using UnityEngine;

public enum Direct
{
    None,
    Forward,
    Back,
    Left,
    Right
}

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public LayerMask brickLayer;
    public Direct direct = Direct.None;

    private Vector2 startTouchPosition, endTouchPosition;
    private Vector3 moveDirection;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float checkDistance = 1.0f;

    void Update()
    {
        DetectSwipe();
        if (isMoving)
        {
            MovePlayer();
        }
    }

    void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            endTouchPosition = Input.mousePosition;
            DetermineSwipeDirection();
            SetTargetPosition();
        }
    }

    void DetermineSwipeDirection()
    {
        Vector2 swipe = endTouchPosition - startTouchPosition;
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        {
            direct = swipe.x > 0 ? Direct.Right : Direct.Left;
        }
        else
        {
            direct = swipe.y > 0 ? Direct.Forward : Direct.Back;
        }
    }

    void SetTargetPosition()
    {
        switch (direct)
        {
            case Direct.Forward:
                moveDirection = Vector3.forward;
                break;
            case Direct.Back:
                moveDirection = Vector3.back;
                break;
            case Direct.Left:
                moveDirection = Vector3.left;
                break;
            case Direct.Right:
                moveDirection = Vector3.right;
                break;
            default:
                return;
        }

        if (Physics.Raycast(transform.position, moveDirection, checkDistance, brickLayer))
        {
            isMoving = true;
            targetPosition = transform.position + moveDirection;
        }
        else
        {
            isMoving = false;
        }
    }

    void MovePlayer()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                if (Physics.Raycast(transform.position, moveDirection, checkDistance, brickLayer))
                {
                    targetPosition += moveDirection;
                }
                else
                {
                    isMoving = false;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, moveDirection);
    }
}