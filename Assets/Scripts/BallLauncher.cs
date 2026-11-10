using UnityEngine;

/// <summary>
/// Controla la bola: la deja lista en el punto de salida, la sigue a la mano mientras
/// el jugador la sostiene, la lanza con una velocidad y un efecto, y aplica la curva
/// mientras rueda.
/// No lee ningún control: el lanzamiento lo piden otros scripts
/// (DebugThrowInput con teclado, VRThrowInput con el gatillo).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallLauncher : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto de salida de la bola. Su eje Z (forward) apunta hacia los pinos")]
    [SerializeField] private Transform spawnPoint;

    [Header("Lanzamiento")]
    [Tooltip("Velocidad máxima de salida en m/s (un tiro rápido real ronda los 9 m/s)")]
    [SerializeField] private float maxSpeed = 10f;

    [Header("Efecto (curva)")]
    [Tooltip("Aceleración lateral en m/s² por cada unidad de efecto")]
    [SerializeField] private float hookAcceleration = 1.2f;
    [Tooltip("Segundos que la bola rueda recta antes de empezar a curvar")]
    [SerializeField] private float hookDelay = 0.6f;
    [Tooltip("Por debajo de esta velocidad (m/s) la bola deja de curvar")]
    [SerializeField] private float minHookSpeed = 1f;

    private Rigidbody rb;
    private float spin;
    private float launchTime;

    public bool IsLaunched { get; private set; }
    public bool IsHeld { get; private set; }
    public float Speed => rb.linearVelocity.magnitude;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (spawnPoint == null)
        {
            Debug.LogError("BallLauncher: falta asignar el punto de salida (spawnPoint).", this);
        }
    }

    private void Start()
    {
        ResetBall();
    }

    /// <summary>Mueve la bola con la mano del jugador, sin física, hasta que se lance.</summary>
    public void HoldAt(Vector3 position, Quaternion rotation)
    {
        if (IsLaunched) return;

        if (!IsHeld)
        {
            IsHeld = true;
            rb.isKinematic = true;
            // Sin interpolación mientras se sostiene, para que no se retrase respecto a la mano.
            rb.interpolation = RigidbodyInterpolation.None;
        }

        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// Lanza la bola desde donde está. spinAmount entre -1 y 1: positivo curva hacia
    /// la derecha, negativo hacia la izquierda (vista desde el jugador).
    /// </summary>
    public void Launch(Vector3 velocity, float spinAmount)
    {
        if (IsLaunched) return;

        IsHeld = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        rb.angularVelocity = Vector3.zero;

        spin = Mathf.Clamp(spinAmount, -1f, 1f);
        launchTime = Time.time;
        IsLaunched = true;
    }

    /// <summary>Devuelve la bola al punto de salida, quieta.</summary>
    public void ResetBall()
    {
        IsLaunched = false;
        IsHeld = false;
        spin = 0f;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (spawnPoint != null)
        {
            rb.position = spawnPoint.position;
            rb.rotation = spawnPoint.rotation;
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
    }

    private void FixedUpdate()
    {
        if (!IsLaunched || Mathf.Approximately(spin, 0f)) return;
        if (Time.time - launchTime < hookDelay || Speed < minHookSpeed) return;

        Vector3 lateral = spawnPoint.right;
        rb.AddForce(lateral * (spin * hookAcceleration), ForceMode.Acceleration);
    }
}
