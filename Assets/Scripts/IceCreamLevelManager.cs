using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IceCreamLevelManager : MonoBehaviour
{
    [Header("Main references")]
    [SerializeField] private IceCreamGameManager gameManager;
    [SerializeField] private ScoopSpawner scoopSpawner;
    [SerializeField] private LivesManager livesManager;

    [Header("Level progression")]
    [Min(1)]
    [SerializeField] private int initialScoopCount = 4;

    [Min(1)]
    [SerializeField] private int levelsPerExtraScoop = 3;

    [Min(1)]
    [SerializeField] private int maximumScoopCount = 8;

    [Header("Level complete transition")]
    [Min(0f)]
    [SerializeField] private float relaxDuration = 2.5f;

    [Header("Game Over")]
    [Min(0f)]
    [SerializeField] private float retryDelay = 1.5f;

    [Header("Countdown")]
    [Min(1)]
    [SerializeField] private int countdownStartNumber = 3;

    [Min(0.1f)]
    [SerializeField] private float countdownStepDuration = 1f;

    [Min(0f)]
    [SerializeField] private float goTextDuration = 0.5f;

    [Header("Sequence generation")]
    [SerializeField]
    private bool avoidAdjacentRepeatedColors = true;

    [SerializeField]
    private bool forceDifferentOrder = true;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text countdownText;

    private readonly List<ScoopColorType> previousOrder =
        new List<ScoopColorType>();

    // Esta es la combinación del nivel actual.
    private readonly List<ScoopColorType> currentLevelOrder =
        new List<ScoopColorType>();

    private ScoopColorType[] availableColors;

    private int currentLevel = 1;

    private bool isTransitioning;

    private Coroutine levelFlowCoroutine;

    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        availableColors =
            (ScoopColorType[])Enum.GetValues(
                typeof(ScoopColorType)
            );
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OrderCompleted +=
                HandleOrderCompleted;

            gameManager.WrongScoopCollected +=
                HandleWrongScoop;
        }

        if (livesManager != null)
        {
            livesManager.LivesDepleted +=
                HandleLivesDepleted;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OrderCompleted -=
                HandleOrderCompleted;

            gameManager.WrongScoopCollected -=
                HandleWrongScoop;
        }

        if (livesManager != null)
        {
            livesManager.LivesDepleted -=
                HandleLivesDepleted;
        }

        if (levelFlowCoroutine != null)
        {
            StopCoroutine(levelFlowCoroutine);
            levelFlowCoroutine = null;
        }
    }

    private IEnumerator Start()
    {
        if (gameManager == null ||
            scoopSpawner == null ||
            livesManager == null)
        {
            Debug.LogError(
                "IceCreamLevelManager tiene referencias faltantes.",
                this
            );

            yield break;
        }

        scoopSpawner.StopSpawning();
        scoopSpawner.ClearLooseScoops();

        livesManager.ResetLives();

        isTransitioning = true;

        GenerateNewLevelOrder();

        yield return StartCoroutine(
            PrepareAndStartCurrentLevel()
        );

        isTransitioning = false;
    }

    // --------------------------------------------------
    // ERROR
    // --------------------------------------------------

    private void HandleWrongScoop()
    {
        if (isTransitioning)
            return;

        livesManager.LoseLife();
    }

    // --------------------------------------------------
    // SIN VIDAS
    // --------------------------------------------------

    private void HandleLivesDepleted()
    {
        if (isTransitioning)
            return;

        if (levelFlowCoroutine != null)
            return;

        levelFlowCoroutine =
            StartCoroutine(RetryCurrentLevel());
    }

    private IEnumerator RetryCurrentLevel()
    {
        isTransitioning = true;

        gameManager.StopCollection();

        scoopSpawner.StopSpawning();
        scoopSpawner.ClearLooseScoops();

        SetCountdownText("¡Sin vidas!");

        yield return new WaitForSeconds(retryDelay);

        // Recuperamos todas las vidas.
        livesManager.ResetLives();

        // IMPORTANTE:
        // no incrementamos currentLevel.
        // no generamos otra secuencia.
        //
        // Por lo tanto se repite exactamente
        // el mismo nivel.

        yield return StartCoroutine(
            PrepareAndStartCurrentLevel()
        );

        isTransitioning = false;

        levelFlowCoroutine = null;
    }

    // --------------------------------------------------
    // NIVEL COMPLETADO
    // --------------------------------------------------

    private void HandleOrderCompleted()
    {
        if (isTransitioning)
            return;

        if (levelFlowCoroutine != null)
            return;

        levelFlowCoroutine =
            StartCoroutine(CompleteLevelTransition());
    }

    private IEnumerator CompleteLevelTransition()
    {
        isTransitioning = true;

        gameManager.StopCollection();

        scoopSpawner.StopSpawning();
        scoopSpawner.ClearLooseScoops();

        SetCountdownText("¡Helado completo!");

        // Más adelante:
        // puntuación
        // bonus
        // animaciones
        // etc.

        yield return new WaitForSeconds(relaxDuration);

        // Avanzamos de nivel.
        currentLevel++;

        // Al superar el nivel sí generamos
        // una nueva combinación.
        GenerateNewLevelOrder();

        // Recuperamos las vidas para
        // el siguiente nivel.
        livesManager.ResetLives();

        yield return StartCoroutine(
            PrepareAndStartCurrentLevel()
        );

        isTransitioning = false;

        levelFlowCoroutine = null;
    }

    // --------------------------------------------------
    // PREPARACIÓN DEL NIVEL
    // --------------------------------------------------

    private IEnumerator PrepareAndStartCurrentLevel()
    {
        UpdateLevelText();

        // Utilizamos currentLevelOrder,
        // que ya fue generada anteriormente.
        gameManager.PrepareOrder(currentLevelOrder);

        // 3...
        // 2...
        // 1...
        for (int number = countdownStartNumber;
             number >= 1;
             number--)
        {
            SetCountdownText(number.ToString());

            yield return new WaitForSeconds(
                countdownStepDuration
            );
        }

        SetCountdownText("¡YA!");

        gameManager.StartPreparedOrder();

        scoopSpawner.BeginSpawning();

        yield return new WaitForSeconds(
            goTextDuration
        );

        HideCountdownText();
    }

    // --------------------------------------------------
    // GENERACIÓN DEL PEDIDO
    // --------------------------------------------------

    private void GenerateNewLevelOrder()
    {
        int scoopCount =
            CalculateScoopCount(currentLevel);

        List<ScoopColorType> newOrder =
            GenerateDifferentOrder(scoopCount);

        currentLevelOrder.Clear();

        for (int i = 0; i < newOrder.Count; i++)
        {
            currentLevelOrder.Add(newOrder[i]);
        }

        StorePreviousOrder(newOrder);
    }

    private int CalculateScoopCount(int level)
    {
        int extraScoops =
            (level - 1) / levelsPerExtraScoop;

        return Mathf.Clamp(
            initialScoopCount + extraScoops,
            1,
            maximumScoopCount
        );
    }

    private List<ScoopColorType> GenerateDifferentOrder(
        int scoopCount)
    {
        const int maximumAttempts = 50;

        List<ScoopColorType> generatedOrder = null;

        for (int attempt = 0;
             attempt < maximumAttempts;
             attempt++)
        {
            generatedOrder =
                GenerateOrder(scoopCount);

            if (!forceDifferentOrder ||
                !OrdersAreEqual(
                    generatedOrder,
                    previousOrder))
            {
                return generatedOrder;
            }
        }

        return generatedOrder;
    }

    private List<ScoopColorType> GenerateOrder(
        int scoopCount)
    {
        List<ScoopColorType> newOrder =
            new List<ScoopColorType>();

        for (int i = 0; i < scoopCount; i++)
        {
            ScoopColorType selectedColor;

            int safetyCounter = 0;

            do
            {
                int randomIndex =
                    UnityEngine.Random.Range(
                        0,
                        availableColors.Length
                    );

                selectedColor =
                    availableColors[randomIndex];

                safetyCounter++;
            }
            while (
                avoidAdjacentRepeatedColors &&
                i > 0 &&
                selectedColor == newOrder[i - 1] &&
                safetyCounter < 20
            );

            newOrder.Add(selectedColor);
        }

        return newOrder;
    }

    private bool OrdersAreEqual(
        IReadOnlyList<ScoopColorType> first,
        IReadOnlyList<ScoopColorType> second)
    {
        if (first == null ||
            second == null ||
            first.Count != second.Count)
        {
            return false;
        }

        for (int i = 0; i < first.Count; i++)
        {
            if (first[i] != second[i])
            {
                return false;
            }
        }

        return true;
    }

    private void StorePreviousOrder(
        IReadOnlyList<ScoopColorType> newOrder)
    {
        previousOrder.Clear();

        for (int i = 0; i < newOrder.Count; i++)
        {
            previousOrder.Add(newOrder[i]);
        }
    }

    // --------------------------------------------------
    // UI
    // --------------------------------------------------

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text =
                $"Nivel {currentLevel}";
        }
    }

    private void SetCountdownText(string message)
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(true);

        countdownText.text = message;
    }

    private void HideCountdownText()
    {
        if (countdownText == null)
            return;

        countdownText.text = "";

        countdownText.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        initialScoopCount =
            Mathf.Max(1, initialScoopCount);

        levelsPerExtraScoop =
            Mathf.Max(1, levelsPerExtraScoop);

        maximumScoopCount =
            Mathf.Max(
                initialScoopCount,
                maximumScoopCount
            );
    }
}