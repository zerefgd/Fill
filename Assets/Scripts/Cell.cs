using UnityEngine;

public class Cell : MonoBehaviour
{
    [HideInInspector] public bool Blocked;
    [HideInInspector] public bool Filled;

    [SerializeField] private Color _blockedColor;
    [SerializeField] private Color _emptyColor;
    [SerializeField] private Color _filledColor;
    [SerializeField] private SpriteRenderer _cellRenderer;

    public void Init(int fill)
    {
        Blocked = fill == 1;
        Filled = Blocked;
        _cellRenderer.color = Blocked ? _blockedColor : _emptyColor;
    }

    public void Add()
    {
        Filled = true;
        _cellRenderer.color = _filledColor;
    }

    public void Remove()
    {
        Filled = false;
        _cellRenderer.color = _emptyColor;
    }

    public void ChangeState()
    {
        Blocked = !Blocked;
        Filled = Blocked;
        _cellRenderer.color = Blocked ? _blockedColor : _emptyColor;
    }
}
