using System.Collections.Generic;
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
    [Header("Player Settings")]
    public GameObject player;
    public float moveSpeed = 5f;

    [Header("Components")]
    public Animator animator;

    [Header("Layer Mask")]
    public LayerMask brickLayer;
    public LayerMask unBrickLayer;
    public LayerMask lineLayer;
    public LayerMask winPosLayer;
    public LayerMask destinationLayer;

    [Header("Environmental Objects")]
    public Transform treasureChest;
    public GameObject brick;
    public int brickCount = 0;
    public float brickHeight = 0.3f;

    [Header("Particle Systems")]
    public ParticleSystem yanchen1;
    public ParticleSystem yanchen2;

    private Vector2 startTouchPosition;
    private Vector3 moveDirection;
    private Vector3 targetPosition;
    private Vector3 brickRotation = new Vector3(-90, 0, 180);
    private Transform brickStack;
    private bool isMoving = false;
    private bool particlesActivated = false;

    public HashSet<Vector3> locationPassed = new HashSet<Vector3>();
    public HashSet<Vector3> linePassed = new HashSet<Vector3>();
    public HashSet<GameObject> lineParents = new HashSet<GameObject>();

    void Start()
    {
        OnInit();
        locationPassed.Add(transform.position);
    }

    void Update()
    {
        if (!isMoving)
        {
            DetectSwipe();
        }
        else
        {
            MovePlayer();
        }
    }

    void OnInit()
    {
        brickStack = new GameObject("BrickStack").transform;
        brickStack.SetParent(transform);
        brickStack.localPosition = Vector3.zero;
    }

    void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Vector2 swipe = (Vector2)Input.mousePosition - startTouchPosition;
            moveDirection = GetMoveDirection(swipe);

            if (moveDirection != Vector3.zero)
            {
                SetTargetPosition();
            }
        }
    }

    Vector3 GetMoveDirection(Vector2 swipe)
    {
        return Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y)
            ? (swipe.x > 0 ? Vector3.right : Vector3.left)
            : (swipe.y > 0 ? Vector3.forward : Vector3.back);
    }

    void SetTargetPosition()
    {
        if (CheckObjectPosition(transform.position, brickLayer))
        {
            HideBrick(transform.position);
        }

        targetPosition = transform.position + moveDirection;

        if (CheckObjectPosition(targetPosition, brickLayer | unBrickLayer | lineLayer | winPosLayer | destinationLayer))
        {
            isMoving = true;
        }
    }

    void MovePlayer()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            transform.position = targetPosition;

            if (CheckObjectPosition(transform.position, destinationLayer))
            {
                HandleDestinationPoint();
                animator.SetTrigger("win");
                return;
            }

            Vector3 nextPosition = targetPosition + moveDirection;
            HandleBrickAndLineInteractions();

            if (CheckObjectPosition(nextPosition, brickLayer | unBrickLayer | lineLayer | winPosLayer | destinationLayer))
            {
                MovingContinuously();
                animator.SetInteger("renwu", 1);
            }
            else
            {
                isMoving = false;
                animator.SetInteger("renwu", 0);
            }
        }
    }

    private void HandleDestinationPoint()
    {
        if (particlesActivated) return;

        particlesActivated = true;
        ClearBrick();
        ActivateParticleEffects();

        if (treasureChest != null)
        {
            /*Vector3 directionToTarget = (treasureChest.position - player.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            float targetYRotation = lookRotation.eulerAngles.y;*/
            player.transform.rotation = Quaternion.Euler(0, -145, 0);
        }
    }

    private void HandleBrickAndLineInteractions()
    {
        bool locationAlreadyExists = false;
        foreach (var position in locationPassed)
        {
            if (Vector3.Distance(targetPosition, position) < 0.1f)
            {
                locationAlreadyExists = true;
                break;
            }
        }

        bool lineAlreadyExists = false;
        foreach (var position in linePassed)
        {
            if (Vector3.Distance(targetPosition, position) < 0.1f)
            {
                lineAlreadyExists = true;
                break;
            }
        }

        if (CheckObjectPosition(transform.position, brickLayer) && !locationAlreadyExists)
        {
            HideBrick(transform.position - moveDirection);
            AddBrick();
            locationPassed.Add(transform.position);
            ++brickCount;
        }

        if (CheckObjectPosition(transform.position, lineLayer) && !lineAlreadyExists)
        {
            RemoveBrick();
            linePassed.Add(transform.position);
            --brickCount;
        }
    }

    private void MovingContinuously()
    {
        if (brickStack.childCount > 0)
        {
            isMoving = true;
            targetPosition = targetPosition + moveDirection;
            HideBrick(transform.position);
        }
    }

    bool CheckObjectPosition(Vector3 position, LayerMask layer)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, lineLayer))
        {
            GameObject lineObject = hit.collider.gameObject;
            if (!lineParents.Contains(lineObject))
            {
                lineParents.Add(lineObject);
                return true;
            }
        }
        return Physics.Raycast(position, Vector3.down, 5f, layer);

    }

    void HideBrick(Vector3 position)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, brickLayer))
        {
            hit.collider.gameObject.SetActive(false);
        }
    }

    void AddBrick()
    {
        GameObject newBrick = Instantiate(brick, brickStack.transform.position, Quaternion.Euler(brickRotation));
        newBrick.transform.SetParent(brickStack, true);

        float yOffset = (brickStack.childCount - 1) * brickHeight;
        newBrick.transform.localPosition = new Vector3(0, yOffset, 0);

        UpdatePlayerPosition(yOffset + brickHeight);
    }

    void RemoveBrick()
    {
        if (brickStack.childCount > 0)
        {
            Destroy(brickStack.GetChild(0).gameObject);
            RestoreLineObjects();
            UpdateBrickStackPosition();
            UpdatePlayerPosition((brickStack.childCount - 1) * brickHeight);
        }
    }

    void ClearBrick()
    {
        foreach (Transform brick in brickStack)
        {
            Destroy(brick.gameObject);
        }

        brickCount = 0;
        UpdatePlayerPosition(0f);
    }

    private void RestoreLineObjects()
    {
        foreach (var parent in lineParents)
        {
            foreach (Transform child in parent.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        lineParents.Clear();
    }

    void UpdateBrickStackPosition()
    {
        for (int i = 0; i < brickStack.childCount; i++)
        {
            Transform brick = brickStack.GetChild(i);
            float yOffset = (i - 1) * brickHeight;
            brick.localPosition = new Vector3(0, yOffset, 0);
        }
    }

    void ActivateParticleEffects()
    {
        yanchen1?.Play();
        yanchen2?.Play();
    }

    void UpdatePlayerPosition(float yOffset)
    {
        player.transform.position = new Vector3(transform.position.x, brickStack.transform.position.y + yOffset, transform.position.z);
    }
}