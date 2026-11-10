using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Llena el Light Probe Group de este objeto con una cuadrícula de sondas de luz, para que
/// la bola y los pinos (que se mueven y no pueden usar lightmaps) reciban la luz horneada.
/// Uso: ajusta Size y Spacing, y en el Inspector haz clic en los tres puntos del componente
/// → "Generar cuadrícula de sondas". Solo hace algo dentro del editor.
/// </summary>
[RequireComponent(typeof(LightProbeGroup))]
public class LightProbeGrid : MonoBehaviour
{
    [Tooltip("Zona a cubrir en metros (ancho, alto, largo). La base queda a la altura de este objeto")]
    [SerializeField] private Vector3 size = new Vector3(2f, 1.8f, 22f);
    [Tooltip("Distancia aproximada entre sondas en metros")]
    [SerializeField] private Vector3 spacing = new Vector3(1f, 0.9f, 2f);

    [ContextMenu("Generar cuadrícula de sondas")]
    private void GenerateGrid()
    {
#if UNITY_EDITOR
        var group = GetComponent<LightProbeGroup>();

        int countX = Count(size.x, spacing.x);
        int countY = Count(size.y, spacing.y);
        int countZ = Count(size.z, spacing.z);

        var positions = new List<Vector3>();
        for (int x = 0; x < countX; x++)
        {
            for (int y = 0; y < countY; y++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    positions.Add(new Vector3(
                        Mathf.Lerp(-size.x / 2f, size.x / 2f, x / (countX - 1f)),
                        Mathf.Lerp(0.1f, size.y, y / (countY - 1f)),
                        Mathf.Lerp(-size.z / 2f, size.z / 2f, z / (countZ - 1f))));
                }
            }
        }

        Undo.RecordObject(group, "Generar sondas de luz");
        group.probePositions = positions.ToArray();
        EditorUtility.SetDirty(group);
        Debug.Log($"LightProbeGrid: se crearon {positions.Count} sondas. Ahora hornea la luz.", this);
#endif
    }

    private static int Count(float length, float step)
    {
        return Mathf.Max(2, Mathf.RoundToInt(length / Mathf.Max(0.1f, step)) + 1);
    }

    private void OnDrawGizmosSelected()
    {
        // Caja amarilla en la Scene: la zona que cubrirán las sondas.
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0f, size.y / 2f, 0f), size);
    }
}
