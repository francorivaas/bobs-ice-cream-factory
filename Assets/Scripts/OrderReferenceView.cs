using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderReferenceView : MonoBehaviour
{
    [Serializable]
    private class ScoopReferenceSprite
    {
        public ScoopColorType scoopColor;
        public Sprite sprite;
    }

    [Header("References")]
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private GameObject referenceScoopPrefab;

    [Header("Scoop Sprites")]
    [SerializeField]
    private List<ScoopReferenceSprite> scoopSprites =
        new List<ScoopReferenceSprite>();

    [Header("UI Layout")]
    [SerializeField] private float verticalSpacing = 85f;

    [Header("Completed Appearance")]
    [Range(0f, 1f)]
    [SerializeField] private float completedAlpha = 0.25f;

    [SerializeField] private float completedScale = 0.85f;

    private readonly List<Image> scoopViews =
        new List<Image>();

    public void BuildReference(
        IReadOnlyList<ScoopColorType> sequence)
    {
        ClearReference();

        if (listRoot == null ||
            referenceScoopPrefab == null)
        {
            Debug.LogError(
                "OrderReferenceView no está configurado.",
                this
            );

            return;
        }

        for (int i = 0; i < sequence.Count; i++)
        {
            GameObject newView =
                Instantiate(
                    referenceScoopPrefab,
                    listRoot
                );

            RectTransform rectTransform =
                newView.GetComponent<RectTransform>();

            Image image =
                newView.GetComponent<Image>();

            if (rectTransform == null ||
                image == null)
            {
                Debug.LogError(
                    "ReferenceScoopPrefab necesita " +
                    "RectTransform e Image.",
                    newView
                );

                Destroy(newView);

                continue;
            }

            Sprite sprite =
                GetSpriteForColor(sequence[i]);

            if (sprite == null)
            {
                Debug.LogError(
                    $"No hay sprite de referencia " +
                    $"para {sequence[i]}.",
                    this
                );
            }

            image.sprite = sprite;

            // Ya no tintamos la imagen.
            image.color = Color.white;

            image.preserveAspect = true;

            rectTransform.anchorMin =
                new Vector2(0.5f, 0f);

            rectTransform.anchorMax =
                new Vector2(0.5f, 0f);

            rectTransform.pivot =
                new Vector2(0.5f, 0.5f);

            rectTransform.anchoredPosition =
                new Vector2(
                    0f,
                    i * verticalSpacing
                );

            rectTransform.localScale =
                Vector3.one;

            scoopViews.Add(image);
        }
    }

    public void MarkCollected(int index)
    {
        if (index < 0 ||
            index >= scoopViews.Count)
        {
            return;
        }

        Image image =
            scoopViews[index];

        Color color =
            image.color;

        color.a =
            completedAlpha;

        image.color = color;

        image.rectTransform.localScale =
            Vector3.one *
            completedScale;
    }

    public void ResetProgress()
    {
        for (int i = 0;
             i < scoopViews.Count;
             i++)
        {
            if (scoopViews[i] == null)
                continue;

            scoopViews[i].color =
                Color.white;

            scoopViews[i]
                .rectTransform
                .localScale =
                Vector3.one;
        }
    }

    private Sprite GetSpriteForColor(ScoopColorType color)
    {
        for (int i = 0; i < scoopSprites.Count; i++)
        {
            if (scoopSprites[i].scoopColor == color)
            {
                return scoopSprites[i].sprite;
            }
        }

        return null;
    }

    private void ClearReference()
    {
        scoopViews.Clear();

        if (listRoot == null)
            return;

        for (
            int i = listRoot.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                listRoot.GetChild(i).gameObject
            );
        }
    }
}