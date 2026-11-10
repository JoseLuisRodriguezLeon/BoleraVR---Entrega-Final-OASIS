using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón que se presiona apuntando con el rayo del control (VRPointer) y apretando el gatillo.
/// Necesita un BoxCollider del tamaño del botón. GameUI los crea automáticamente.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class VRButton : MonoBehaviour
{
    public event Action Clicked;

    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(0.16f, 0.22f, 0.36f, 1f);
    [SerializeField] private Color hoverColor = new Color(0.30f, 0.55f, 0.95f, 1f);

    private bool hovered;

    public void Setup(Image backgroundImage, Color normal, Color hover)
    {
        background = backgroundImage;
        normalColor = normal;
        hoverColor = hover;
        Refresh();
    }

    public void SetHover(bool value)
    {
        hovered = value;
        Refresh();
    }

    public void Click()
    {
        Clicked?.Invoke();
    }

    private void OnDisable()
    {
        hovered = false;
        Refresh();
    }

    private void Refresh()
    {
        if (background != null)
        {
            background.color = hovered ? hoverColor : normalColor;
        }
    }
}
