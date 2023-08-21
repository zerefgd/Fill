using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{

    [SerializeField] private int _row;
    [SerializeField] private int _col;
    [SerializeField] private Level _level;
    [SerializeField] private Cell _cellPrefab;
    [SerializeField] private Transform _edgePrefab;

    private Cell[,] cells;
    private List<Vector2Int> filledPoints;
    private List<Transform> edges;
    private Vector2Int startPos, endPos;
    private List<Vector2Int> directions = new List<Vector2Int>()
    {
        Vector2Int.up, Vector2Int.down,Vector2Int.left,Vector2Int.right
    };

    private void Awake()
    {
        filledPoints = new List<Vector2Int>();
        cells = new Cell[_row, _col];
        edges = new List<Transform>();
        CreateLevel();
        SpawnLevel();
    }

    private void CreateLevel()
    {
        if (_level.Row == _row && _level.Col == _col) return;

        _level.Row = _row;
        _level.Col = _col;
        _level.Data = new List<int>();

        for (int i = 0; i < _row; i++)
        {
            for (int j = 0; j < _col; j++)
            {
                _level.Data.Add(0);
            }
        }

        EditorUtility.SetDirty(_level);
    }

    private void SpawnLevel()
    {
        Vector3 camPos = Camera.main.transform.position;
        camPos.x = _level.Col * 0.5f;
        camPos.y = _level.Row * 0.5f;
        Camera.main.transform.position = camPos;
        Camera.main.orthographicSize = Mathf.Max(_level.Row, _level.Col) + 2f;

        for (int i = 0; i < _level.Row; i++)
        {
            for (int j = 0; j < _level.Col; j++)
            {
                cells[i, j] = Instantiate(_cellPrefab);
                cells[i, j].Init(_level.Data[i * _level.Col + j]);
                cells[i, j].transform.position = new Vector3(j + 0.5f, i + 0.5f, 0);
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            startPos = new Vector2Int(Mathf.FloorToInt(mousePos.y), Mathf.FloorToInt(mousePos.x));
            if (!IsValid(startPos)) return;
            bool blocked = cells[startPos.x, startPos.y].Blocked;
            bool filled = cells[startPos.x, startPos.y].Filled;
            if (!blocked && filled) return;
            cells[startPos.x, startPos.y].ChangeState();
            _level.Data[startPos.x * _col + startPos.y] = blocked ? 0 : 1;
            EditorUtility.SetDirty(_level);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            startPos = new Vector2Int(Mathf.FloorToInt(mousePos.y), Mathf.FloorToInt(mousePos.x));
            endPos = startPos;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            endPos = new Vector2Int(Mathf.FloorToInt(mousePos.y), Mathf.FloorToInt(mousePos.x));

            if (!IsNeighbour()) return;

            if (AddEmpty())
            {
                filledPoints.Add(startPos);
                filledPoints.Add(endPos);
                cells[startPos.x, startPos.y].Add();
                cells[endPos.x, endPos.y].Add();
                Transform edge = Instantiate(_edgePrefab);
                edges.Add(edge);
                edge.transform.position = new Vector3(
                    startPos.y * 0.5f + 0.5f + endPos.y * 0.5f,
                    startPos.x * 0.5f + 0.5f + endPos.x * 0.5f,
                    0f
                    );
                bool horizontal = (endPos.y - startPos.y) < 0 || (endPos.y - startPos.y) > 0;
                edge.transform.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0);
            }
            else if (AddToEnd())
            {
                filledPoints.Add(endPos);
                cells[endPos.x, endPos.y].Add();
                Transform edge = Instantiate(_edgePrefab);
                edges.Add(edge);
                edge.transform.position = new Vector3(
                    startPos.y * 0.5f + 0.5f + endPos.y * 0.5f,
                    startPos.x * 0.5f + 0.5f + endPos.x * 0.5f,
                    0f
                    );
                bool horizontal = (endPos.y - startPos.y) < 0 || (endPos.y - startPos.y) > 0;
                edge.transform.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0);
            }
            else if (AddToStart())
            {
                filledPoints.Insert(0, endPos);
                cells[endPos.x, endPos.y].Add();
                Transform edge = Instantiate(_edgePrefab);
                edges.Insert(0, edge);
                edge.transform.position = new Vector3(
                    startPos.y * 0.5f + 0.5f + endPos.y * 0.5f,
                    startPos.x * 0.5f + 0.5f + endPos.x * 0.5f,
                    0f
                    );
                bool horizontal = (endPos.y - startPos.y) < 0 || (endPos.y - startPos.y) > 0;
                edge.transform.eulerAngles = new Vector3(0, 0, horizontal ? 90f : 0);
            }
            else if (RemoveFromEnd())
            {
                Transform removeEdge = edges[edges.Count - 1];
                edges.RemoveAt(edges.Count - 1);
                Destroy(removeEdge.gameObject);
                filledPoints.RemoveAt(filledPoints.Count - 1);
                cells[startPos.x, startPos.y].Remove();
            }
            else if (RemoveFromStart())
            {
                Transform removeEdge = edges[0];
                edges.RemoveAt(0);
                Destroy(removeEdge.gameObject);
                filledPoints.RemoveAt(0);
                cells[startPos.x, startPos.y].Remove();
            }

            RemoveEmpty();
            startPos = endPos;
        }
    }

    private bool AddEmpty()
    {
        if (edges.Count > 0) return false;
        if (cells[startPos.x, startPos.y].Filled) return false;
        if (cells[endPos.x, endPos.y].Filled) return false;
        return true;
    }

    private bool AddToEnd()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[filledPoints.Count - 1];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        if (cells[endPos.x, endPos.y].Filled) return false;
        return true;
    }

    private bool AddToStart()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[0];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        if (cells[endPos.x, endPos.y].Filled) return false;
        return true;
    }

    private bool RemoveFromEnd()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[filledPoints.Count - 1];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        pos = filledPoints[filledPoints.Count - 2];
        lastCell = cells[pos.x, pos.y];
        if (cells[endPos.x, endPos.y] != lastCell) return false;
        return true;
    }
    private bool RemoveFromStart()
    {
        if (filledPoints.Count < 2) return false;
        Vector2Int pos = filledPoints[0];
        Cell lastCell = cells[pos.x, pos.y];
        if (cells[startPos.x, startPos.y] != lastCell) return false;
        pos = filledPoints[1];
        lastCell = cells[pos.x, pos.y];
        if (cells[endPos.x, endPos.y] != lastCell) return false;
        return true;
    }

    private void RemoveEmpty()
    {
        if (filledPoints.Count != 1) return;
        cells[filledPoints[0].x, filledPoints[0].y].Remove();
        filledPoints.RemoveAt(0);
    }

    private bool IsNeighbour()
    {
        return IsValid(startPos) && IsValid(endPos) && directions.Contains(startPos - endPos);
    }

    private bool IsValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < _level.Row && pos.y < _level.Col;
    }
}
