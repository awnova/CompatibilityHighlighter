using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CompatibilityHighlighter
{
    internal sealed class CellBorderOverlay : MonoBehaviour
    {
        private const float SurplusPixel = 1f;

        private static CellBorderOverlay _instance;

        private static readonly Vector3[] WorldCorners = new Vector3[4];

        private readonly Dictionary<RectTransform, List<RectTransform>> _groups =
            new Dictionary<RectTransform, List<RectTransform>>();
        private readonly List<CellBorderMesh> _active = new List<CellBorderMesh>();
        private readonly List<CellBorderMesh> _pool = new List<CellBorderMesh>();
        private readonly List<Rect> _scratch = new List<Rect>();
        private RectTransform _poolRoot;

        private void Awake()
        {
            _instance = this;
        }

        internal static void Clear()
        {
            _instance?.ClearInternal();
        }

        internal static void Rebuild(List<RectTransform> cells, Color color)
        {
            _instance?.RebuildInternal(cells, color);
        }

        private void RebuildInternal(List<RectTransform> cells, Color color)
        {
            ClearInternal();

            if (cells == null || cells.Count == 0)
            {
                return;
            }

            color.a = 1f;

            _groups.Clear();
            foreach (var cell in cells)
            {
                if (cell != null && cell.parent is RectTransform container)
                {
                    if (!_groups.TryGetValue(container, out var group))
                    {
                        group = new List<RectTransform>();
                        _groups[container] = group;
                    }

                    group.Add(cell);
                }
            }

            foreach (var pair in _groups)
            {
                var mesh = Rent(pair.Key);

                _scratch.Clear();
                foreach (var cell in pair.Value)
                {
                    _scratch.Add(ToLocalRect(cell, mesh.rectTransform));
                }

                mesh.SetCells(_scratch, color);
                _active.Add(mesh);
            }

            _groups.Clear();
        }

        private static Rect ToLocalRect(RectTransform cell, RectTransform space)
        {
            cell.GetWorldCorners(WorldCorners);
            var min = space.InverseTransformPoint(WorldCorners[0]);
            var max = space.InverseTransformPoint(WorldCorners[2]);

            return Rect.MinMaxRect(min.x, min.y + SurplusPixel, max.x - SurplusPixel, max.y);
        }

        private void ClearInternal()
        {
            foreach (var mesh in _active)
            {
                if (mesh == null)
                {
                    continue;
                }

                mesh.gameObject.SetActive(false);
                mesh.rectTransform.SetParent(_poolRoot, false);
                _pool.Add(mesh);
            }

            _active.Clear();
        }

        private CellBorderMesh Rent(RectTransform container)
        {
            CellBorderMesh mesh = null;
            while (_pool.Count > 0)
            {
                int last = _pool.Count - 1;
                mesh = _pool[last];
                _pool.RemoveAt(last);
                if (mesh != null)
                {
                    break;
                }
            }

            if (mesh == null)
            {
                mesh = CreateMesh();
            }

            var rect = mesh.rectTransform;
            rect.SetParent(container, false);
            rect.SetAsLastSibling();
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            mesh.gameObject.layer = container.gameObject.layer;
            mesh.gameObject.SetActive(true);
            return mesh;
        }

        private CellBorderMesh CreateMesh()
        {
            EnsurePoolRoot();

            var go = new GameObject("CompatibilityHighlighterBorder",
                typeof(RectTransform), typeof(CellBorderMesh));
            go.transform.SetParent(_poolRoot, false);
            go.SetActive(false);

            return go.GetComponent<CellBorderMesh>();
        }

        private void EnsurePoolRoot()
        {
            if (_poolRoot != null)
            {
                return;
            }

            var root = new GameObject("CompatibilityHighlighterBorderPool", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            root.SetActive(false);
            _poolRoot = (RectTransform)root.transform;
        }

        private void OnDestroy()
        {
            ClearInternal();

            foreach (var mesh in _pool)
            {
                if (mesh != null)
                {
                    Object.Destroy(mesh.gameObject);
                }
            }

            _pool.Clear();

            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    internal sealed class CellBorderMesh : MaskableGraphic
    {
        private const float Thickness = 1f;

        private const float Epsilon = 0.5f;

        private readonly List<Rect> _cells = new List<Rect>();
        private readonly List<Vector2> _covered = new List<Vector2>();

        protected override void Awake()
        {
            base.Awake();

            raycastTarget = false;
        }

        internal void SetCells(List<Rect> cells, Color stroke)
        {
            _cells.Clear();
            _cells.AddRange(cells);
            color = stroke;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];

                EmitEdge(vh, i, cell, Edge.Top);
                EmitEdge(vh, i, cell, Edge.Bottom);
                EmitEdge(vh, i, cell, Edge.Left);
                EmitEdge(vh, i, cell, Edge.Right);
            }
        }

        private enum Edge { Top, Bottom, Left, Right }

        private void EmitEdge(VertexHelper vh, int index, Rect cell, Edge edge)
        {
            bool horizontal = edge == Edge.Top || edge == Edge.Bottom;
            float from = horizontal ? cell.xMin : cell.yMin;
            float to = horizontal ? cell.xMax : cell.yMax;

            if (edge == Edge.Top || edge == Edge.Left)
            {
                EmitSegment(vh, cell, edge, from, to);
                return;
            }

            _covered.Clear();
            for (int j = 0; j < _cells.Count; j++)
            {
                if (j == index)
                {
                    continue;
                }

                var other = _cells[j];
                if (!Touches(cell, other, edge))
                {
                    continue;
                }

                float overlapFrom = Mathf.Max(from, horizontal ? other.xMin : other.yMin);
                float overlapTo = Mathf.Min(to, horizontal ? other.xMax : other.yMax);
                if (overlapTo - overlapFrom > Epsilon)
                {
                    _covered.Add(new Vector2(overlapFrom, overlapTo));
                }
            }

            _covered.Sort((a, b) => a.x.CompareTo(b.x));

            float cursor = from;
            for (int c = 0; c < _covered.Count; c++)
            {
                var span = _covered[c];
                if (span.x - cursor > Epsilon)
                {
                    EmitSegment(vh, cell, edge, cursor, span.x);
                }

                cursor = Mathf.Max(cursor, span.y);
            }

            if (to - cursor > Epsilon)
            {
                EmitSegment(vh, cell, edge, cursor, to);
            }
        }

        private static bool Touches(Rect cell, Rect other, Edge edge)
        {
            switch (edge)
            {
                case Edge.Top:
                    return Mathf.Abs(other.yMin - cell.yMax) < Epsilon && Overlaps(
                        cell.xMin, cell.xMax, other.xMin, other.xMax);
                case Edge.Bottom:
                    return Mathf.Abs(other.yMax - cell.yMin) < Epsilon && Overlaps(
                        cell.xMin, cell.xMax, other.xMin, other.xMax);
                case Edge.Left:
                    return Mathf.Abs(other.xMax - cell.xMin) < Epsilon && Overlaps(
                        cell.yMin, cell.yMax, other.yMin, other.yMax);
                default:
                    return Mathf.Abs(other.xMin - cell.xMax) < Epsilon && Overlaps(
                        cell.yMin, cell.yMax, other.yMin, other.yMax);
            }
        }

        private static bool Overlaps(float aMin, float aMax, float bMin, float bMax) =>
            Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin) > Epsilon;

        private void EmitSegment(VertexHelper vh, Rect cell, Edge edge, float from, float to)
        {
            switch (edge)
            {
                case Edge.Top:
                    AddQuad(vh, from, cell.yMax - Thickness, to, cell.yMax);
                    break;
                case Edge.Bottom:
                    AddQuad(vh, from, cell.yMin, to, cell.yMin + Thickness);
                    break;
                case Edge.Left:
                    AddQuad(vh, cell.xMin, from, cell.xMin + Thickness, to);
                    break;
                default:
                    AddQuad(vh, cell.xMax - Thickness, from, cell.xMax, to);
                    break;
            }
        }

        private void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax)
        {
            int start = vh.currentVertCount;

            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector3(xMin, yMin);
            vh.AddVert(vertex);
            vertex.position = new Vector3(xMin, yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMin);
            vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
