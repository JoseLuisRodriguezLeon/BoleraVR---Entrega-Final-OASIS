using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controles de prueba con teclado, para jugar partidas completas sin casco.
/// Usa el GameManager: elegir rondas, lanzar, reintentar y salir.
/// Las teclas se pueden cambiar en el Inspector si chocan con el Meta XR Simulator.
/// Solo funciona dentro del editor: en el APK se desactiva solo.
/// </summary>
public class DebugThrowInput : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [Tooltip("El punto de salida de la bola (SalidaBola); su eje Z apunta hacia los pinos")]
    [SerializeField] private Transform laneDirection;

    [Header("Tiro")]
    [SerializeField] private float speed = 7f;
    [Tooltip("Desvío en grados respecto al centro de la pista (negativo = izquierda)")]
    [SerializeField] private float angle = 0f;
    [Tooltip("Efecto entre -1 y 1 (negativo = curva a la izquierda)")]
    [SerializeField] private float spin = 0f;

    [Header("Teclas de tiro")]
    [SerializeField] private Key throwKey = Key.B;
    [SerializeField] private Key speedUpKey = Key.I;
    [SerializeField] private Key speedDownKey = Key.K;
    [SerializeField] private Key angleLeftKey = Key.J;
    [SerializeField] private Key angleRightKey = Key.L;
    [SerializeField] private Key spinLeftKey = Key.U;
    [SerializeField] private Key spinRightKey = Key.O;

    [Header("Teclas de partida")]
    [Tooltip("Empieza con el último número de rondas elegido")]
    [SerializeField] private Key repeatLastKey = Key.Enter;
    [SerializeField] private Key retryKey = Key.R;
    [SerializeField] private Key exitKey = Key.Escape;

    // Teclas 1 a 9 = esas rondas; 0 = 10 rondas.
    private static readonly Key[] RoundKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };

    private void Awake()
    {
        // Solo para probar en el editor: en el casco no hay teclado.
        if (!Application.isEditor)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || gameManager == null || laneDirection == null) return;

        // Ajustes del tiro, disponibles siempre.
        if (kb[speedUpKey].wasPressedThisFrame) speed = Mathf.Min(speed + 0.5f, 12f);
        if (kb[speedDownKey].wasPressedThisFrame) speed = Mathf.Max(speed - 0.5f, 0.5f);
        if (kb[angleLeftKey].wasPressedThisFrame) angle -= 0.5f;
        if (kb[angleRightKey].wasPressedThisFrame) angle += 0.5f;
        if (kb[spinLeftKey].wasPressedThisFrame) spin = Mathf.Max(spin - 0.25f, -1f);
        if (kb[spinRightKey].wasPressedThisFrame) spin = Mathf.Min(spin + 0.25f, 1f);

        switch (gameManager.State)
        {
            case GameManager.GameState.SelectingRounds:
                for (int i = 0; i < RoundKeys.Length; i++)
                {
                    if (kb[RoundKeys[i]].wasPressedThisFrame)
                    {
                        gameManager.StartGame(i + 1);
                        return;
                    }
                }
                if (kb[repeatLastKey].wasPressedThisFrame)
                {
                    gameManager.StartGame(gameManager.LastChosenRounds);
                }
                break;

            case GameManager.GameState.WaitingThrow:
                if (kb[throwKey].wasPressedThisFrame)
                {
                    Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * laneDirection.forward;
                    gameManager.TryLaunch(direction * speed, spin);
                }
                break;

            case GameManager.GameState.GameOver:
                if (kb[exitKey].wasPressedThisFrame)
                {
                    gameManager.Exit();
                }
                break;
        }

        // Reintentar funciona en cualquier momento (útil para abortar una prueba).
        if (kb[retryKey].wasPressedThisFrame && gameManager.State != GameManager.GameState.SelectingRounds)
        {
            gameManager.Retry();
        }
    }

    private void OnGUI()
    {
        if (gameManager == null) return;

        var sb = new StringBuilder();

        switch (gameManager.State)
        {
            case GameManager.GameState.SelectingRounds:
                sb.AppendLine("ELIGE RONDAS: teclas 1 a 9, 0 = 10 rondas");
                sb.AppendLine($"{repeatLastKey} = repetir la última ({gameManager.LastChosenRounds})");
                break;

            case GameManager.GameState.GameOver:
                sb.AppendLine($"FIN DE LA PARTIDA — Puntaje: {gameManager.Score.TotalScore}");
                sb.AppendLine($"{retryKey} reintentar · {exitKey} salir");
                break;

            default:
                sb.AppendLine($"Velocidad: {speed:0.0} m/s ({speedUpKey}/{speedDownKey})");
                sb.AppendLine($"Ángulo: {angle:0.0}° ({angleLeftKey}/{angleRightKey})");
                sb.AppendLine($"Efecto: {spin:0.00} ({spinLeftKey}/{spinRightKey})");
                sb.AppendLine($"Estado: {gameManager.State} · {throwKey} lanzar · {retryKey} reiniciar");
                break;
        }

        if (gameManager.Score != null)
        {
            sb.AppendLine(BuildScoreboard(gameManager.Score));
        }

        GUI.Label(new Rect(10, 10, 800, 220), sb.ToString());
    }

    private static string BuildScoreboard(BowlingScore score)
    {
        var sb = new StringBuilder();
        int?[] totals = score.GetCumulativeScores();

        for (int i = 0; i < score.TotalFrames; i++)
        {
            string marks = string.Join(" ", score.GetFrameMarks(i));
            string total = totals[i].HasValue ? totals[i].Value.ToString() : "";
            sb.Append($"[{i + 1}: {marks} | {total}]  ");
        }

        return sb.ToString();
    }
}