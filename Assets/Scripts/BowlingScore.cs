using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lógica de puntaje de bolos. No depende de la escena: se puede probar sola.
/// El número de rondas (frames) es configurable de 1 a 10.
/// La última ronda siempre usa las reglas del décimo frame:
/// un strike da dos tiros extra y un spare da uno.
/// </summary>
public class BowlingScore
{
    public const int PinsPerRack = 10;
    public const int MinFrames = 1;
    public const int MaxFrames = 10;

    private readonly List<int>[] frames;

    public int TotalFrames { get; }
    public int CurrentFrameIndex { get; private set; }
    public bool IsGameOver { get; private set; }

    public BowlingScore(int totalFrames)
    {
        TotalFrames = Mathf.Clamp(totalFrames, MinFrames, MaxFrames);
        frames = new List<int>[TotalFrames];
        for (int i = 0; i < TotalFrames; i++)
        {
            frames[i] = new List<int>();
        }
    }

    public bool IsLastFrame(int frameIndex) => frameIndex == TotalFrames - 1;

    /// <summary>Tiros registrados en una ronda.</summary>
    public IReadOnlyList<int> GetRolls(int frameIndex) => frames[frameIndex];

    /// <summary>Pinos que deberían estar de pie antes del próximo tiro.</summary>
    public int PinsStanding
    {
        get
        {
            if (IsGameOver) return 0;

            List<int> f = frames[CurrentFrameIndex];
            if (f.Count == 0) return PinsPerRack;

            if (!IsLastFrame(CurrentFrameIndex))
            {
                return PinsPerRack - f[0];
            }

            // Última ronda: los pinos se reinician después de un strike o un spare.
            if (f.Count == 1)
            {
                return f[0] == PinsPerRack ? PinsPerRack : PinsPerRack - f[0];
            }

            if (f[0] == PinsPerRack)
            {
                return f[1] == PinsPerRack ? PinsPerRack : PinsPerRack - f[1];
            }

            return PinsPerRack; // spare en los dos primeros tiros
        }
    }

    /// <summary>
    /// Registra un tiro. Devuelve false si la partida ya terminó.
    /// Si el número de pinos no es posible, se ajusta al rango válido.
    /// </summary>
    public bool RecordRoll(int pinsKnocked)
    {
        if (IsGameOver) return false;

        int standing = PinsStanding;
        if (pinsKnocked < 0 || pinsKnocked > standing)
        {
            Debug.LogWarning($"BowlingScore: tiro de {pinsKnocked} pinos ajustado (había {standing} de pie).");
            pinsKnocked = Mathf.Clamp(pinsKnocked, 0, standing);
        }

        List<int> f = frames[CurrentFrameIndex];
        f.Add(pinsKnocked);

        if (!IsLastFrame(CurrentFrameIndex))
        {
            bool strike = f.Count == 1 && pinsKnocked == PinsPerRack;
            if (strike || f.Count == 2)
            {
                CurrentFrameIndex++;
            }
        }
        else
        {
            bool noBonus = f.Count == 2 && f[0] + f[1] < PinsPerRack;
            if (f.Count == 3 || noBonus)
            {
                IsGameOver = true;
            }
        }

        return true;
    }

    /// <summary>
    /// Puntaje acumulado de cada ronda. Una ronda queda en null mientras
    /// falten tiros para calcular su bono.
    /// </summary>
    public int?[] GetCumulativeScores()
    {
        var result = new int?[TotalFrames];
        var allRolls = new List<int>();
        foreach (List<int> f in frames)
        {
            allRolls.AddRange(f);
        }

        int rollIndex = 0;
        int total = 0;

        for (int i = 0; i < TotalFrames; i++)
        {
            List<int> f = frames[i];

            if (IsLastFrame(i))
            {
                if (IsGameOver)
                {
                    foreach (int pins in f) total += pins;
                    result[i] = total;
                }
                break;
            }

            if (f.Count == 0) break;

            if (f[0] == PinsPerRack)
            {
                // Strike: 10 + los dos tiros siguientes.
                if (allRolls.Count <= rollIndex + 2) break;
                total += PinsPerRack + allRolls[rollIndex + 1] + allRolls[rollIndex + 2];
                rollIndex += 1;
            }
            else
            {
                if (f.Count < 2) break;

                if (f[0] + f[1] == PinsPerRack)
                {
                    // Spare: 10 + el tiro siguiente.
                    if (allRolls.Count <= rollIndex + 2) break;
                    total += PinsPerRack + allRolls[rollIndex + 2];
                }
                else
                {
                    total += f[0] + f[1];
                }
                rollIndex += 2;
            }

            result[i] = total;
        }

        return result;
    }

    /// <summary>Último puntaje acumulado que ya se puede calcular.</summary>
    public int TotalScore
    {
        get
        {
            int?[] scores = GetCumulativeScores();
            for (int i = scores.Length - 1; i >= 0; i--)
            {
                if (scores[i].HasValue) return scores[i].Value;
            }
            return 0;
        }
    }

    /// <summary>Símbolos de una ronda para el marcador: X strike, / spare, - cero.</summary>
    public string[] GetFrameMarks(int frameIndex)
    {
        List<int> f = frames[frameIndex];
        var marks = new string[f.Count];
        bool freshRack = true;
        int firstInRack = 0;

        for (int k = 0; k < f.Count; k++)
        {
            int pins = f[k];

            if (freshRack && pins == PinsPerRack)
            {
                marks[k] = "X";
            }
            else if (!freshRack && firstInRack + pins == PinsPerRack)
            {
                marks[k] = "/";
                freshRack = true;
            }
            else
            {
                marks[k] = pins == 0 ? "-" : pins.ToString();
                if (freshRack)
                {
                    firstInRack = pins;
                    freshRack = false;
                }
                else
                {
                    freshRack = true;
                }
            }
        }

        return marks;
    }
}
