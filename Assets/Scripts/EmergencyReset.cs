using System.Collections;
using UnityEngine;

/// <summary>
/// Reinicio de emergencia: mantener presionado B (control derecho) o Y (control izquierdo)
/// durante unos segundos vuelve a la selección de rondas.
/// Útil si algo falla durante una presentación. El control vibra suave mientras se mantiene
/// el botón y fuerte al reiniciar.
/// </summary>
public class EmergencyReset : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [Tooltip("Segundos que hay que mantener presionado B o Y")]
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float holdHapticAmplitude = 0.15f;
    [SerializeField] private float resetHapticAmplitude = 0.8f;

    private float heldTime;
    private bool waitForRelease;
    private OVRInput.Controller vibratingController = OVRInput.Controller.None;

    private void Awake()
    {
        if (gameManager == null)
        {
            Debug.LogError("EmergencyReset: falta asignar el GameManager.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        bool right = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch); // B
        bool left = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);  // Y

        if (!right && !left)
        {
            heldTime = 0f;
            waitForRelease = false;
            StopHoldVibration();
            return;
        }

        // Después de reiniciar hay que soltar el botón antes de poder reiniciar otra vez.
        if (waitForRelease) return;

        // En la selección de rondas no hay nada que reiniciar.
        if (gameManager.State == GameManager.GameState.SelectingRounds)
        {
            heldTime = 0f;
            StopHoldVibration();
            return;
        }

        OVRInput.Controller controller = right ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
        heldTime += Time.deltaTime;

        vibratingController = controller;
        OVRInput.SetControllerVibration(1f, holdHapticAmplitude, controller);

        if (heldTime >= holdTime)
        {
            heldTime = 0f;
            waitForRelease = true;
            vibratingController = OVRInput.Controller.None;
            gameManager.Retry();
            StartCoroutine(StrongPulse(controller));
        }
    }

    private void StopHoldVibration()
    {
        if (vibratingController == OVRInput.Controller.None) return;
        OVRInput.SetControllerVibration(0f, 0f, vibratingController);
        vibratingController = OVRInput.Controller.None;
    }

    private IEnumerator StrongPulse(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1f, resetHapticAmplitude, controller);
        yield return new WaitForSeconds(0.2f);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}
