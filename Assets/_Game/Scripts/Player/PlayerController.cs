using System.Collections.Generic;
using UnityEngine;

public enum Direct { None, Forward, Back, Left, Right }

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
    public LayerMask pushLayer;

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
    private bool hasReachedDestination = false;
    private ParticleSystem currentParticleSystem;

    public HashSet<Vector3> locationPassed = new HashSet<Vector3>();
    public HashSet<Vector3> linePassed = new HashSet<Vector3>();
    public HashSet<GameObject> lineParents = new HashSet<GameObject>();
    public HashSet<GameObject> pushParents = new HashSet<GameObject>();

    void Start()
    {
        OnInit();
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

    public void OnInit()
    {
        if (brickStack != null)
        {
            Destroy(brickStack.gameObject);
        }

        brickStack = new GameObject("BrickStack").transform;
        brickStack.SetParent(transform);
        brickStack.localPosition = Vector3.zero;

        brickCount = 0;
        isMoving = false;
        particlesActivated = false;
        hasReachedDestination = false;

        //Reset animation
        animator.ResetTrigger("win");
        animator.SetInteger("renwu", 0);
        animator.Rebind();

        player.transform.rotation = Quaternion.identity;
        currentParticleSystem = null;

        locationPassed.Clear();
        linePassed.Clear();
        lineParents.Clear();
        locationPassed.Add(transform.position);

        UpdatePlayerPosition(0f);
    }

    public void SetParticleSystem(ParticleSystem particleSystem)
    {
        currentParticleSystem = particleSystem;
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
                UIManager.Instance.retryButton.gameObject.SetActive(true);
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
        targetPosition = transform.position + moveDirection;

        if (CheckObjectPosition(targetPosition, brickLayer | unBrickLayer | lineLayer | winPosLayer | destinationLayer | pushLayer))
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

            if (!hasReachedDestination)
            {
                if (CheckObjectPosition(transform.position, destinationLayer))
                {
                    animator.SetTrigger("win");
                    HandleDestinationPoint();
                    hasReachedDestination = true;
                    return;
                }
            }

            HandleBrickAndLineInteractions();

            if (brickCount < 1 && CheckObjectPosition(transform.position, lineLayer))
            {
                if (moveDirection == Vector3.back || moveDirection == Vector3.left)
                {
                    SetTargetPosition();
                    return;
                }
                else
                {
                    isMoving = false;
                    animator.SetInteger("renwu", 0);
                    return;
                }
            }

            Vector3 nextPosition = targetPosition + moveDirection;

            if (CheckObjectPosition(nextPosition, brickLayer | unBrickLayer | lineLayer | winPosLayer | destinationLayer | pushLayer))
            {
                isMoving = true;
                MovingContinuously();
                HandlePushObject();
            }
            else
            {
                HandleStopAtBrick();
                isMoving = false;
                animator.SetInteger("renwu", 0);
            }
        }
    }

    private void HandlePushObject()
    {
        if (Physics.Raycast(transform.position + moveDirection, Vector3.down, 5f, pushLayer))
        {
            Vector3 newDirection = FindNewDirectionAroundPush();
            if (newDirection != Vector3.zero)
            {
                moveDirection = newDirection;
                SetTargetPosition();
                isMoving = true;
                animator.SetInteger("renwu", 1);
            }
        }
    }

    private Vector3 FindNewDirectionAroundPush()
    {
        Vector3[] possibleDirections = new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        foreach (Vector3 direction in possibleDirections)
        {
            Vector3 checkPosition = transform.position + direction;

            if (CheckObjectPosition(checkPosition, brickLayer))
            {
                return direction;
            }
        }

        return Vector3.zero;
    }

    private void HandleStopAtBrick()
    {
        HandleBrickAndLineInteractions();
        HideBrick(transform.position);
    }

    private void HandleDestinationPoint()
    {
        if (particlesActivated && treasureChest == null) return;

        particlesActivated = true;
        ClearBrick();
        ActivateParticleEffects();
        player.transform.rotation = Quaternion.Euler(0, -145, 0);
        UIManager.Instance.ShowCompletePanel();
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
            targetPosition = targetPosition + moveDirection;
            animator.SetInteger("renwu", 1);
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
        newBrick.transform.localPosition = new Vector3(0, yOffset - brickHeight, 0);

        UpdatePlayerPosition(yOffset);
    }

    void RemoveBrick()
    {
        if (brickStack.childCount > 1)
        {
            Destroy(brickStack.GetChild(0).gameObject);
            RestoreLineObjects();
            UpdateBrickStackPosition();
            UpdatePlayerPosition(((brickStack.childCount - 1) * brickHeight) - brickHeight);
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
            brick.localPosition = new Vector3(0, yOffset - brickHeight, 0);
        }
    }

    void ActivateParticleEffects()
    {
        if (currentParticleSystem == null) return;
        currentParticleSystem.Play();
    }

    void UpdatePlayerPosition(float yOffset)
    {
        player.transform.position = new Vector3(transform.position.x, brickStack.transform.position.y + yOffset, transform.position.z);
    }
}