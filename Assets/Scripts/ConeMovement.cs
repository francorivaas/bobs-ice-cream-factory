using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ConeMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float followSpeed = 25f;
    [SerializeField] private float keyboardSpeed = 8f;

    [Header("Horizontal limits")]
    [SerializeField] private float minX = -7f;
    [SerializeField] private float maxX = 3.5f;

    [Header("Camera")]
    [SerializeField] private Camera gameplayCamera;

    private float targetX;
    private float pointerOffsetX;
    private bool isDragging;

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        targetX = transform.position.x;
    }

    private void Update()
    {
        HandlePointerInput();
        HandleKeyboardInput();
        MoveCone();
    }

    private void HandlePointerInput()
    {
        Vector2 screenPosition;
        bool pressedThisFrame;
        bool isPressed;
        bool releasedThisFrame;

        ReadPointerState(
            out screenPosition,
            out pressedThisFrame,
            out isPressed,
            out releasedThisFrame);

        if (pressedThisFrame)
        {
            float pointerWorldX = ScreenToWorldX(screenPosition);

            pointerOffsetX = transform.position.x - pointerWorldX;
            isDragging = true;
        }

        if (isPressed && isDragging)
        {
            float pointerWorldX = ScreenToWorldX(screenPosition);
            targetX = pointerWorldX + pointerOffsetX;
        }

        if (releasedThisFrame)
        {
            isDragging = false;
        }
    }

    private void HandleKeyboardInput()
    {
        if (isDragging)
            return;

        float direction = ReadKeyboardDirection();

        if (Mathf.Abs(direction) > 0.01f)
        {
            targetX += direction * keyboardSpeed * Time.deltaTime;
        }
    }

    private void MoveCone()
    {
        targetX = Mathf.Clamp(targetX, minX, maxX);

        Vector3 currentPosition = transform.position;

        float nextX = Mathf.MoveTowards(
            currentPosition.x,
            targetX,
            followSpeed * Time.deltaTime);

        transform.position = new Vector3(
            nextX,
            currentPosition.y,
            currentPosition.z);
    }

    private float ScreenToWorldX(Vector2 screenPosition)
    {
        if (gameplayCamera == null)
            return transform.position.x;

        float distanceFromCamera =
            Mathf.Abs(transform.position.z - gameplayCamera.transform.position.z);

        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                distanceFromCamera));

        return worldPosition.x;
    }

    private float ReadKeyboardDirection()
    {
#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
            return 0f;

        float direction = 0f;

        if (Keyboard.current.leftArrowKey.isPressed ||
            Keyboard.current.aKey.isPressed)
        {
            direction -= 1f;
        }

        if (Keyboard.current.rightArrowKey.isPressed ||
            Keyboard.current.dKey.isPressed)
        {
            direction += 1f;
        }

        return direction;

#else

        float direction = 0f;

        if (Input.GetKey(KeyCode.LeftArrow) ||
            Input.GetKey(KeyCode.A))
        {
            direction -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow) ||
            Input.GetKey(KeyCode.D))
        {
            direction += 1f;
        }

        return direction;

#endif
    }

    private void ReadPointerState(
        out Vector2 screenPosition,
        out bool pressedThisFrame,
        out bool isPressed,
        out bool releasedThisFrame)
    {
        screenPosition = Vector2.zero;
        pressedThisFrame = false;
        isPressed = false;
        releasedThisFrame = false;

#if ENABLE_INPUT_SYSTEM

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            bool touchIsRelevant =
                touch.press.isPressed ||
                touch.press.wasPressedThisFrame ||
                touch.press.wasReleasedThisFrame;

            if (touchIsRelevant)
            {
                screenPosition = touch.position.ReadValue();
                pressedThisFrame = touch.press.wasPressedThisFrame;
                isPressed = touch.press.isPressed;
                releasedThisFrame = touch.press.wasReleasedThisFrame;
                return;
            }
        }

        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            isPressed = Mouse.current.leftButton.isPressed;
            releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
        }

#else

        screenPosition = Input.mousePosition;
        pressedThisFrame = Input.GetMouseButtonDown(0);
        isPressed = Input.GetMouseButton(0);
        releasedThisFrame = Input.GetMouseButtonUp(0);

#endif
    }
}