using System;
using UnityEngine;

/// <summary>
/// Controla el flujo de la partida: elegir rondas, esperar el tiro, seguir la bola,
/// contar pinos, barrer o reiniciar los pinos, y terminar.
/// Los controles (teclado ahora, VR después) solo llaman a StartGame, TryLaunch, Retry y Exit.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        SelectingRounds,
        WaitingThrow,
        BallRolling,
        Counting,
        GameOver
    }

    [Header("Referencias")]
    [SerializeField] private BallLauncher ball;
    [SerializeField] private PinSetter pinSetter;
    [Tooltip("El punto de salida de la bola (SalidaBola); su eje Z apunta hacia los pinos")]
    [SerializeField] private Transform laneStart;

    [Header("Fin del tiro")]
    [Tooltip("Segundos máximos desde que se lanza la bola")]
    [SerializeField] private float maxThrowTime = 10f;
    [Tooltip("Si la bola va más lento que esto (m/s)...")]
    [SerializeField] private float stoppedSpeed = 0.05f;
    [Tooltip("...durante estos segundos, el tiro termina")]
    [SerializeField] private float stoppedTime = 1f;
    [Tooltip("El tiro termina cuando la bola pasa esta distancia (m) más allá del primer pino")]
    [SerializeField] private float distancePastPins = 1.5f;
    [Tooltip("El tiro termina si la bola cae más que esto (m) por debajo de la salida")]
    [SerializeField] private float fallDistance = 0.25f;

    [Header("Conteo de pinos")]
    [Tooltip("Espera mínima (s) para que los pinos terminen de caer")]
    [SerializeField] private float minCountWait = 1.5f;
    [Tooltip("Espera máxima (s) aunque algún pino siga moviéndose")]
    [SerializeField] private float maxCountWait = 4f;

    public GameState State { get; private set; } = GameState.SelectingRounds;
    public BowlingScore Score { get; private set; }
    public int LastChosenRounds { get; private set; } = 5;

    public event Action<GameState> StateChanged;
    public event Action ScoreChanged;

    private float timeInState;
    private float stoppedTimer;
    private int fallenBeforeThrow;

    private void Awake()
    {
        if (ball == null || pinSetter == null || laneStart == null)
        {
            Debug.LogError("GameManager: faltan referencias (Ball, Pin Setter o Lane Start).", this);
            enabled = false;
        }
    }

    private void Start()
    {
        EnterSelectingRounds();
    }

    // ---------- Acciones que llaman los controles ----------

    /// <summary>Empieza una partida con el número de rondas elegido (1 a 10).</summary>
    public void StartGame(int rounds)
    {
        if (State != GameState.SelectingRounds) return;

        LastChosenRounds = Mathf.Clamp(rounds, BowlingScore.MinFrames, BowlingScore.MaxFrames);
        Score = new BowlingScore(LastChosenRounds);
        fallenBeforeThrow = 0;

        pinSetter.ResetAll();
        ball.gameObject.SetActive(true);
        ball.ResetBall();

        ScoreChanged?.Invoke();
        ChangeState(GameState.WaitingThrow);
    }

    /// <summary>Lanza la bola solo si el juego está esperando un tiro.</summary>
    public bool TryLaunch(Vector3 velocity, float spin)
    {
        if (State != GameState.WaitingThrow) return false;

        ball.Launch(velocity, spin);
        if (!ball.IsLaunched) return false;

        ChangeState(GameState.BallRolling);
        return true;
    }

    /// <summary>Vuelve a la selección de rondas.</summary>
    public void Retry()
    {
        EnterSelectingRounds();
    }

    /// <summary>Cierra la aplicación (en el editor, detiene el modo Play).</summary>
    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Ciclo de la partida ----------

    private void Update()
    {
        timeInState += Time.deltaTime;

        switch (State)
        {
            case GameState.BallRolling:
                if (HasThrowEnded())
                {
                    ChangeState(GameState.Counting);
                }
                break;

            case GameState.Counting:
                if (timeInState < minCountWait) break;
                if (!pinSetter.AreSettled() && timeInState < maxCountWait) break;
                ResolveThrow();
                break;
        }
    }

    private bool HasThrowEnded()
    {
        if (timeInState > maxThrowTime) return true;

        Vector3 ballPos = ball.transform.position;

        // Cayó al foso o se salió de la pista.
        if (ballPos.y < laneStart.position.y - fallDistance) return true;

        // Pasó más allá de los pinos.
        float ballAlong = Vector3.Dot(ballPos - laneStart.position, laneStart.forward);
        float pinsAlong = Vector3.Dot(pinSetter.transform.position - laneStart.position, laneStart.forward);
        if (ballAlong > pinsAlong + distancePastPins) return true;

        // Se quedó quieta en la pista.
        stoppedTimer = ball.Speed < stoppedSpeed ? stoppedTimer + Time.deltaTime : 0f;
        return stoppedTimer > stoppedTime;
    }

    private void ResolveThrow()
    {
        int fallenNow = pinSetter.CountFallen();
        int knocked = Mathf.Max(0, fallenNow - fallenBeforeThrow);
        int frameBefore = Score.CurrentFrameIndex;

        Score.RecordRoll(knocked);
        ScoreChanged?.Invoke();

        ball.ResetBall();

        if (Score.IsGameOver)
        {
            ball.gameObject.SetActive(false);
            ChangeState(GameState.GameOver);
            return;
        }

        // Pinos nuevos si cayeron todos o empieza otra ronda; si no, solo se barren los caídos.
        bool newRack = pinSetter.CountStanding() == 0 || Score.CurrentFrameIndex != frameBefore;
        if (newRack)
        {
            pinSetter.ResetAll();
            fallenBeforeThrow = 0;
        }
        else
        {
            pinSetter.RemoveFallen();
            fallenBeforeThrow = fallenNow;
        }

        ChangeState(GameState.WaitingThrow);
    }

    private void EnterSelectingRounds()
    {
        Score = null;
        fallenBeforeThrow = 0;

        ball.ResetBall();
        ball.gameObject.SetActive(false);
        pinSetter.ResetAll();

        ScoreChanged?.Invoke();
        ChangeState(GameState.SelectingRounds);
    }

    private void ChangeState(GameState newState)
    {
        State = newState;
        timeInState = 0f;
        stoppedTimer = 0f;
        StateChanged?.Invoke(newState);
    }
}
