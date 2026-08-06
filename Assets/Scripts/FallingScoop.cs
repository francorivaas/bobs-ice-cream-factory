using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingScoop : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private Collider2D scoopCollider;

    [Header("Cleanup")]
    [SerializeField] private float destroyBelowY = -8f;

    public ScoopColorType ColorType { get; private set; }
    public bool IsResolved { get; private set; }

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        scoopCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (rigidBody == null)
            rigidBody = GetComponent<Rigidbody2D>();

        if (scoopCollider == null)
            scoopCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!IsResolved && transform.position.y < destroyBelowY)
        {
            Destroy(gameObject);
        }
    }

    public void Configure(ScoopColorType newColor)
    {
        ColorType = newColor;
        spriteRenderer.color = ScoopColorPalette.GetColor(newColor);

        name = "Scoop_" + newColor;
    }

    public void Collect(
        Transform stackParent,
        Vector3 localStackPosition,
        int sortingOrder)
    {
        if (IsResolved)
            return;

        IsResolved = true;

        rigidBody.simulated = false;
        scoopCollider.enabled = false;

        transform.SetParent(stackParent, false);
        transform.localPosition = localStackPosition;
        transform.localRotation = Quaternion.identity;

        spriteRenderer.sortingOrder = sortingOrder;
    }

    public void Reject()
    {
        if (IsResolved)
            return;

        IsResolved = true;
        Destroy(gameObject);
    }
}