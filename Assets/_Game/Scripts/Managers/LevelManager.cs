using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Levels Settings")]
    public GameObject[] ListLevels;

    [Header("Player")]
    public Transform player;

    [Header("Texts UI")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinText;

    private GameObject currentLevel;
    private int currentLevelIndex = 0;
    private int coinValue = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        LoadLevel(0);
    }

    public void LoadLevel(int levelIndex)
    {
        if (currentLevel != null)
        {
            Destroy(currentLevel);
        }

        GameObject mapObject = GameObject.Find("Map");
        if (mapObject == null) return;

        currentLevel = Instantiate(ListLevels[levelIndex], mapObject.transform);
        Vector3 startPosition = FindFirstBrickPosition();

        if (startPosition == Vector3.zero) return;
        player.position = new Vector3(startPosition.x, 3f, startPosition.z);
        levelText.text = "Level " + (levelIndex + 1);

        if (player.TryGetComponent<PlayerController>(out var playerController))
        {
            playerController.OnInit();
        }

        UIManager.Instance.OnInit();
    }

    private Vector3 FindFirstBrickPosition()
    {
        Transform brickParent = currentLevel.transform.Find("Brick");
        if (brickParent == null) return Vector3.zero;

        foreach (Transform child in brickParent)
        {
            if (child.CompareTag("Brick"))
            {
                return child.position;
            }
        }

        return Vector3.zero;
    }

    public void GetCoin()
    {
        coinValue += 50;
        UpdateCoinText();
    }

    private void UpdateCoinText()
    {
        if (coinText != null)
        {
            coinText.text = coinValue.ToString();
        }
    }

    public void LoadNextLevel()
    {
        currentLevelIndex = (currentLevelIndex + 1) % ListLevels.Length;
        LoadLevel(currentLevelIndex);
    }

    public void ReloadCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }
}