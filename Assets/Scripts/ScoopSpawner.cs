using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class ScoopSpawner : MonoBehaviour
{
    private enum SpawnType
    {
        Normal,
        Bomb,
        Golden
    }

    [Header("Prefabs")]

    [FormerlySerializedAs("scoopPrefab")]
    [SerializeField]
    private FallingScoop normalScoopPrefab;

    [SerializeField]
    private FallingScoop bombScoopPrefab;

    [SerializeField]
    private FallingScoop goldenScoopPrefab;

    [Header("References")]
    [SerializeField]
    private IceCreamGameManager gameManager;

    [Header("Spawn Area")]
    [SerializeField] private float minimumX = -7f;
    [SerializeField] private float maximumX = 3.5f;
    [SerializeField] private float spawnY = 6f;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.4f;
    [SerializeField] private float spawnInterval = 0.9f;

    // --------------------------------------------------
    // SPAWN WEIGHTS
    // --------------------------------------------------

    [Header("Spawn Weights")]

    [Tooltip(
        "Peso relativo de una bola normal."
    )]
    [Min(0f)]
    [SerializeField] private float normalWeight = 85f;

    [Tooltip(
        "Peso relativo de aparición de la bomba."
    )]
    [Min(0f)]
    [SerializeField] private float bombWeight = 10f;

    [Tooltip(
        "Peso relativo de aparición de la bola dorada."
    )]
    [Min(0f)]
    [SerializeField] private float goldenWeight = 5f;

    // --------------------------------------------------

    [Header("Normal Scoop Settings")]

    [Tooltip(
        "Cuando aparece una bola normal, probabilidad " +
        "de que sea específicamente el color requerido."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float requiredColorChance = 0.35f;

    private ScoopColorType[] availableColors;

    private Coroutine spawnCoroutine;

    public bool IsSpawning =>
        spawnCoroutine != null;

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

    // --------------------------------------------------

    public void BeginSpawning()
    {
        StopSpawning();

        spawnCoroutine =
            StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine == null)
            return;

        StopCoroutine(spawnCoroutine);

        spawnCoroutine = null;
    }

    public void ClearLooseScoops()
    {
        FallingScoop[] scoops =
            FindObjectsByType<FallingScoop>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < scoops.Length; i++)
        {
            FallingScoop scoop = scoops[i];

            if (
                scoop != null &&
                !scoop.IsResolved
            )
            {
                Destroy(
                    scoop.gameObject
                );
            }
        }
    }

    // --------------------------------------------------

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(
            initialDelay
        );

        while (true)
        {
            if (
                gameManager != null &&
                gameManager.IsCollectionEnabled &&
                !gameManager.IsComplete
            )
            {
                SpawnScoop();
            }

            yield return new WaitForSeconds(
                spawnInterval
            );
        }
    }

    // --------------------------------------------------

    private void SpawnScoop()
    {
        SpawnType spawnType =
            SelectSpawnType();

        FallingScoop prefab =
            GetPrefabForSpawnType(spawnType);

        if (prefab == null)
        {
            Debug.LogWarning(
                $"No existe prefab para {spawnType}.",
                this
            );

            return;
        }

        float randomX =
            UnityEngine.Random.Range(
                minimumX,
                maximumX
            );

        Vector3 spawnPosition =
            new Vector3(
                randomX,
                spawnY,
                0f
            );

        FallingScoop newScoop =
            Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity
            );

        switch (spawnType)
        {
            case SpawnType.Normal:

                newScoop.Configure(
                    SelectNormalColor()
                );

                break;

            case SpawnType.Bomb:

                ConfigureSpecialScoop(
                    newScoop,
                    SpecialScoopType.Bomb
                );

                break;

            case SpawnType.Golden:

                ConfigureSpecialScoop(
                    newScoop,
                    SpecialScoopType.Golden
                );

                break;
        }
    }

    // --------------------------------------------------
    // WEIGHTED RANDOM
    // --------------------------------------------------

    private SpawnType SelectSpawnType()
    {
        float effectiveNormalWeight =
            normalScoopPrefab != null
                ? normalWeight
                : 0f;

        float effectiveBombWeight =
            bombScoopPrefab != null
                ? bombWeight
                : 0f;

        float effectiveGoldenWeight =
            goldenScoopPrefab != null
                ? goldenWeight
                : 0f;

        float totalWeight =
            effectiveNormalWeight +
            effectiveBombWeight +
            effectiveGoldenWeight;

        // Seguridad.
        if (totalWeight <= 0f)
        {
            return SpawnType.Normal;
        }

        float roll =
            UnityEngine.Random.Range(
                0f,
                totalWeight
            );

        // NORMAL
        if (roll < effectiveNormalWeight)
        {
            return SpawnType.Normal;
        }

        roll -= effectiveNormalWeight;

        // BOMB
        if (roll < effectiveBombWeight)
        {
            return SpawnType.Bomb;
        }

        // GOLDEN
        return SpawnType.Golden;
    }

    // --------------------------------------------------

    private FallingScoop GetPrefabForSpawnType(
        SpawnType spawnType)
    {
        switch (spawnType)
        {
            case SpawnType.Bomb:
                return bombScoopPrefab;

            case SpawnType.Golden:
                return goldenScoopPrefab;

            default:
                return normalScoopPrefab;
        }
    }

    // --------------------------------------------------

    private ScoopColorType SelectNormalColor()
    {
        bool spawnRequiredColor =
            UnityEngine.Random.value <
            requiredColorChance;

        if (
            spawnRequiredColor &&
            gameManager != null &&
            gameManager.TryGetCurrentRequiredColor(
                out ScoopColorType requiredColor
            )
        )
        {
            return requiredColor;
        }

        int randomIndex =
            UnityEngine.Random.Range(
                0,
                availableColors.Length
            );

        return availableColors[randomIndex];
    }

    // --------------------------------------------------

    private void ConfigureSpecialScoop(
        FallingScoop scoop,
        SpecialScoopType type)
    {
        SpecialScoop special =
            scoop.GetComponent<SpecialScoop>();

        if (special == null)
        {
            special =
                scoop.gameObject.AddComponent<
                    SpecialScoop
                >();
        }

        special.Configure(type);
    }
}