using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ConeCollector : MonoBehaviour
{
    [SerializeField] private IceCreamGameManager gameManager;

    [Header("Stack")]
    [SerializeField] private float verticalStep = 0.65f;

    private Vector3 initialLocalPosition;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FallingScoop scoop =
            other.GetComponentInParent<FallingScoop>();

        if (scoop == null || scoop.IsResolved)
            return;

        if (gameManager == null)
        {
            Debug.LogError(
                "ConeCollector no tiene asignado el GameManager.",
                this);

            return;
        }

        gameManager.TryCollectScoop(scoop);
    }

    public void SetStackLevel(int collectedScoopCount)
    {
        transform.localPosition =
            initialLocalPosition +
            Vector3.up * verticalStep * collectedScoopCount;
    }
}