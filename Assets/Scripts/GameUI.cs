using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea por código los paneles en VR y el marcador en el mundo, y los conecta al GameManager:
/// - Panel de selección de rondas (- / +, 1, 5, 10 y Jugar).
/// - Pantalla final (puntaje, Reintentar y Salir con confirmación).
/// - Marcador sobre la pista con cada ronda, el total y los avisos de strike y spare.
/// No hay que armar nada a mano: solo asignar las tres referencias.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [Tooltip("CenterEyeAnchor del Camera Rig: los paneles aparecen frente a la cabeza")]
    [SerializeField] private Transform head;
    [Tooltip("Objeto vacío donde va el marcador (sobre la pista, con su eje Z hacia los pinos)")]
    [SerializeField] private Transform scoreboardAnchor;

    [Header("Paneles")]
    [Tooltip("Distancia del panel frente a la cabeza (m)")]
    [SerializeField] private float panelDistance = 1.2f;
    [Tooltip("Altura del panel respecto a los ojos (m); negativo = un poco más abajo")]
    [SerializeField] private float panelHeightOffset = -0.15f;
    [Tooltip("Altura mínima del panel sobre el piso del Camera Rig (m)")]
    [SerializeField] private float minPanelHeight = 0.9f;

    [Header("Colores")]
    [SerializeField] private Color panelColor = new Color(0.05f, 0.07f, 0.12f, 0.92f);
    [SerializeField] private Color buttonColor = new Color(0.16f, 0.22f, 0.36f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.30f, 0.55f, 0.95f, 1f);
    [SerializeField] private Color playColor = new Color(0.12f, 0.55f, 0.28f, 1f);
    [SerializeField] private Color playHoverColor = new Color(0.20f, 0.80f, 0.40f, 1f);
    [SerializeField] private Color exitColor = new Color(0.55f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color exitHoverColor = new Color(0.85f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color cellColor = new Color(0.12f, 0.15f, 0.24f, 1f);
    [SerializeField] private Color currentCellColor = new Color(0.22f, 0.35f, 0.60f, 1f);
    [SerializeField] private Color totalCellColor = new Color(0.10f, 0.30f, 0.20f, 1f);
    [SerializeField] private Color labelColor = new Color(0.70f, 0.75f, 0.85f, 1f);
    [SerializeField] private Color celebrationColor = new Color(1f, 0.85f, 0.2f, 1f);

    // 1000 píxeles de interfaz = 1 metro.
    private const float PixelSize = 0.001f;

    private class Cell
    {
        public Image Background;
        public TMP_Text Marks;
        public TMP_Text Total;
    }

    private Transform selectionPanel;
    private Transform gameOverPanel;
    private TMP_Text roundsText;
    private TMP_Text finalScoreText;
    private TMP_Text finalDetailText;
    private TMP_Text exitLabel;

    private Transform scoreboardRoot;
    private Image scoreboardBackground;
    private TMP_Text statusText;
    private TMP_Text celebrationText;
    private TMP_Text totalText;
    private GameObject totalCell;
    private readonly List<Cell> cells = new List<Cell>();

    private int selectedRounds = 5;
    private int lastRollCount;
    private float exitConfirmUntil = -1f;
    private Coroutine celebrationRoutine;
    private Coroutine exitResetRoutine;

    // ---------- Inicio ----------

    private void Awake()
    {
        if (gameManager == null || head == null || scoreboardAnchor == null)
        {
            Debug.LogError("GameUI: faltan referencias (Game Manager, Head o Scoreboard Anchor).", this);
            enabled = false;
            return;
        }

        BuildSelectionPanel();
        BuildGameOverPanel();
        BuildScoreboard();
    }

    private void OnEnable()
    {
        if (gameManager == null) return;
        gameManager.StateChanged += OnStateChanged;
        gameManager.ScoreChanged += OnScoreChanged;
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.StateChanged -= OnStateChanged;
        gameManager.ScoreChanged -= OnScoreChanged;
    }

    // ---------- Reacciones al juego ----------

    private void OnStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.SelectingRounds:
                selectedRounds = gameManager.LastChosenRounds;
                UpdateRoundsText();
                gameOverPanel.gameObject.SetActive(false);
                scoreboardRoot.gameObject.SetActive(false);
                StartCoroutine(ShowInFront(selectionPanel, state));
                break;

            case GameManager.GameState.GameOver:
                selectionPanel.gameObject.SetActive(false);
                finalScoreText.text = gameManager.Score.TotalScore.ToString();
                int rounds = gameManager.Score.TotalFrames;
                finalDetailText.text = rounds == 1 ? "1 ronda" : $"{rounds} rondas";
                ResetExitButton();
                StartCoroutine(ShowInFront(gameOverPanel, state));
                break;

            default:
                selectionPanel.gameObject.SetActive(false);
                gameOverPanel.gameObject.SetActive(false);
                break;
        }
    }

    private void OnScoreChanged()
    {
        BowlingScore score = gameManager.Score;
        if (score == null)
        {
            lastRollCount = 0;
            return;
        }

        if (cells.Count != score.TotalFrames)
        {
            RebuildCells(score.TotalFrames);
        }

        scoreboardRoot.gameObject.SetActive(true);
        RefreshScoreboard(score);

        int rolls = CountRolls(score);
        if (rolls > lastRollCount)
        {
            CheckCelebration(score);
        }
        lastRollCount = rolls;
    }

    // ---------- Acciones de los botones ----------

    private void ChangeRounds(int delta)
    {
        selectedRounds = Mathf.Clamp(selectedRounds + delta, BowlingScore.MinFrames, BowlingScore.MaxFrames);
        UpdateRoundsText();
    }

    private void SetRounds(int rounds)
    {
        selectedRounds = Mathf.Clamp(rounds, BowlingScore.MinFrames, BowlingScore.MaxFrames);
        UpdateRoundsText();
    }

    private void UpdateRoundsText()
    {
        roundsText.text = selectedRounds.ToString();
    }

    private void OnPlayClicked()
    {
        gameManager.StartGame(selectedRounds);
    }

    private void OnRetryClicked()
    {
        gameManager.Retry();
    }

    private void OnExitClicked()
    {
        // Primer toque pide confirmación; el segundo (dentro de 3 s) sale.
        if (Time.time <= exitConfirmUntil)
        {
            gameManager.Exit();
            return;
        }

        exitConfirmUntil = Time.time + 3f;
        exitLabel.text = "¿Seguro?\nToca otra vez";
        if (exitResetRoutine != null) StopCoroutine(exitResetRoutine);
        exitResetRoutine = StartCoroutine(ResetExitLater());
    }

    private IEnumerator ResetExitLater()
    {
        yield return new WaitForSeconds(3f);
        ResetExitButton();
    }

    private void ResetExitButton()
    {
        exitConfirmUntil = -1f;
        if (exitLabel != null) exitLabel.text = "Salir";
    }

    // ---------- Colocar paneles frente al jugador ----------

    private IEnumerator ShowInFront(Transform panel, GameManager.GameState forState)
    {
        // Al iniciar, el casco tarda un momento en reportar la posición de la cabeza.
        while (Time.timeSinceLevelLoad < 0.5f) yield return null;
        yield return null;

        if (gameManager.State != forState) yield break;

        Vector3 forward = head.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = head.position + forward * panelDistance;
        float floorY = head.root.position.y;
        position.y = Mathf.Max(head.position.y + panelHeightOffset, floorY + minPanelHeight);

        panel.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        panel.gameObject.SetActive(true);
    }

    // ---------- Marcador ----------

    private void RefreshScoreboard(BowlingScore score)
    {
        int?[] totals = score.GetCumulativeScores();

        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].Marks.text = string.Join(" ", score.GetFrameMarks(i));
            cells[i].Total.text = totals[i].HasValue ? totals[i].Value.ToString() : "";
            bool isCurrent = !score.IsGameOver && i == score.CurrentFrameIndex;
            cells[i].Background.color = isCurrent ? currentCellColor : cellColor;
        }

        totalText.text = score.TotalScore.ToString();
        statusText.text = score.IsGameOver
            ? "Partida terminada"
            : $"Ronda {score.CurrentFrameIndex + 1} de {score.TotalFrames}";
    }

    private static int CountRolls(BowlingScore score)
    {
        int count = 0;
        for (int i = 0; i < score.TotalFrames; i++)
        {
            count += score.GetRolls(i).Count;
        }
        return count;
    }

    private void CheckCelebration(BowlingScore score)
    {
        int frame = score.CurrentFrameIndex;
        if (score.GetRolls(frame).Count == 0 && frame > 0) frame--;

        string[] marks = score.GetFrameMarks(frame);
        if (marks.Length == 0) return;

        string last = marks[marks.Length - 1];
        if (last == "X") Celebrate("¡STRIKE!");
        else if (last == "/") Celebrate("¡SPARE!");
    }

    private void Celebrate(string message)
    {
        if (celebrationRoutine != null) StopCoroutine(celebrationRoutine);
        celebrationRoutine = StartCoroutine(CelebrationRoutine(message));
    }

    private IEnumerator CelebrationRoutine(string message)
    {
        celebrationText.text = message;
        celebrationText.gameObject.SetActive(true);

        // Pequeño "salto" de tamaño para que se note.
        Transform t = celebrationText.transform;
        float duration = 0.25f;
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float s = Mathf.Lerp(1.6f, 1f, time / duration);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;

        yield return new WaitForSeconds(1.8f);
        celebrationText.gameObject.SetActive(false);
    }

    // ---------- Construcción de la interfaz ----------

    private void BuildSelectionPanel()
    {
        selectionPanel = CreatePanel("Panel Rondas", new Vector2(820f, 600f));

        CreateText(selectionPanel, "¿Cuántas rondas?", 56f, new Vector2(0f, 225f), new Vector2(760f, 80f));

        CreateButton(selectionPanel, "-", new Vector2(-240f, 95f), new Vector2(150f, 130f),
            buttonColor, buttonHoverColor, () => ChangeRounds(-1), out _);
        roundsText = CreateText(selectionPanel, "5", 120f, new Vector2(0f, 95f), new Vector2(260f, 150f));
        CreateButton(selectionPanel, "+", new Vector2(240f, 95f), new Vector2(150f, 130f),
            buttonColor, buttonHoverColor, () => ChangeRounds(1), out _);

        CreateButton(selectionPanel, "1", new Vector2(-240f, -60f), new Vector2(190f, 100f),
            buttonColor, buttonHoverColor, () => SetRounds(1), out _);
        CreateButton(selectionPanel, "5", new Vector2(0f, -60f), new Vector2(190f, 100f),
            buttonColor, buttonHoverColor, () => SetRounds(5), out _);
        CreateButton(selectionPanel, "10", new Vector2(240f, -60f), new Vector2(190f, 100f),
            buttonColor, buttonHoverColor, () => SetRounds(10), out _);

        CreateButton(selectionPanel, "JUGAR", new Vector2(0f, -205f), new Vector2(460f, 120f),
            playColor, playHoverColor, OnPlayClicked, out _);
    }

    private void BuildGameOverPanel()
    {
        gameOverPanel = CreatePanel("Panel Final", new Vector2(820f, 600f));

        CreateText(gameOverPanel, "¡Fin de la partida!", 56f, new Vector2(0f, 220f), new Vector2(760f, 80f));
        CreateText(gameOverPanel, "Puntaje", 40f, new Vector2(0f, 135f), new Vector2(400f, 60f), labelColor);
        finalScoreText = CreateText(gameOverPanel, "0", 130f, new Vector2(0f, 35f), new Vector2(500f, 160f));
        finalDetailText = CreateText(gameOverPanel, "", 36f, new Vector2(0f, -65f), new Vector2(500f, 50f), labelColor);

        CreateButton(gameOverPanel, "Reintentar", new Vector2(-210f, -200f), new Vector2(340f, 120f),
            buttonColor, buttonHoverColor, OnRetryClicked, out _);
        CreateButton(gameOverPanel, "Salir", new Vector2(210f, -200f), new Vector2(340f, 120f),
            exitColor, exitHoverColor, OnExitClicked, out exitLabel);
    }

    private void BuildScoreboard()
    {
        var go = new GameObject("Marcador (UI)", typeof(RectTransform));
        go.transform.SetParent(scoreboardAnchor, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * PixelSize;

        scoreboardRoot = go.transform;
        scoreboardBackground = CreateImage(scoreboardRoot, "Fondo", new Vector2(800f, 300f), panelColor, Vector2.zero);
        statusText = CreateText(scoreboardRoot, "", 44f, Vector2.zero, new Vector2(700f, 70f));
        celebrationText = CreateText(scoreboardRoot, "", 110f, Vector2.zero, new Vector2(1400f, 150f), celebrationColor);
        celebrationText.gameObject.SetActive(false);

        go.SetActive(false);
    }

    private void RebuildCells(int frames)
    {
        foreach (Cell cell in cells)
        {
            Destroy(cell.Background.gameObject);
        }
        cells.Clear();
        if (totalCell != null) Destroy(totalCell);

        const float cellWidth = 130f;
        const float cellHeight = 170f;
        const float gap = 10f;
        const float totalWidth = 200f;
        const float padding = 30f;
        const float headerHeight = 70f;

        float width = frames * (cellWidth + gap) + totalWidth + padding * 2f;
        float height = cellHeight + headerHeight + padding * 2f;

        scoreboardBackground.rectTransform.sizeDelta = new Vector2(width, height);
        statusText.rectTransform.sizeDelta = new Vector2(width - padding * 2f, headerHeight);
        statusText.rectTransform.anchoredPosition = new Vector2(0f, height / 2f - padding - headerHeight / 2f);
        celebrationText.rectTransform.anchoredPosition = new Vector2(0f, height / 2f + 90f);

        float x = -width / 2f + padding + cellWidth / 2f;
        float y = -height / 2f + padding + cellHeight / 2f;

        for (int i = 0; i < frames; i++)
        {
            Image background = CreateImage(scoreboardRoot, $"Ronda {i + 1}", new Vector2(cellWidth, cellHeight), cellColor, new Vector2(x, y));
            CreateText(background.transform, (i + 1).ToString(), 30f, new Vector2(0f, 58f), new Vector2(cellWidth, 40f), labelColor);
            TMP_Text marks = CreateText(background.transform, "", 42f, new Vector2(0f, 8f), new Vector2(cellWidth - 10f, 56f));
            TMP_Text total = CreateText(background.transform, "", 44f, new Vector2(0f, -50f), new Vector2(cellWidth - 10f, 56f));

            cells.Add(new Cell { Background = background, Marks = marks, Total = total });
            x += cellWidth + gap;
        }

        float totalX = x - cellWidth / 2f + totalWidth / 2f;
        Image totalBackground = CreateImage(scoreboardRoot, "Total", new Vector2(totalWidth, cellHeight), totalCellColor, new Vector2(totalX, y));
        CreateText(totalBackground.transform, "TOTAL", 30f, new Vector2(0f, 58f), new Vector2(totalWidth, 40f), labelColor);
        totalText = CreateText(totalBackground.transform, "0", 64f, new Vector2(0f, -20f), new Vector2(totalWidth - 10f, 90f));
        totalCell = totalBackground.gameObject;
    }

    // ---------- Ayudantes para crear elementos ----------

    private Transform CreatePanel(string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        ((RectTransform)go.transform).sizeDelta = size;
        go.transform.localScale = Vector3.one * PixelSize;

        CreateImage(go.transform, "Fondo", size, panelColor, Vector2.zero);
        go.SetActive(false);
        return go.transform;
    }

    private Image CreateImage(Transform parent, string name, Vector2 size, Color color, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateText(Transform parent, string text, float fontSize, Vector2 position, Vector2 size, Color? color = null)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color ?? Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = fontSize * 0.4f;
        label.fontSize = fontSize;
        label.raycastTarget = false;
        return label;
    }

    private VRButton CreateButton(Transform parent, string label, Vector2 position, Vector2 size,
        Color normal, Color hover, Action onClick, out TMP_Text labelText)
    {
        Image image = CreateImage(parent, "Boton " + label, size, normal, position);
        GameObject go = image.gameObject;

        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(size.x, size.y, 30f);

        var button = go.AddComponent<VRButton>();
        button.Setup(image, normal, hover);
        button.Clicked += onClick;

        labelText = CreateText(go.transform, label, Mathf.Min(56f, size.y * 0.5f), Vector2.zero, size * 0.9f);
        return button;
    }
}
