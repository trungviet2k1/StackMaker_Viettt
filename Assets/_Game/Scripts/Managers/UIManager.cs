using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Complete Panel")]
    public GameObject completePanel;

    [Header("Image UI")]
    public GameObject congratulationsBg;
    public GameObject coinBg;
    public GameObject getCoinBg;

    [Header("Button UI")]
    public Button watchVideoButton;
    public Button nextButton;
    public Button retryButton;

    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public float scaleMultiplier = 1.2f;
    public float delayBetweenElements = 0.3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        OnInit();
    }

    public void ShowCompletePanel()
    {
        completePanel.SetActive(true);
        StartCoroutine(AnimatePanelElements());
    }

    public void HideCompletePanel()
    {
        completePanel.SetActive(false);
        ResetElementScale();
        SetElementUIActive(false);
    }

    public void OnNextButtonClicked()
    {
        LevelManager.Instance.GetCoin();
        StartCoroutine(HandleNextButtonClick());
    }

    public void OnRetryButtonClicked()
    {
        LevelManager.Instance.ReloadCurrentLevel();
    }

    private IEnumerator HandleNextButtonClick()
    {
        if (watchVideoButton != null)
        {
            watchVideoButton.interactable = false;
        }

        yield return new WaitForSeconds(3f);
        HideCompletePanel();
        LevelManager.Instance.LoadNextLevel();
    }

    private IEnumerator AnimatePanelElements()
    {
        GameObject[] elements = { congratulationsBg, coinBg, getCoinBg, watchVideoButton.gameObject, nextButton.gameObject };

        foreach (var element in elements)
        {
            yield return StartCoroutine(AnimateElement(element));
        }
    }

    private IEnumerator AnimateElement(GameObject element)
    {
        if (element == null) yield break;

        element.SetActive(true);
        Vector3 originalScale = element.transform.localScale;
        Vector3 targetScale = originalScale * scaleMultiplier;
        float halfDuration = animationDuration / 2;

        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            element.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            element.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(delayBetweenElements);
    }

    private void ResetElementScale()
    {
        foreach (var element in new GameObject[] { congratulationsBg, coinBg, getCoinBg, watchVideoButton.gameObject, nextButton.gameObject })
        {
            if (element != null)
            {
                element.transform.localScale = Vector3.one;
            }
        }
    }

    private void SetElementUIActive(bool isActive)
    {
        if (congratulationsBg != null) congratulationsBg.SetActive(isActive);
        if (coinBg != null) coinBg.SetActive(isActive);
        if (getCoinBg != null) getCoinBg.SetActive(isActive);
        if (watchVideoButton != null) watchVideoButton.gameObject.SetActive(isActive);
        if (nextButton != null) nextButton.gameObject.SetActive(isActive);
        if (retryButton != null) retryButton.gameObject.SetActive(isActive);
    }

    public void OnInit()
    {
        HideCompletePanel();
        ResetElementScale();

        if (watchVideoButton != null)
        {
            watchVideoButton.interactable = true;
        }

        if (retryButton != null)
        {
            watchVideoButton.gameObject.SetActive(false);
        }
    }
}