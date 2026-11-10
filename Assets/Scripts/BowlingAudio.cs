using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de sonido de la bolera. Se conecta solo a la bola, a los pinos y a los botones:
/// - Bola rodando (en bucle; el volumen y el tono suben con la velocidad).
/// - Golpe de la bola contra los pinos y choques de los pinos (volumen según la fuerza).
/// - Strike, spare y fin de partida.
/// - Clic de los botones de los menús.
/// - Ambiente de fondo (opcional).
/// Cualquier sonido que dejes vacío simplemente no suena.
/// </summary>
public class BowlingAudio : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BallLauncher ball;
    [SerializeField] private PinSetter pinSetter;

    [Header("Bola rodando (sonido en bucle)")]
    [SerializeField] private AudioClip rollLoop;
    [Range(0f, 1f)] [SerializeField] private float rollMaxVolume = 0.8f;
    [Tooltip("Velocidad (m/s) a la que el rodado suena al máximo")]
    [SerializeField] private float rollFullSpeed = 8f;
    [Tooltip("Distancia desde el centro de la bola hasta el piso para considerar que está rodando")]
    [SerializeField] private float groundCheckDistance = 0.16f;

    [Header("Golpes (se elige uno al azar de cada lista)")]
    [Tooltip("La bola golpeando los pinos")]
    [SerializeField] private AudioClip[] ballHitsPins;
    [Tooltip("Pinos chocando entre sí, contra el piso o contra las paredes")]
    [SerializeField] private AudioClip[] pinHits;
    [Tooltip("Velocidad de impacto (m/s) que suena al volumen máximo")]
    [SerializeField] private float hitFullVelocity = 4f;
    [Tooltip("Los impactos más suaves que esto no suenan")]
    [SerializeField] private float hitMinVelocity = 0.4f;
    [Tooltip("Cuántos golpes pueden sonar al mismo tiempo")]
    [SerializeField] private int maxSimultaneousHits = 8;

    [Header("Celebraciones y fin")]
    [SerializeField] private AudioClip strikeClip;
    [SerializeField] private AudioClip spareClip;
    [SerializeField] private AudioClip gameOverClip;

    [Header("Menús")]
    [SerializeField] private AudioClip uiClick;

    [Header("Ambiente (opcional, en bucle)")]
    [SerializeField] private AudioClip ambienceLoop;
    [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 0.25f;

    private AudioSource rollSource;
    private AudioSource uiSource;
    private readonly List<AudioSource> hitSources = new List<AudioSource>();
    private readonly List<VRButton> buttons = new List<VRButton>();
    private int nextHitSource;
    private float lastBallHitTime = -1f;
    private int lastRollCount;

    private void Start()
    {
        if (gameManager == null || ball == null || pinSetter == null)
        {
            Debug.LogError("BowlingAudio: faltan referencias (Game Manager, Ball o Pin Setter).", this);
            enabled = false;
            return;
        }

        // Sonidos sin posición (menús, celebraciones y ambiente).
        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f;

        if (ambienceLoop != null)
        {
            AudioSource ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = ambienceLoop;
            ambience.loop = true;
            ambience.volume = ambienceVolume;
            ambience.spatialBlend = 0f;
            ambience.Play();
        }

        // El rodado sale desde la bola.
        rollSource = ball.gameObject.AddComponent<AudioSource>();
        rollSource.clip = rollLoop;
        rollSource.loop = true;
        rollSource.playOnAwake = false;
        rollSource.spatialBlend = 1f;
        rollSource.volume = 0f;
        rollSource.minDistance = 1.5f;
        rollSource.maxDistance = 30f;

        // Varias fuentes 3D para que los golpes suenen donde ocurren.
        for (int i = 0; i < Mathf.Max(1, maxSimultaneousHits); i++)
        {
            var go = new GameObject($"Golpe {i + 1}");
            go.transform.SetParent(transform, false);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 1.5f;
            source.maxDistance = 30f;
            hitSources.Add(source);
        }

        // Avisos de choque en la bola y en cada pino.
        ball.gameObject.AddComponent<CollisionSoundRelay>().Init(this, true);
        foreach (Rigidbody pin in pinSetter.GetComponentsInChildren<Rigidbody>(true))
        {
            pin.gameObject.AddComponent<CollisionSoundRelay>().Init(this, false);
        }

        // Clic en todos los botones de los menús.
        foreach (VRButton button in FindObjectsByType<VRButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            button.Clicked += PlayClick;
            buttons.Add(button);
        }

        gameManager.ScoreChanged += OnScoreChanged;
        gameManager.StateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.ScoreChanged -= OnScoreChanged;
            gameManager.StateChanged -= OnStateChanged;
        }

        foreach (VRButton button in buttons)
        {
            if (button != null) button.Clicked -= PlayClick;
        }
    }

    private void Update()
    {
        if (rollSource == null || rollSource.clip == null) return;
        if (!ball.gameObject.activeInHierarchy) return;

        if (!rollSource.isPlaying) rollSource.Play();

        float speed01 = Mathf.Clamp01(ball.Speed / rollFullSpeed);
        bool rolling = ball.IsLaunched && IsBallOnGround();
        float targetVolume = rolling ? speed01 * rollMaxVolume : 0f;

        rollSource.volume = Mathf.MoveTowards(rollSource.volume, targetVolume, Time.deltaTime * 4f);
        rollSource.pitch = Mathf.Lerp(0.8f, 1.2f, speed01);
    }

    private bool IsBallOnGround()
    {
        return Physics.Raycast(ball.transform.position, Vector3.down, groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Lo llama CollisionSoundRelay cuando la bola o un pino chocan.</summary>
    public void HandleCollision(CollisionSoundRelay source, Collision collision)
    {
        float impact = collision.relativeVelocity.magnitude;
        if (impact < hitMinVelocity || collision.contactCount == 0) return;

        CollisionSoundRelay other = collision.collider.GetComponentInParent<CollisionSoundRelay>();
        Vector3 point = collision.GetContact(0).point;

        if (source.IsBall)
        {
            // La bola solo suena al golpear pinos; el rodado tiene su propio sonido.
            if (other == null || other.IsBall) return;
            if (Time.time - lastBallHitTime < 0.15f) return;

            lastBallHitTime = Time.time;
            PlayHit(Pick(ballHitsPins), point, impact);
        }
        else
        {
            // El choque bola-pino ya lo hace sonar la bola.
            if (other != null && other.IsBall) return;

            // Un choque entre dos pinos llega a ambos: solo suena una vez.
            if (other != null && other.GetInstanceID() < source.GetInstanceID()) return;

            PlayHit(Pick(pinHits), point, impact);
        }
    }

    private void PlayHit(AudioClip clip, Vector3 position, float impact)
    {
        if (clip == null || hitSources.Count == 0) return;

        AudioSource source = hitSources[nextHitSource];
        nextHitSource = (nextHitSource + 1) % hitSources.Count;

        source.transform.position = position;
        source.pitch = Random.Range(0.92f, 1.08f);
        source.volume = Mathf.Clamp01(impact / hitFullVelocity);
        source.clip = clip;
        source.Play();
    }

    private void OnScoreChanged()
    {
        BowlingScore score = gameManager.Score;
        if (score == null)
        {
            lastRollCount = 0;
            return;
        }

        int rolls = 0;
        for (int i = 0; i < score.TotalFrames; i++)
        {
            rolls += score.GetRolls(i).Count;
        }

        if (rolls > lastRollCount)
        {
            int frame = score.CurrentFrameIndex;
            if (score.GetRolls(frame).Count == 0 && frame > 0) frame--;

            string[] marks = score.GetFrameMarks(frame);
            if (marks.Length > 0)
            {
                string last = marks[marks.Length - 1];
                if (last == "X") Play2D(strikeClip);
                else if (last == "/") Play2D(spareClip);
            }
        }

        lastRollCount = rolls;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            Play2D(gameOverClip);
        }
    }

    private void PlayClick()
    {
        Play2D(uiClick);
    }

    private void Play2D(AudioClip clip)
    {
        if (clip != null && uiSource != null)
        {
            uiSource.PlayOneShot(clip);
        }
    }

    private static AudioClip Pick(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
