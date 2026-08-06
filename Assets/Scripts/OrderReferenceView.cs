using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderReferenceView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private GameObject referenceScoopPrefab;

    [Header("UI Layout")]
    [SerializeField] private float verticalSpacing = 85f;

    [Header("Completed appearance")]
    [Range(0f, 1f)]
    [SerializeField] private float completedAlpha = 0.25f;

    [SerializeField] private float completedScale = 0.85f;

    private readonly List<Image> scoopViews = new();
    private readonly List<Color> originalColors = new();

    public void BuildReference(IReadOnlyList<ScoopColorType> sequence)
    {
        ClearReference();

        if (listRoot == null)
        {
            Debug.LogError(
                "OrderReferenceView: List Root no está asignado.",
                this
            );

            return;
        }

        if (referenceScoopPrefab == null)
        {
            Debug.LogError(
                "OrderReferenceView: Reference Scoop Prefab no está asignado.",
                this
            );

            return;
        }

        for (int i = 0; i < sequence.Count; i++)
        {
            GameObject newView = Instantiate(
                referenceScoopPrefab,
                listRoot
            );

            newView.name = $"Reference_{i}_{sequence[i]}";

            RectTransform rectTransform =
                newView.GetComponent<RectTransform>();

            Image image = newView.GetComponent<Image>();

            if (rectTransform == null || image == null)
            {
                Debug.LogError(
                    "El prefab de referencia necesita RectTransform e Image.",
                    newView
                );

                Destroy(newView);
                continue;
            }

            // El elemento 0 queda abajo y los siguientes crecen hacia arriba.
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            rectTransform.anchoredPosition =
                new Vector2(0f, i * verticalSpacing);

            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            Color color = ScoopColorPalette.GetColor(sequence[i]);

            image.color = color;

            scoopViews.Add(image);
            originalColors.Add(color);
        }
    }

    public void MarkCollected(int index)
    {
        if (index < 0 || index >= scoopViews.Count)
            return;

        Image image = scoopViews[index];

        Color completedColor = originalColors[index];
        completedColor.a = completedAlpha;

        image.color = completedColor;
        image.rectTransform.localScale =
            Vector3.one * completedScale;
    }

    public void ResetProgress()
    {
        for (int i = 0; i < scoopViews.Count; i++)
        {
            if (scoopViews[i] == null)
                continue;

            scoopViews[i].color = originalColors[i];
            scoopViews[i].rectTransform.localScale = Vector3.one;
        }
    }

    private void ClearReference()
    {
        scoopViews.Clear();
        originalColors.Clear();

        if (listRoot == null)
            return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(listRoot.GetChild(i).gameObject);
        }
    }
}