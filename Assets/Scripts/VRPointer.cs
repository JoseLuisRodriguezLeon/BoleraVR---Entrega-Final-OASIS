using System.Collections;
using UnityEngine;

/// <summary>
/// Rayo desde cada control para usar los menús en VR: apunta a un botón y aprieta el gatillo.
/// Solo está activo mientras se eligen las rondas o en la pantalla final,
/// así no interfiere con el lanzamiento de la bola.
/// </summary>
public class VRPointer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [Tooltip("LeftControllerAnchor (dentro del Camera Rig)")]
    [SerializeField] private Transform leftController;
    [Tooltip("RightControllerAnchor (dentro del Camera Rig)")]
    [SerializeField] private Transform rightController;
    [Tooltip("Material del rayo (shader Universal Render Pipeline/Unlit). Sin material se ve rosado")]
    [SerializeField] private Material rayMaterial;

    [Header("Rayo")]
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float rayWidth = 0.005f;
    [SerializeField] private Color rayColor = Color.white;
    [SerializeField] private Color rayHoverColor = new Color(0.3f, 0.8f, 1f, 1f);

    [Header("Gatillo (histéresis)")]
    [Range(0f, 1f)] [SerializeField] private float pressThreshold = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float releaseThreshold = 0.35f;

    [Header("Vibración al hacer clic")]
    [SerializeField] private float clickHapticAmplitude = 0.4f;
    [SerializeField] private float clickHapticDuration = 0.05f;

    private class Pointer
    {
        public Transform Anchor;
        public OVRInput.Controller Controller;
        public LineRenderer Line;
        public Material LineMaterial;
        public bool TriggerDown;
        public VRButton Hovered;
    }

    private Pointer left;
    private Pointer right;

    private void Awake()
    {
        if (gameManager == null || leftController == null || rightController == null)
        {
            Debug.LogError("VRPointer: faltan referencias en el Inspector.", this);
            enabled = false;
            return;
        }

        if (rayMaterial == null)
        {
            Debug.LogWarning("VRPointer: falta el material del rayo; se verá rosado.", this);
        }

        left = CreatePointer("Rayo izquierdo", leftController, OVRInput.Controller.LTouch);
        right = CreatePointer("Rayo derecho", rightController, OVRInput.Controller.RTouch);
    }

    private Pointer CreatePointer(string name, Transform anchor, OVRInput.Controller controller)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.widthMultiplier = rayWidth;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;

        Material material = null;
        if (rayMaterial != null)
        {
            material = new Material(rayMaterial);
            line.sharedMaterial = material;
        }

        return new Pointer
        {
            Anchor = anchor,
            Controller = controller,
            Line = line,
            LineMaterial = material
        };
    }

    private void LateUpdate()
    {
        bool menuActive = gameManager.State == GameManager.GameState.SelectingRounds ||
                          gameManager.State == GameManager.GameState.GameOver;

        UpdatePointer(left, menuActive);
        UpdatePointer(right, menuActive);
    }

    private void UpdatePointer(Pointer p, bool menuActive)
    {
        bool justPressed = ReadTrigger(p);

        if (!menuActive)
        {
            SetHovered(p, null);
            p.Line.enabled = false;
            return;
        }

        Vector3 origin = p.Anchor.position;
        Vector3 direction = p.Anchor.forward;
        float length = maxDistance;
        VRButton target = null;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            length = hit.distance;
            target = hit.collider.GetComponent<VRButton>();
        }

        SetHovered(p, target);

        p.Line.enabled = true;
        p.Line.SetPosition(0, origin);
        p.Line.SetPosition(1, origin + direction * length);
        if (p.LineMaterial != null)
        {
            p.LineMaterial.color = target != null ? rayHoverColor : rayColor;
        }

        if (justPressed && target != null)
        {
            target.Click();
            StartCoroutine(HapticPulse(p.Controller));
        }
    }

    /// <summary>Devuelve true solo en el frame en que el gatillo se presiona.</summary>
    private bool ReadTrigger(Pointer p)
    {
        float value = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, p.Controller);

        if (!p.TriggerDown && value > pressThreshold)
        {
            p.TriggerDown = true;
            return true;
        }

        if (p.TriggerDown && value < releaseThreshold)
        {
            p.TriggerDown = false;
        }
        return false;
    }

    private void SetHovered(Pointer p, VRButton button)
    {
        if (p.Hovered == button) return;

        Pointer other = p == left ? right : left;
        if (p.Hovered != null && other.Hovered != p.Hovered)
        {
            p.Hovered.SetHover(false);
        }

        p.Hovered = button;
        if (button != null)
        {
            button.SetHover(true);
        }
    }

    private IEnumerator HapticPulse(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1f, clickHapticAmplitude, controller);
        yield return new WaitForSeconds(clickHapticDuration);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }

    private void OnDestroy()
    {
        if (left?.LineMaterial != null) Destroy(left.LineMaterial);
        if (right?.LineMaterial != null) Destroy(right.LineMaterial);
    }
}
