using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lanzamiento con los controles del Quest, estilo Wii:
/// presiona el gatillo para tomar la bola, balancea el brazo y suelta el gatillo para lanzar.
/// La fuerza y la dirección salen del movimiento real del control en el momento de soltar;
/// el efecto sale del giro de muñeca.
/// Cualquiera de las dos manos puede lanzar: la que presione primero el gatillo.
/// </summary>
public class VRThrowInput : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BallLauncher ball;
    [Tooltip("SalidaBola: su eje Z apunta hacia los pinos")]
    [SerializeField] private Transform laneDirection;
    [Tooltip("LeftControllerAnchor (dentro del Camera Rig)")]
    [SerializeField] private Transform leftController;
    [Tooltip("RightControllerAnchor (dentro del Camera Rig)")]
    [SerializeField] private Transform rightController;

    [Header("Gatillo (histéresis)")]
    [Tooltip("Por encima de este valor el gatillo cuenta como presionado")]
    [Range(0f, 1f)] [SerializeField] private float pressThreshold = 0.55f;
    [Tooltip("Por debajo de este valor el gatillo cuenta como suelto")]
    [Range(0f, 1f)] [SerializeField] private float releaseThreshold = 0.35f;

    [Header("Bola en la mano")]
    [Tooltip("Posición de la bola respecto al control, en metros (X derecha, Y arriba, Z adelante)")]
    [SerializeField] private Vector3 holdOffset = new Vector3(0f, -0.04f, 0.12f);

    [Header("Fuerza del lanzamiento")]
    [Tooltip("Segundos de movimiento que se promedian para calcular la velocidad al soltar")]
    [SerializeField] private float velocityWindow = 0.08f;
    [Tooltip("Multiplica la velocidad de la mano (más de 1 = ayuda a lanzar más fuerte)")]
    [SerializeField] private float speedMultiplier = 1.3f;
    [Tooltip("Velocidad mínima hacia los pinos (m/s). Por debajo, la bola vuelve sin contar como tiro")]
    [SerializeField] private float minForwardSpeed = 1f;
    [Tooltip("Límite de velocidad hacia arriba (m/s), para evitar bolas voladoras")]
    [SerializeField] private float maxUpwardSpeed = 0.5f;
    [Tooltip("Límite de velocidad hacia abajo (m/s), para evitar rebotes fuertes contra el piso")]
    [SerializeField] private float maxDownwardSpeed = 1.5f;

    [Header("Ayuda de dirección")]
    [Tooltip("0 = dirección real de la mano; 1 = siempre recto por el centro de la pista")]
    [Range(0f, 1f)] [SerializeField] private float aimAssist = 0.3f;

    [Header("Efecto (giro de muñeca)")]
    [Tooltip("Giro de muñeca (rad/s) que produce el efecto máximo")]
    [SerializeField] private float fullSpinTwistRate = 8f;
    [Tooltip("Actívalo si la curva sale hacia el lado contrario al giro de la muñeca")]
    [SerializeField] private bool invertSpin = false;

    [Header("Vibración")]
    [SerializeField] private float grabHapticAmplitude = 0.3f;
    [SerializeField] private float releaseHapticAmplitude = 0.7f;
    [SerializeField] private float hapticDuration = 0.08f;

    private const float MaxSampleAge = 0.3f;

    private struct PoseSample
    {
        public float Time;
        public Vector3 Position;
        public Quaternion Rotation;

        public PoseSample(float time, Vector3 position, Quaternion rotation)
        {
            Time = time;
            Position = position;
            Rotation = rotation;
        }
    }

    private class Hand
    {
        public Transform Anchor;
        public OVRInput.Controller Controller;
        public bool TriggerDown;
        public bool JustPressed;
        public bool JustReleased;
        public readonly List<PoseSample> Samples = new List<PoseSample>();
    }

    private Hand left;
    private Hand right;
    private Hand holdingHand;

    // Evita que el clic de un menú se convierta en un agarre: hay que soltar ambos gatillos primero.
    private bool armed;

    private void Awake()
    {
        if (gameManager == null || ball == null || laneDirection == null ||
            leftController == null || rightController == null)
        {
            Debug.LogError("VRThrowInput: faltan referencias en el Inspector.", this);
            enabled = false;
            return;
        }

        left = new Hand { Anchor = leftController, Controller = OVRInput.Controller.LTouch };
        right = new Hand { Anchor = rightController, Controller = OVRInput.Controller.RTouch };
    }

    private void Update()
    {
        UpdateTrigger(left);
        UpdateTrigger(right);

        if (gameManager.State != GameManager.GameState.WaitingThrow)
        {
            holdingHand = null;
            armed = false;
            return;
        }

        if (!armed)
        {
            if (left.TriggerDown || right.TriggerDown) return;
            armed = true;
        }

        if (holdingHand == null)
        {
            if (left.JustPressed) StartHold(left);
            else if (right.JustPressed) StartHold(right);
        }
        else if (holdingHand.JustReleased)
        {
            Release(holdingHand);
        }
    }

    private void LateUpdate()
    {
        // Se muestrea en LateUpdate para usar la posición más reciente de los controles.
        float now = Time.time;
        Sample(left, now);
        Sample(right, now);

        if (holdingHand != null)
        {
            ball.HoldAt(BallPositionFor(holdingHand), holdingHand.Anchor.rotation);
        }
    }

    private void UpdateTrigger(Hand hand)
    {
        float value = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, hand.Controller);
        hand.JustPressed = false;
        hand.JustReleased = false;

        if (!hand.TriggerDown && value > pressThreshold)
        {
            hand.TriggerDown = true;
            hand.JustPressed = true;
        }
        else if (hand.TriggerDown && value < releaseThreshold)
        {
            hand.TriggerDown = false;
            hand.JustReleased = true;
        }
    }

    private void Sample(Hand hand, float now)
    {
        hand.Samples.Add(new PoseSample(now, hand.Anchor.position, hand.Anchor.rotation));
        while (hand.Samples.Count > 0 && now - hand.Samples[0].Time > MaxSampleAge)
        {
            hand.Samples.RemoveAt(0);
        }
    }

    private Vector3 BallPositionFor(Hand hand)
    {
        return hand.Anchor.position + hand.Anchor.rotation * holdOffset;
    }

    private void StartHold(Hand hand)
    {
        holdingHand = hand;
        ball.HoldAt(BallPositionFor(hand), hand.Anchor.rotation);
        Haptic(hand, grabHapticAmplitude);
    }

    private void Release(Hand hand)
    {
        holdingHand = null;
        ComputeHandMotion(hand, out Vector3 handVelocity, out Vector3 handAngularVelocity);

        Vector3 forward = Flat(laneDirection.forward).normalized;
        Vector3 horizontal = Flat(handVelocity) * speedMultiplier;
        float forwardSpeed = Vector3.Dot(horizontal, forward);

        if (forwardSpeed < minForwardSpeed)
        {
            // Soltó sin fuerza o hacia atrás: la bola vuelve a la salida y no cuenta como tiro.
            ball.ResetBall();
            return;
        }

        // Ayuda de dirección: acerca la dirección de la mano a la del centro de la pista.
        Vector3 direction = Vector3.Slerp(horizontal.normalized, forward, aimAssist);
        Vector3 launchVelocity = direction * horizontal.magnitude;
        launchVelocity.y = Mathf.Clamp(handVelocity.y * speedMultiplier, -maxDownwardSpeed, maxUpwardSpeed);

        // Efecto: giro de la muñeca alrededor del eje de la pista.
        float twist = Vector3.Dot(handAngularVelocity, forward);
        float spin = Mathf.Clamp(twist / fullSpinTwistRate, -1f, 1f);
        if (invertSpin) spin = -spin;

        if (gameManager.TryLaunch(launchVelocity, spin))
        {
            Haptic(hand, releaseHapticAmplitude);
        }
        else
        {
            ball.ResetBall();
        }
    }

    /// <summary>Velocidad y velocidad angular promedio de la mano en la ventana de tiempo.</summary>
    private void ComputeHandMotion(Hand hand, out Vector3 velocity, out Vector3 angularVelocity)
    {
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        List<PoseSample> samples = hand.Samples;
        if (samples.Count < 2) return;

        PoseSample last = samples[samples.Count - 1];
        int firstIndex = samples.Count - 2;
        while (firstIndex > 0 && last.Time - samples[firstIndex - 1].Time <= velocityWindow)
        {
            firstIndex--;
        }
        PoseSample first = samples[firstIndex];

        float dt = last.Time - first.Time;
        if (dt <= 0.0001f) return;

        velocity = (last.Position - first.Position) / dt;

        Quaternion delta = last.Rotation * Quaternion.Inverse(first.Rotation);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (float.IsNaN(axis.x) || float.IsInfinity(axis.x)) return;
        if (angleDeg > 180f) angleDeg -= 360f;

        angularVelocity = axis.normalized * (angleDeg * Mathf.Deg2Rad / dt);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private void Haptic(Hand hand, float amplitude)
    {
        StartCoroutine(HapticPulse(hand.Controller, amplitude));
    }

    private IEnumerator HapticPulse(OVRInput.Controller controller, float amplitude)
    {
        OVRInput.SetControllerVibration(1f, amplitude, controller);
        yield return new WaitForSeconds(hapticDuration);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}
