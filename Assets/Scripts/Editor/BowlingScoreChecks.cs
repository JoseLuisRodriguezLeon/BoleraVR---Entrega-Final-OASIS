using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pruebas rápidas de la lógica de puntaje, sin casco ni escena.
/// Menú de Unity: Bolera → Probar puntaje. Los resultados salen en la Console.
/// Este archivo va dentro de una carpeta llamada "Editor" para que no entre en el build.
/// </summary>
public static class BowlingScoreChecks
{
    [MenuItem("Bolera/Probar puntaje")]
    public static void RunAll()
    {
        int failed = 0;

        failed += Check("1 ronda: strike, strike, strike", 1, new[] { 10, 10, 10 }, 30);
        failed += Check("1 ronda: 9 y spare, luego 5", 1, new[] { 9, 1, 5 }, 15);
        failed += Check("1 ronda: 7 y 2", 1, new[] { 7, 2 }, 9);

        failed += Check("5 rondas: todo strikes", 5, Repeat(10, 7), 150);
        failed += Check("5 rondas: todo 9 y 0", 5, Pairs(9, 0, 5), 45);
        failed += Check("5 rondas: todo spares 5/5, extra 5", 5, WithExtra(Pairs(5, 5, 5), 5), 75);

        failed += Check("10 rondas: partida perfecta", 10, Repeat(10, 12), 300);
        failed += Check("10 rondas: todo 9 y 0", 10, Pairs(9, 0, 10), 90);
        failed += Check("10 rondas: todo spares 5/5, extra 5", 10, WithExtra(Pairs(5, 5, 10), 5), 150);
        failed += Check("10 rondas: partida mixta clásica", 10,
            new[] { 10, 7, 3, 9, 0, 10, 0, 8, 8, 2, 0, 6, 10, 10, 10, 8, 1 }, 167);

        if (failed == 0)
        {
            Debug.Log("PUNTAJE: todas las pruebas pasaron.");
        }
        else
        {
            Debug.LogError($"PUNTAJE: {failed} prueba(s) fallaron. Revisa los mensajes de arriba.");
        }
    }

    private static int Check(string name, int frames, int[] rolls, int expected)
    {
        var score = new BowlingScore(frames);

        for (int i = 0; i < rolls.Length; i++)
        {
            if (score.IsGameOver)
            {
                Debug.LogError($"FALLÓ — {name}: la partida terminó antes del tiro {i + 1}.");
                return 1;
            }
            score.RecordRoll(rolls[i]);
        }

        if (!score.IsGameOver)
        {
            Debug.LogError($"FALLÓ — {name}: la partida no terminó después del último tiro.");
            return 1;
        }

        if (score.RecordRoll(0))
        {
            Debug.LogError($"FALLÓ — {name}: aceptó un tiro después de terminar.");
            return 1;
        }

        if (score.TotalScore != expected)
        {
            Debug.LogError($"FALLÓ — {name}: esperaba {expected}, dio {score.TotalScore}.");
            return 1;
        }

        Debug.Log($"OK — {name}: {score.TotalScore}");
        return 0;
    }

    private static int[] Repeat(int pins, int count)
    {
        var rolls = new int[count];
        for (int i = 0; i < count; i++) rolls[i] = pins;
        return rolls;
    }

    private static int[] Pairs(int first, int second, int frames)
    {
        var rolls = new List<int>();
        for (int i = 0; i < frames; i++)
        {
            rolls.Add(first);
            rolls.Add(second);
        }
        return rolls.ToArray();
    }

    private static int[] WithExtra(int[] rolls, int extra)
    {
        var list = new List<int>(rolls) { extra };
        return list.ToArray();
    }
}
