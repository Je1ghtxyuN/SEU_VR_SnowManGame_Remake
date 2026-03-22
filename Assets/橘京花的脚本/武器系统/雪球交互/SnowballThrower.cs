using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

[RequireComponent(typeof(XRDirectInteractor))]
public class SnowballThrower : MonoBehaviour
{
    [Header("必填参数")]
    public GameObject snowballPrefab;
    public InputActionReference gripAction;

    [Header("生成位置")]
    [Tooltip("相对于手柄坐标系的偏移量 (X=左右, Y=上下, Z=前后)")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, -0.05f, 0.15f);

    [Header("抛掷参数")]
    [SerializeField] private float throwForceMultiplier = 1.2f;
    [SerializeField] private float gravityScale = 0.5f;
    [SerializeField] private int velocitySmoothingFrames = 10;

    private XRDirectInteractor interactor;
    private List<GameObject> activeSnowballs = new List<GameObject>();
    private Queue<Vector3> velocityHistory = new Queue<Vector3>();
    private Vector3 previousPosition;
    private Vector3 smoothedVelocity;

    public bool canThrow = true;

    private void Awake()
    {
        interactor = GetComponent<XRDirectInteractor>();

        if (gripAction != null && gripAction.action != null)
        {
            gripAction.action.Enable();
            gripAction.action.performed += OnGripPressed;
            gripAction.action.canceled += OnGripReleased;
        }
        else
        {
            Debug.LogError("Grip Action 未设置或无效! SnowballThrower 组件将被禁用。");
            enabled = false;
            return;
        }

        previousPosition = transform.position;

        for (int i = 0; i < velocitySmoothingFrames; i++)
        {
            velocityHistory.Enqueue(Vector3.zero);
        }
    }

    private void Update()
    {
        if (Time.deltaTime <= Mathf.Epsilon || Time.timeScale == 0f) return;

        Vector3 currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;

        velocityHistory.Enqueue(currentVelocity);
        if (velocityHistory.Count > velocitySmoothingFrames)
        {
            velocityHistory.Dequeue();
        }

        smoothedVelocity = CalculateSmoothedVelocity();

        for (int i = activeSnowballs.Count - 1; i >= 0; i--)
        {
            if (activeSnowballs[i] == null)
            {
                activeSnowballs.RemoveAt(i);
            }
        }
    }

    private Vector3 CalculateSmoothedVelocity()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        int framesToSkip = Mathf.FloorToInt(velocitySmoothingFrames * 0.1f);
        int framesToUse = velocitySmoothingFrames - framesToSkip;

        foreach (Vector3 velocity in velocityHistory)
        {
            if (count < framesToUse)
            {
                sum += velocity;
                count++;
            }
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    private void OnDestroy()
    {
        if (gripAction != null && gripAction.action != null)
        {
            gripAction.action.performed -= OnGripPressed;
            gripAction.action.canceled -= OnGripReleased;
        }

        //activeSnowballs.Clear();
        //velocityHistory.Clear();
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f || !canThrow) return;

        if (snowballPrefab != null)
        {
            // ⭐ 修改点：使用 TransformPoint 将局部坐标转为世界坐标
            // 这样你可以在 Inspector 里调整 X/Y/Z 偏移
            Vector3 spawnPos = transform.TransformPoint(spawnOffset);

            GameObject snowball = Instantiate(snowballPrefab, spawnPos, Quaternion.identity);
            activeSnowballs.Add(snowball);

            XRGrabInteractable grabInteractable = snowball.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = snowball.AddComponent<XRGrabInteractable>();
            }

            grabInteractable.throwOnDetach = false;
            grabInteractable.selectExited.AddListener((args) => OnSnowballReleased(args, snowball));

            Rigidbody rb = snowball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false;

                Collider snowballCollider = snowball.GetComponent<Collider>();
                if (snowballCollider != null)
                {
                    foreach (var controllerCollider in GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(snowballCollider, controllerCollider, true);
                    }
                }
            }

            if (interactor != null)
            {
                interactor.StartManualInteraction(grabInteractable as IXRSelectInteractable);
            }
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        if (interactor != null && interactor.hasSelection)
        {
            interactor.EndManualInteraction();
        }
    }

    private void OnSnowballReleased(SelectExitEventArgs args, GameObject snowball)
    {
        if (snowball == null) return;

        Rigidbody rb = snowball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (float.IsNaN(smoothedVelocity.x) || float.IsInfinity(smoothedVelocity.x))
            {
                smoothedVelocity = Vector3.zero;
            }

            Vector3 throwVelocity = smoothedVelocity * throwForceMultiplier;
            rb.velocity = throwVelocity;
            rb.angularVelocity = Vector3.Cross(transform.forward, throwVelocity).normalized *
                                throwVelocity.magnitude * 0.1f;

            SnowballGravity snowballGravity = snowball.AddComponent<SnowballGravity>();
            snowballGravity.gravityScale = gravityScale;

            Collider snowballCollider = snowball.GetComponent<Collider>();
            if (snowballCollider != null)
            {
                foreach (var controllerCollider in GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(snowballCollider, controllerCollider, false);
                }
            }
        }

        activeSnowballs.Remove(snowball);
    }
}

public class SnowballGravity : MonoBehaviour
{
    public float gravityScale = 0.5f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        }
    }
}