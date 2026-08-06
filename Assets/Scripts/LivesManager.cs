using System;
using UnityEngine;
using UnityEngine.UI;

public class LivesManager : MonoBehaviour
{
    [Header("Lives")]
    [Min(1)]
    [SerializeField] private int startingLives = 3;

    [Header("UI Images")]
    [SerializeField] private Image[] lifeImages;

    [Header("Empty life appearance")]
    [SerializeField] private bool hideEmptyLives = true;

    [Range(0f, 1f)]
    [SerializeField] private float emptyLifeAlpha = 0.2f;

    private int currentLives;

    public int CurrentLives => currentLives;
    public int StartingLives => startingLives;

    public event Action LivesDepleted;

    private void Awake()
    {
        ResetLives();
    }

    public void LoseLife()
    {
        if (currentLives <= 0)
            return;

        currentLives--;

        UpdateLivesUI();

        Debug.Log(
            $"Vida perdida. Vidas restantes: {currentLives}"
        );

        if (currentLives <= 0)
        {
            Debug.Log("¡Sin vidas!");

            LivesDepleted?.Invoke();
        }
    }

    public void ResetLives()
    {
        currentLives = startingLives;

        UpdateLivesUI();
    }

    private void UpdateLivesUI()
    {
        if (lifeImages == null)
            return;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null)
                continue;

            bool hasLife = i < currentLives;

            if (hideEmptyLives)
            {
                lifeImages[i].gameObject.SetActive(hasLife);
            }
            else
            {
                lifeImages[i].gameObject.SetActive(true);

                Color color = lifeImages[i].color;

                color.a = hasLife
                    ? 1f
                    : emptyLifeAlpha;

                lifeImages[i].color = color;
            }
        }
    }

    private void OnValidate()
    {
        startingLives = Mathf.Max(1, startingLives);
    }
}