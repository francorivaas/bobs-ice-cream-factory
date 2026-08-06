using System;
using System.Collections;
using UnityEngine;

public class ScoopSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FallingScoop scoopPrefab;
    [SerializeField] private IceCreamGameManager gameManager;

    [Header("Spawn area")]
    [SerializeField] private float minimumX = -7f;
    [SerializeField] private float maximumX = 3.5f;
    [SerializeField] private float spawnY = 6f;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.4f;
    [SerializeField] private float spawnInterval = 0.9f;

    [Header("Required color probability")]
    [Range(0f, 1f)]
    [SerializeField] private float requiredColorChance = 0.35f;

    private ScoopColorType[] availableColors;
    private Coroutine spawnCoroutine;

    public bool IsSpawning => spawnCoroutine != null;

    private void Awake()
    {
        availableColors =
            (ScoopColorType[])Enum.GetValues(
                typeof(ScoopColorType)
            );
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    public void BeginSpawning()
    {
        StopSpawning();

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine == null)
            return;

        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    /// <summary>
    /// Elimina solamente las bolas que están cayendo.
    /// No elimina las bolas ya apiladas sobre el cono.
    /// </summary>
    public void ClearLooseScoops()
    {
        FallingScoop[] scoops =
            FindObjectsByType<FallingScoop>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < scoops.Length; i++)
        {
            FallingScoop scoop = scoops[i];

            if (scoop != null && !scoop.IsResolved)
            {
                Destroy(scoop.gameObject);
            }
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (gameManager != null &&
                gameManager.IsCollectionEnabled &&
                !gameManager.IsComplete)
            {
                SpawnScoop();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnScoop()
    {
        if (scoopPrefab == null)
        {
            Debug.LogError(
                "ScoopSpawner no tiene asignado el Scoop Prefab.",
                this
            );

            StopSpawning();
            return;
        }

        float randomX = UnityEngine.Random.Range(
            minimumX,
            maximumX
        );

        Vector3 spawnPosition = new Vector3(
            randomX,
            spawnY,
            0f
        );

        FallingScoop newScoop = Instantiate(
            scoopPrefab,
            spawnPosition,
            Quaternion.identity
        );

        newScoop.Configure(SelectColor());
    }

    private ScoopColorType SelectColor()
    {
        bool spawnRequiredColor =
            UnityEngine.Random.value < requiredColorChance;

        if (spawnRequiredColor &&
            gameManager != null &&
            gameManager.TryGetCurrentRequiredColor(
                out ScoopColorType requiredColor))
        {
            return requiredColor;
        }

        int randomIndex = UnityEngine.Random.Range(
            0,
            availableColors.Length
        );

        return availableColors[randomIndex];
    }
}