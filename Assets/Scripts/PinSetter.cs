using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coloca los 10 pinos en triángulo, detecta los caídos, retira los caídos y reinicia la formación.
/// Coloca este objeto en el punto del primer pino, sobre la superficie de la pista,
/// con su eje Z azul (forward) apuntando hacia el fondo de la pista.
/// Déjalo en la raíz de la escena (sin padre escalado).
/// </summary>
public class PinSetter : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Prefab del pino: pivote en la base, con Rigidbody y colliders en la raíz")]
    [SerializeField] private GameObject pinPrefab;

    [Header("Formación (metros)")]
    [Tooltip("Distancia entre centros de pinos vecinos (reglamento: 30.48 cm)")]
    [SerializeField] private float pinSpacing = 0.3048f;

    [Header("Detección de pinos caídos")]
    [Tooltip("Inclinación en grados a partir de la cual un pino cuenta como caído")]
    [SerializeField] private float fallenAngle = 20f;
    [Tooltip("Si un pino baja más de esta distancia (cae al foso), cuenta como caído")]
    [SerializeField] private float fallenDrop = 0.1f;

    [Header("Reposo")]
    [Tooltip("Por debajo de esta velocidad (m/s) un pino se considera quieto")]
    [SerializeField] private float settleLinearSpeed = 0.05f;
    [Tooltip("Por debajo de esta velocidad angular (rad/s) un pino se considera quieto")]
    [SerializeField] private float settleAngularSpeed = 0.1f;

    private readonly List<Rigidbody> pins = new List<Rigidbody>();
    private readonly List<Pose> startPoses = new List<Pose>();

    public int TotalPins => pins.Count;

    private void Awake()
    {
        if (pinPrefab == null)
        {
            Debug.LogError("PinSetter: falta asignar el prefab del pino.", this);
            return;
        }
        SpawnPins();
    }

    /// <summary>Crea los pinos numerados 1 a 10, de adelante hacia atrás y de izquierda a derecha.</summary>
    private void SpawnPins()
    {
        float rowDepth = pinSpacing * Mathf.Sqrt(3f) / 2f; // ~26.4 cm entre filas

        for (int row = 0; row < 4; row++)
        {
            for (int i = 0; i <= row; i++)
            {
                float x = (i - row / 2f) * pinSpacing;
                float z = row * rowDepth;
                Vector3 position = transform.position + transform.rotation * new Vector3(x, 0f, z);
                Quaternion rotation = transform.rotation;

                GameObject pin = Instantiate(pinPrefab, position, rotation, transform);
                pin.name = $"Pino {pins.Count + 1}";

                Rigidbody rb = pin.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogError("PinSetter: el prefab del pino necesita un Rigidbody en la raíz.", pin);
                    continue;
                }

                pins.Add(rb);
                startPoses.Add(new Pose(position, rotation));
            }
        }
    }

    /// <summary>Un pino cuenta como caído si fue retirado, está inclinado o cayó al foso.</summary>
    public bool IsFallen(int index)
    {
        Rigidbody rb = pins[index];
        if (!rb.gameObject.activeSelf) return true;

        float tilt = Vector3.Angle(rb.transform.up, Vector3.up);
        float drop = startPoses[index].position.y - rb.position.y;
        return tilt > fallenAngle || drop > fallenDrop;
    }

    /// <summary>
    /// Total de pinos caídos desde el último ResetAll (incluye los ya retirados).
    /// Para el segundo tiro de un frame: caídos ahora menos caídos tras el primer tiro.
    /// </summary>
    public int CountFallen()
    {
        int count = 0;
        for (int i = 0; i < pins.Count; i++)
        {
            if (IsFallen(i)) count++;
        }
        return count;
    }

    public int CountStanding() => pins.Count - CountFallen();

    /// <summary>True cuando ningún pino activo se está moviendo.</summary>
    public bool AreSettled()
    {
        foreach (Rigidbody rb in pins)
        {
            if (!rb.gameObject.activeSelf || rb.IsSleeping()) continue;

            if (rb.linearVelocity.magnitude > settleLinearSpeed ||
                rb.angularVelocity.magnitude > settleAngularSpeed)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Barredor después del primer tiro: retira los caídos y deja los que siguen de pie.</summary>
    public void RemoveFallen()
    {
        for (int i = 0; i < pins.Count; i++)
        {
            if (IsFallen(i)) pins[i].gameObject.SetActive(false);
        }
    }

    /// <summary>Vuelve a colocar los 10 pinos en su posición inicial, quietos.</summary>
    public void ResetAll()
    {
        for (int i = 0; i < pins.Count; i++)
        {
            Rigidbody rb = pins[i];
            rb.gameObject.SetActive(true);

            rb.isKinematic = true;
            rb.transform.SetPositionAndRotation(startPoses[i].position, startPoses[i].rotation);
            rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
        Physics.SyncTransforms();
    }
}
