using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ConeMovement : MonoBehaviour
{
    [Header("Normal Movement")]
    [SerializeField] private float followSpeed = 25f;
    [SerializeField] private float keyboardSpeed = 8f;

    [Header("Horizontal Limits")]
    [SerializeField] private float minX = -7f;
    [SerializeField] private float maxX = 3.5f;

    [Header("Camera")]
    [SerializeField] private Camera gameplayCamera;

    // --------------------------------------------------
    // DASH
    // --------------------------------------------------

    [Header("Dash")]
    [SerializeField] private bool dashEnabled = true;

    [Tooltip("Distancia horizontal recorrida por el dash.")]
    [Min(0.1f)]
    [SerializeField] private float dashDistance = 2.5f;

    [Tooltip("Duración total del desplazamiento.")]
    [Min(0.01f)]
    [SerializeField] private float dashDuration = 0.16f;

    [Tooltip("Tiempo mínimo entre el inicio de dos dashes.")]
    [Min(0f)]
    [SerializeField] private float dashCooldown = 0.7f;

    [Tooltip("Si está activo, las bolas incorrectas no hacen daño durante el dash.")]
    [SerializeField] private bool invulnerableDuringDash = true;

    [Tooltip("Curva de desplazamiento del dash.")]
    [SerializeField]
    private AnimationCurve dashMovementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // --------------------------------------------------
    // DASH VISUAL
    // --------------------------------------------------

    [Header("Dash Visual Feedback")]

    [Tooltip(
        "Objeto que contiene todos los SpriteRenderer del helado. " +
        "Si queda vacío se utilizará este mismo GameObject."
    )]
    [SerializeField] private Transform dashVisualRoot;

    [Range(0.05f, 1f)]
    [SerializeField] private float dashOpacity = 0.45f;

    // --------------------------------------------------

    private float targetX;
    private float pointerOffsetX;

    private bool isDragging;
    private bool isDashing;

    private float nextDashAllowedTime;

    private Coroutine dashCoroutine;

    private readonly Dictionary<SpriteRenderer, float>
        cachedRendererAlphas =
            new Dictionary<SpriteRenderer, float>();

    // --------------------------------------------------
    // PUBLIC
    // --------------------------------------------------

    public bool IsDashing => isDashing;

    public bool IsInvulnerable =>
        isDashing && invulnerableDuringDash;

    // --------------------------------------------------

    private void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (dashVisualRoot == null)
        {
            dashVisualRoot = transform;
        }

        targetX = transform.position.x;
    }

    private void Update()
    {
        // Mientras hacemos dash, el coroutine controla
        // completamente la posición.
        if (isDashing)
            return;

        HandlePointerInput();

        float keyboardDirection =
            ReadKeyboardDirection();

        // Primero comprobamos el dash.
        if (ReadDashPressed())
        {
            TryDash(keyboardDirection);

            if (isDashing)
                return;
        }

        HandleKeyboardInput(keyboardDirection);

        MoveCone();
    }

    // --------------------------------------------------
    // NORMAL MOVEMENT
    // --------------------------------------------------

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
            out releasedThisFrame
        );

        if (pressedThisFrame)
        {
            float pointerWorldX =
                ScreenToWorldX(screenPosition);

            pointerOffsetX =
                transform.position.x - pointerWorldX;

            isDragging = true;
        }

        if (isPressed && isDragging)
        {
            float pointerWorldX =
                ScreenToWorldX(screenPosition);

            targetX =
                pointerWorldX + pointerOffsetX;
        }

        if (releasedThisFrame)
        {
            isDragging = false;
        }
    }

    private void HandleKeyboardInput(
        float direction)
    {
        if (isDragging)
            return;

        if (Mathf.Abs(direction) < 0.01f)
            return;

        targetX +=
            direction *
            keyboardSpeed *
            Time.deltaTime;
    }

    private void MoveCone()
    {
        targetX =
            Mathf.Clamp(
                targetX,
                minX,
                maxX
            );

        Vector3 currentPosition =
            transform.position;

        float nextX =
            Mathf.MoveTowards(
                currentPosition.x,
                targetX,
                followSpeed * Time.deltaTime
            );

        transform.position =
            new Vector3(
                nextX,
                currentPosition.y,
                currentPosition.z
            );
    }

    // --------------------------------------------------
    // DASH
    // --------------------------------------------------

    /// <summary>
    /// Puede llamarse desde otros scripts.
    /// direction debe ser -1 o 1.
    ///
    /// Esto nos servirá más adelante para crear
    /// botones táctiles en Android.
    /// </summary>
    public bool TryDash(float direction)
    {
        if (!dashEnabled)
            return false;

        if (isDashing)
            return false;

        if (Time.time < nextDashAllowedTime)
            return false;

        if (Mathf.Abs(direction) < 0.01f)
            return false;

        direction =
            Mathf.Sign(direction);

        float startX =
            transform.position.x;

        float destinationX =
            Mathf.Clamp(
                startX + direction * dashDistance,
                minX,
                maxX
            );

        // No permitimos utilizar el dash contra una pared
        // simplemente para obtener inmunidad.
        if (Mathf.Abs(destinationX - startX) < 0.01f)
            return false;

        nextDashAllowedTime =
            Time.time + dashCooldown;

        dashCoroutine =
            StartCoroutine(
                DashRoutine(
                    direction,
                    destinationX
                )
            );

        return true;
    }

    private IEnumerator DashRoutine(
        float direction,
        float destinationX)
    {
        isDashing = true;
        isDragging = false;

        float startX =
            transform.position.x;

        float elapsedTime = 0f;

        targetX = destinationX;

        BeginDashVisual();

        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / dashDuration
                );

            float curveValue;

            if (dashMovementCurve != null)
            {
                curveValue =
                    dashMovementCurve.Evaluate(
                        normalizedTime
                    );
            }
            else
            {
                curveValue = normalizedTime;
            }

            float newX =
                Mathf.Lerp(
                    startX,
                    destinationX,
                    curveValue
                );

            Vector3 position =
                transform.position;

            position.x = newX;

            transform.position = position;

            // Esto también detecta bolas que hayan sido
            // recogidas mientras el dash estaba activo.
            RefreshDashVisual();

            yield return null;
        }

        Vector3 finalPosition =
            transform.position;

        finalPosition.x = destinationX;

        transform.position =
            finalPosition;

        targetX = destinationX;

        EndDashVisual();

        isDashing = false;
        dashCoroutine = null;
    }

    // --------------------------------------------------
    // DASH VISUAL
    // --------------------------------------------------

    private void BeginDashVisual()
    {
        cachedRendererAlphas.Clear();

        RefreshDashVisual();
    }

    private void RefreshDashVisual()
    {
        if (dashVisualRoot == null)
            return;

        SpriteRenderer[] renderers =
            dashVisualRoot.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer =
                renderers[i];

            if (renderer == null)
                continue;

            if (!cachedRendererAlphas.ContainsKey(renderer))
            {
                cachedRendererAlphas.Add(
                    renderer,
                    renderer.color.a
                );
            }

            Color color =
                renderer.color;

            color.a =
                cachedRendererAlphas[renderer] *
                dashOpacity;

            renderer.color = color;
        }
    }

    private void EndDashVisual()
    {
        foreach (
            KeyValuePair<SpriteRenderer, float>
            pair in cachedRendererAlphas)
        {
            if (pair.Key == null)
                continue;

            Color color =
                pair.Key.color;

            color.a =
                pair.Value;

            pair.Key.color =
                color;
        }

        cachedRendererAlphas.Clear();
    }

    // --------------------------------------------------
    // INPUT
    // --------------------------------------------------

    private float ReadKeyboardDirection()
    {
#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
            return 0f;

        float direction = 0f;

        if (
            Keyboard.current.leftArrowKey.isPressed ||
            Keyboard.current.aKey.isPressed
        )
        {
            direction -= 1f;
        }

        if (
            Keyboard.current.rightArrowKey.isPressed ||
            Keyboard.current.dKey.isPressed
        )
        {
            direction += 1f;
        }

        return direction;

#else

        float direction = 0f;

        if (
            Input.GetKey(KeyCode.LeftArrow) ||
            Input.GetKey(KeyCode.A)
        )
        {
            direction -= 1f;
        }

        if (
            Input.GetKey(KeyCode.RightArrow) ||
            Input.GetKey(KeyCode.D)
        )
        {
            direction += 1f;
        }

        return direction;

#endif
    }

    private bool ReadDashPressed()
    {
#if ENABLE_INPUT_SYSTEM

        if (Mouse.current == null)
            return false;

        return Mouse.current
            .rightButton
            .wasPressedThisFrame;

#else

        return Input.GetMouseButtonDown(1);

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
            var touch =
                Touchscreen.current.primaryTouch;

            bool touchIsRelevant =
                touch.press.isPressed ||
                touch.press.wasPressedThisFrame ||
                touch.press.wasReleasedThisFrame;

            if (touchIsRelevant)
            {
                screenPosition =
                    touch.position.ReadValue();

                pressedThisFrame =
                    touch.press.wasPressedThisFrame;

                isPressed =
                    touch.press.isPressed;

                releasedThisFrame =
                    touch.press.wasReleasedThisFrame;

                return;
            }
        }

        if (Mouse.current != null)
        {
            screenPosition =
                Mouse.current.position.ReadValue();

            pressedThisFrame =
                Mouse.current
                    .leftButton
                    .wasPressedThisFrame;

            isPressed =
                Mouse.current
                    .leftButton
                    .isPressed;

            releasedThisFrame =
                Mouse.current
                    .leftButton
                    .wasReleasedThisFrame;
        }

#else

        screenPosition =
            Input.mousePosition;

        pressedThisFrame =
            Input.GetMouseButtonDown(0);

        isPressed =
            Input.GetMouseButton(0);

        releasedThisFrame =
            Input.GetMouseButtonUp(0);

#endif
    }

    // --------------------------------------------------

    private float ScreenToWorldX(
        Vector2 screenPosition)
    {
        if (gameplayCamera == null)
            return transform.position.x;

        float distanceFromCamera =
            Mathf.Abs(
                transform.position.z -
                gameplayCamera.transform.position.z
            );

        Vector3 worldPosition =
            gameplayCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    distanceFromCamera
                )
            );

        return worldPosition.x;
    }

    private void OnDisable()
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        EndDashVisual();

        isDashing = false;
    }
}