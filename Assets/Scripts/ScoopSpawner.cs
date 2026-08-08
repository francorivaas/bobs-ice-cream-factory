using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoopSpawner : MonoBehaviour
{
    private enum SpawnType
    {
        Normal,
        Bomb,
        Golden
    }

    // --------------------------------------------------
    // NORMAL SCOOP PREFAB ENTRY
    // --------------------------------------------------

    [Serializable]
    private class NormalScoopPrefab
    {
        public ScoopColorType color;
        public FallingScoop prefab;
    }

    // --------------------------------------------------

    [Header("Normal Scoop Prefabs")]
    [SerializeField]
    private List<NormalScoopPrefab> normalScoopPrefabs =
        new List<NormalScoopPrefab>();

    [Header("Special Prefabs")]
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

    [Min(0f)]
    [SerializeField] private float normalWeight = 90f;

    [Min(0f)]
    [SerializeField] private float bombWeight = 7f;

    [Min(0f)]
    [SerializeField] private float goldenWeight = 3f;

    // --------------------------------------------------

    [Header("Normal Scoop Settings")]

    [Tooltip(
        "Probabilidad de que una bola normal sea " +
        "exactamente el color que necesita el jugador."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float requiredColorChance = 0.35f;

    private Coroutine spawnCoroutine;

    public bool IsSpawning =>
        spawnCoroutine != null;

    // --------------------------------------------------

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

    // --------------------------------------------------

    [Obsolete]
    public void ClearLooseScoops()
    {
        FallingScoop[] scoops =
            FindObjectsByType<FallingScoop>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < scoops.Length; i++)
        {
            FallingScoop scoop = scoops[i];

            if (scoop != null &&
                !scoop.IsResolved)
            {
                Destroy(scoop.gameObject);
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

        switch (spawnType)
        {
            case SpawnType.Normal:
                SpawnNormalScoop(spawnPosition);
                break;

            case SpawnType.Bomb:
                SpawnSpecialScoop(
                    bombScoopPrefab,
                    spawnPosition,
                    SpecialScoopType.Bomb
                );
                break;

            case SpawnType.Golden:
                SpawnSpecialScoop(
                    goldenScoopPrefab,
                    spawnPosition,
                    SpecialScoopType.Golden
                );
                break;
        }
    }

    // --------------------------------------------------
    // NORMAL SCOOPS
    // --------------------------------------------------

    private void SpawnNormalScoop(
        Vector3 spawnPosition)
    {
        if (normalScoopPrefabs == null ||
            normalScoopPrefabs.Count == 0)
        {
            Debug.LogError(
                "No hay prefabs normales configurados.",
                this
            );

            return;
        }

        ScoopColorType selectedColor =
            SelectNormalColor();

        FallingScoop selectedPrefab =
            GetPrefabForColor(selectedColor);

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"No existe prefab para el color " +
                $"{selectedColor}.",
                this
            );

            return;
        }

        Instantiate(
            selectedPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    private ScoopColorType SelectNormalColor()
    {
        // Primero decidimos si queremos ayudar al jugador
        // generando el color requerido.
        bool spawnRequiredColor =
            UnityEngine.Random.value <
            requiredColorChance;

        if (
            spawnRequiredColor &&
            gameManager != null &&
            gameManager.TryGetCurrentRequiredColor(
                out ScoopColorType requiredColor
            ) &&
            GetPrefabForColor(requiredColor) != null
        )
        {
            return requiredColor;
        }

        // De lo contrario elegimos cualquiera
        // de los prefabs configurados.
        int randomIndex =
            UnityEngine.Random.Range(
                0,
                normalScoopPrefabs.Count
            );

        return normalScoopPrefabs[
            randomIndex
        ].color;
    }

    private FallingScoop GetPrefabForColor(
        ScoopColorType color)
    {
        for (int i = 0;
             i < normalScoopPrefabs.Count;
             i++)
        {
            NormalScoopPrefab entry =
                normalScoopPrefabs[i];

            if (entry.color == color)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    // --------------------------------------------------
    // SPECIAL SCOOPS
    // --------------------------------------------------

    private void SpawnSpecialScoop(
        FallingScoop prefab,
        Vector3 spawnPosition,
        SpecialScoopType type)
    {
        if (prefab == null)
        {
            Debug.LogWarning(
                $"No hay prefab configurado para {type}.",
                this
            );

            return;
        }

        FallingScoop newScoop =
            Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity
            );

        SpecialScoop special =
            newScoop.GetComponent<SpecialScoop>();

        if (special == null)
        {
            Debug.LogError(
                $"El prefab {prefab.name} necesita " +
                $"un componente SpecialScoop.",
                prefab
            );

            Destroy(newScoop.gameObject);

            return;
        }
    }

    // --------------------------------------------------
    // WEIGHTS
    // --------------------------------------------------

    private SpawnType SelectSpawnType()
    {
        float effectiveNormalWeight =
            normalScoopPrefabs.Count > 0
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

        if (totalWeight <= 0f)
        {
            return SpawnType.Normal;
        }

        float roll =
            UnityEngine.Random.Range(
                0f,
                totalWeight
            );

        if (roll < effectiveNormalWeight)
        {
            return SpawnType.Normal;
        }

        roll -= effectiveNormalWeight;

        if (roll < effectiveBombWeight)
        {
            return SpawnType.Bomb;
        }

        return SpawnType.Golden;
    }
}