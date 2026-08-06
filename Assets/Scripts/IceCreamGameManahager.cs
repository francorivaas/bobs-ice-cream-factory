using System;
using System.Collections.Generic;
using UnityEngine;

public class IceCreamGameManager : MonoBehaviour
{
    [Header("Player references")]
    [SerializeField] private Transform stackRoot;
    [SerializeField] private ConeCollector coneCollector;

    [Header("Reference panel")]
    [SerializeField] private OrderReferenceView referenceView;

    [Header("Stack appearance")]
    [SerializeField] private float stackSpacing = 0.65f;
    [SerializeField] private int firstScoopSortingOrder = 10;

    private readonly List<ScoopColorType> scoopOrder =
        new List<ScoopColorType>();

    private readonly List<FallingScoop> collectedScoops =
        new List<FallingScoop>();

    private int currentOrderIndex;

    private bool isComplete;
    private bool collectionEnabled;

    // Eventos
    public event Action OrderCompleted;
    public event Action WrongScoopCollected;

    public bool IsComplete => isComplete;
    public bool IsCollectionEnabled => collectionEnabled;

    public int CurrentOrderLength => scoopOrder.Count;
    public int CollectedScoopCount => currentOrderIndex;

    public void PrepareOrder(
        IReadOnlyList<ScoopColorType> newOrder)
    {
        collectionEnabled = false;
        isComplete = false;
        currentOrderIndex = 0;

        ClearCollectedScoops();

        scoopOrder.Clear();

        if (newOrder == null || newOrder.Count == 0)
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
            referenceView.BuildReference(scoopOrder);
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
        if (scoopOrder.Count == 0 ||
            isComplete ||
            currentOrderIndex >= scoopOrder.Count)
        {
            requiredColor = default;
            return false;
        }

        requiredColor = scoopOrder[currentOrderIndex];

        return true;
    }

    public void TryCollectScoop(FallingScoop scoop)
    {
        if (!collectionEnabled ||
            isComplete ||
            scoop == null ||
            scoop.IsResolved)
        {
            return;
        }

        if (!TryGetCurrentRequiredColor(
                out ScoopColorType requiredColor))
        {
            return;
        }

        if (scoop.ColorType != requiredColor)
        {
            HandleWrongScoop(scoop);
            return;
        }

        CollectCorrectScoop(scoop);
    }

    private void CollectCorrectScoop(FallingScoop scoop)
    {
        int stackIndex = currentOrderIndex;

        Vector3 localPosition =
            Vector3.up * stackSpacing * stackIndex;

        scoop.Collect(
            stackRoot,
            localPosition,
            firstScoopSortingOrder + stackIndex
        );

        collectedScoops.Add(scoop);

        if (referenceView != null)
        {
            referenceView.MarkCollected(stackIndex);
        }

        currentOrderIndex++;

        if (coneCollector != null)
        {
            coneCollector.SetStackLevel(currentOrderIndex);
        }

        if (currentOrderIndex >= scoopOrder.Count)
        {
            CompleteOrder();
            return;
        }

        Debug.Log(
            $"Siguiente bola: {scoopOrder[currentOrderIndex]}"
        );
    }

    private void HandleWrongScoop(FallingScoop scoop)
    {
        Debug.Log(
            $"Incorrecta. Se esperaba " +
            $"{scoopOrder[currentOrderIndex]}, " +
            $"pero llegó {scoop.ColorType}."
        );

        // Eliminamos la bola equivocada.
        scoop.Reject();

        // Avisamos al sistema de vidas.
        WrongScoopCollected?.Invoke();
    }

    private void CompleteOrder()
    {
        isComplete = true;
        collectionEnabled = false;

        Debug.Log("¡Helado completado!");

        OrderCompleted?.Invoke();
    }

    private void ClearCollectedScoops()
    {
        for (int i = 0; i < collectedScoops.Count; i++)
        {
            if (collectedScoops[i] != null)
            {
                Destroy(
                    collectedScoops[i].gameObject
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