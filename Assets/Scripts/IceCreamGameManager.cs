using System;
using System.Collections.Generic;
using UnityEngine;

public class IceCreamGameManager : MonoBehaviour
{
    [Header("Player references")]
    [SerializeField] private Transform stackRoot;
    [SerializeField] private ConeCollector coneCollector;
    [SerializeField] private ConeMovement coneMovement;

    [Header("Reference panel")]
    [SerializeField] private OrderReferenceView referenceView;

    [Header("Stack appearance")]
    [SerializeField] private float stackSpacing = 0.65f;
    [SerializeField] private int firstScoopSortingOrder = 10;

    [Header("Dash Interaction")]
    [Tooltip(
        "Permite recoger bolas correctas y doradas durante el dash."
    )]
    [SerializeField]
    private bool collectCorrectScoopsDuringDash = true;

    private readonly List<ScoopColorType> scoopOrder =
        new List<ScoopColorType>();

    private readonly List<FallingScoop> collectedScoops =
        new List<FallingScoop>();

    private int currentOrderIndex;

    private bool isComplete;
    private bool collectionEnabled;

    public event Action OrderCompleted;

    // LivesManager escucha indirectamente este evento
    // desde IceCreamLevelManager.
    public event Action WrongScoopCollected;

    public bool IsComplete => isComplete;
    public bool IsCollectionEnabled => collectionEnabled;

    public int CurrentOrderLength =>
        scoopOrder.Count;

    public int CollectedScoopCount =>
        currentOrderIndex;

    private void Awake()
    {
        if (coneMovement == null)
        {
            coneMovement =
                FindFirstObjectByType<ConeMovement>();
        }
    }

    // --------------------------------------------------
    // ORDER
    // --------------------------------------------------

    public void PrepareOrder(
        IReadOnlyList<ScoopColorType> newOrder)
    {
        collectionEnabled = false;
        isComplete = false;
        currentOrderIndex = 0;

        ClearCollectedScoops();

        scoopOrder.Clear();

        if (newOrder == null ||
            newOrder.Count == 0)
        {
            Debug.LogError(
                "IceCreamGameManager recibió una secuencia vacía.",
                this
            );

            return;
        }

        for (int i = 0; i < newOrder.Count; i++)
        {
            scoopOrder.Add(newOrder[i]);
        }

        if (referenceView != null)
        {
            referenceView.BuildReference(
                scoopOrder
            );
        }

        if (coneCollector != null)
        {
            coneCollector.SetStackLevel(0);
        }

        Debug.Log(
            $"Pedido preparado. Primera bola: {scoopOrder[0]}"
        );
    }

    public void StartPreparedOrder()
    {
        if (scoopOrder.Count == 0)
        {
            Debug.LogError(
                "No existe un pedido preparado.",
                this
            );

            return;
        }

        isComplete = false;
        collectionEnabled = true;

        Debug.Log(
            $"Pedido iniciado. Se espera: " +
            $"{scoopOrder[currentOrderIndex]}"
        );
    }

    public void StopCollection()
    {
        collectionEnabled = false;
    }

    public bool TryGetCurrentRequiredColor(
        out ScoopColorType requiredColor)
    {
        if (
            scoopOrder.Count == 0 ||
            isComplete ||
            currentOrderIndex >= scoopOrder.Count
        )
        {
            requiredColor = default;
            return false;
        }

        requiredColor =
            scoopOrder[currentOrderIndex];

        return true;
    }

    // --------------------------------------------------
    // COLLISION
    // --------------------------------------------------

    public void TryCollectScoop(
        FallingScoop scoop)
    {
        if (
            !collectionEnabled ||
            isComplete ||
            scoop == null ||
            scoop.IsResolved
        )
        {
            return;
        }

        if (!TryGetCurrentRequiredColor(
                out ScoopColorType requiredColor))
        {
            return;
        }

        bool playerIsInvulnerable =
            coneMovement != null &&
            coneMovement.IsInvulnerable;

        SpecialScoop specialScoop =
            scoop.GetComponent<SpecialScoop>();

        // --------------------------------------------------
        // SPECIAL SCOOPS
        // --------------------------------------------------

        if (specialScoop != null)
        {
            switch (specialScoop.Type)
            {
                case SpecialScoopType.Bomb:

                    // Durante el dash la bomba
                    // simplemente atraviesa al jugador.
                    if (playerIsInvulnerable)
                    {
                        return;
                    }

                    HandleBombScoop(scoop);
                    return;

                case SpecialScoopType.Golden:

                    // La bola dorada equivale siempre
                    // a la próxima bola correcta.

                    if (
                        playerIsInvulnerable &&
                        !collectCorrectScoopsDuringDash
                    )
                    {
                        return;
                    }

                    HandleGoldenScoop(scoop);
                    return;
            }
        }

        // --------------------------------------------------
        // NORMAL SCOOPS
        // --------------------------------------------------

        if (playerIsInvulnerable)
        {
            // Las incorrectas atraviesan el jugador.
            if (scoop.ColorType != requiredColor)
            {
                return;
            }

            if (!collectCorrectScoopsDuringDash)
            {
                return;
            }
        }

        if (scoop.ColorType != requiredColor)
        {
            HandleWrongScoop(scoop);
            return;
        }

        CollectCorrectScoop(scoop);
    }

    // --------------------------------------------------
    // GOLDEN
    // --------------------------------------------------

    private void HandleGoldenScoop(
        FallingScoop scoop)
    {
        Debug.Log(
            $"¡Comodín dorado! Sustituye a " +
            $"{scoopOrder[currentOrderIndex]}."
        );

        // Se trata exactamente como una bola correcta.
        // No modificamos su SpriteRenderer, por lo que
        // seguirá viéndose dorada al quedar apilada.
        CollectCorrectScoop(scoop);
    }

    // --------------------------------------------------
    // BOMB
    // --------------------------------------------------

    private void HandleBombScoop(
        FallingScoop scoop)
    {
        Debug.Log(
            "¡BOMBA! El helado actual ha sido destruido."
        );

        // Destruimos la bomba.
        scoop.Reject();

        // Destruimos todas las bolas acumuladas
        // y volvemos al inicio del mismo pedido.
        ResetCurrentProgress();

        // Quitamos UNA única vida.
        WrongScoopCollected?.Invoke();
    }

    // --------------------------------------------------
    // NORMAL WRONG SCOOP
    // --------------------------------------------------

    private void HandleWrongScoop(
        FallingScoop scoop)
    {
        Debug.Log(
            $"Incorrecta. Se esperaba " +
            $"{scoopOrder[currentOrderIndex]}, " +
            $"pero llegó {scoop.ColorType}."
        );

        scoop.Reject();

        WrongScoopCollected?.Invoke();
    }

    // --------------------------------------------------
    // CORRECT SCOOP
    // --------------------------------------------------

    private void CollectCorrectScoop(
        FallingScoop scoop)
    {
        int stackIndex =
            currentOrderIndex;

        Vector3 localPosition =
            Vector3.up *
            stackSpacing *
            stackIndex;

        scoop.Collect(
            stackRoot,
            localPosition,
            firstScoopSortingOrder + stackIndex
        );

        collectedScoops.Add(scoop);

        if (referenceView != null)
        {
            referenceView.MarkCollected(
                stackIndex
            );
        }

        currentOrderIndex++;

        if (coneCollector != null)
        {
            coneCollector.SetStackLevel(
                currentOrderIndex
            );
        }

        if (currentOrderIndex >= scoopOrder.Count)
        {
            CompleteOrder();
            return;
        }

        Debug.Log(
            $"Siguiente bola: " +
            $"{scoopOrder[currentOrderIndex]}"
        );
    }

    // --------------------------------------------------
    // RESET DEL HELADO
    // --------------------------------------------------

    public void ResetCurrentProgress()
    {
        bool wasCollectionEnabled =
            collectionEnabled;

        ClearCollectedScoops();

        currentOrderIndex = 0;
        isComplete = false;

        collectionEnabled =
            wasCollectionEnabled;

        if (referenceView != null)
        {
            referenceView.ResetProgress();
        }

        if (coneCollector != null)
        {
            coneCollector.SetStackLevel(0);
        }

        if (scoopOrder.Count > 0)
        {
            Debug.Log(
                $"Helado destruido. Volvemos a: " +
                $"{scoopOrder[0]}"
            );
        }
    }

    // --------------------------------------------------

    private void CompleteOrder()
    {
        isComplete = true;
        collectionEnabled = false;

        Debug.Log("¡Helado completado!");

        OrderCompleted?.Invoke();
    }

    private void ClearCollectedScoops()
    {
        for (
            int i = 0;
            i < collectedScoops.Count;
            i++
        )
        {
            if (collectedScoops[i] != null)
            {
                Destroy(
                    collectedScoops[i]
                        .gameObject
                );
            }
        }

        collectedScoops.Clear();

        currentOrderIndex = 0;

        if (coneCollector != null)
        {
            coneCollector.SetStackLevel(0);
        }
    }
}