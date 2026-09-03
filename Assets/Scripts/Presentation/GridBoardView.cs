using System.Collections.Generic;
using BlockBlast.Core;
using BlockBlast.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 8x8 보드 전체 뷰 및 마우스/터치 드래그 위치에 따른 정밀 셀 히트테스트와 프리뷰 하이라이트를 총괄하는 클래스입니다.
    /// </summary>
    public sealed class GridBoardView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("보드 데이터 및 비즈니스 로직 관리자입니다.")]
        [FormerlySerializedAs("gridManager")]
        [SerializeField] private GridManager _gridManager;

        [Tooltip("보드 컨테이너 RectTransform입니다.")]
        [FormerlySerializedAs("boardContainer")]
        [SerializeField] private RectTransform _boardContainer;

        [Tooltip("보드 셀 격자 레이아웃 그룹입니다.")]
        [FormerlySerializedAs("gridLayout")]
        [SerializeField] private GridLayoutGroup _gridLayout;

        [Header("Highlight Colors")]
        [Tooltip("블록 배치가 가능한 위치일 때 표시되는 하이라이트 색상입니다.")]
        [FormerlySerializedAs("validPreviewColor")]
        [SerializeField] private Color _validPreviewColor = new Color(0.2f, 0.9f, 0.3f, 0.55f);

        [Tooltip("블록 배치가 불가능한 위치일 때 표시되는 하이라이트 색상입니다.")]
        [FormerlySerializedAs("invalidPreviewColor")]
        [SerializeField] private Color _invalidPreviewColor = new Color(0.9f, 0.2f, 0.2f, 0.45f);

        #endregion

        #region Private Fields

        private GridCellView[,] _cellViews = new GridCellView[GridManager.BoardSize, GridManager.BoardSize];
        private readonly List<Vector2Int> _currentHighlightedCells = new List<Vector2Int>();

        #endregion

        #region Public Properties

        public RectTransform BoardContainer => _boardContainer != null ? _boardContainer : (RectTransform)transform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSelf();
        }

        private void OnDestroy()
        {
            if (_gridManager != null)
            {
                _gridManager.OnCellUpdated -= HandleCellUpdated;
                _gridManager.OnLinesCleared -= HandleLinesCleared;
                _gridManager.OnBoardReset -= HandleBoardReset;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 내부 컴포넌트 참조 및 64개 셀 뷰를 자동으로 매핑하고 이벤트를 구독합니다.
        /// </summary>
        public void SetupSelf()
        {
            if (_gridManager == null)
            {
                _gridManager = Object.FindAnyObjectByType<GridManager>();
            }

            if (_boardContainer == null)
            {
                _boardContainer = (RectTransform)transform;
            }

            if (_gridLayout == null)
            {
                _gridLayout = GetComponentInChildren<GridLayoutGroup>();
            }

            EnsureCellViews();

            if (_gridManager != null)
            {
                _gridManager.OnCellUpdated -= HandleCellUpdated;
                _gridManager.OnCellUpdated += HandleCellUpdated;

                _gridManager.OnLinesCleared -= HandleLinesCleared;
                _gridManager.OnLinesCleared += HandleLinesCleared;

                _gridManager.OnBoardReset -= HandleBoardReset;
                _gridManager.OnBoardReset += HandleBoardReset;
            }
        }

        /// <summary>
        /// 64개 자식 셀 뷰 컴포넌트들의 좌표를 2차원 배열에 완벽하게 등록합니다.
        /// </summary>
        public void EnsureCellViews()
        {
            var cells = GetComponentsInChildren<GridCellView>(true);
            foreach (var cell in cells)
            {
                if (cell != null)
                {
                    cell.SetupSelf();
                    int gx = cell.GridX;
                    int gy = cell.GridY;
                    if (gx >= 0 && gx < GridManager.BoardSize && gy >= 0 && gy < GridManager.BoardSize)
                    {
                        _cellViews[gx, gy] = cell;
                    }
                }
            }
        }

        /// <summary>
        /// 수동으로 관리자와 셀 뷰 목록을 전달받아 초기화합니다.
        /// </summary>
        public void Initialize(GridManager manager, GridCellView[,] cells)
        {
            _gridManager = manager;
            _cellViews = cells;
            SetupSelf();
        }

        /// <summary>
        /// 화면 포인터 좌표를 기준으로 64개 개별 셀을 정밀 히트테스트하여 가장 인접한 (x, y) 그리드 좌표를 도출합니다.
        /// </summary>
        /// <param name="screenPoint">마우스/터치 화면 좌표입니다.</param>
        /// <param name="cam">UI 렌더링에 사용되는 카메라입니다.</param>
        /// <param name="gridCoord">판별된 그리드 좌표 출력값입니다.</param>
        public bool TryGetGridCoordFromScreenPoint(Vector2 screenPoint, Camera cam, out Vector2Int gridCoord)
        {
            gridCoord = new Vector2Int(-1, -1);
            EnsureCellViews();

            float minDistanceSqr = float.MaxValue;
            Vector2Int closestCoord = new Vector2Int(-1, -1);

            for (int x = 0; x < GridManager.BoardSize; x++)
            {
                for (int y = 0; y < GridManager.BoardSize; y++)
                {
                    var cell = _cellViews[x, y];
                    if (cell == null)
                    {
                        continue;
                    }

                    RectTransform rt = (RectTransform)cell.transform;

                    // 1. 셀 영역 내 직접 포함 여부 검사
                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, cam))
                    {
                        gridCoord = new Vector2Int(x, y);
                        return true;
                    }

                    // 2. 셀 중심점과의 화면 거리 계산 (경계선 인근 스냅 보정)
                    Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                    float distSqr = (cellScreenPos - screenPoint).sqrMagnitude;
                    if (distSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distSqr;
                        closestCoord = new Vector2Int(x, y);
                    }
                }
            }

            // 셀 크기 범위 내에 있으면 가장 가까운 셀로 스냅
            if (closestCoord.x >= 0 && minDistanceSqr < 40000f) // 200px 반경
            {
                gridCoord = closestCoord;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 블록 호버 시 배치 가능 여부에 따라 보드 위에 프리뷰 하이라이트를 표시합니다.
        /// </summary>
        public void ShowPreview(BlockShapeData shape, int originX, int originY, bool canPlace)
        {
            ClearPreview();
            EnsureCellViews();

            if (shape == null)
            {
                return;
            }

            Color color = canPlace ? _validPreviewColor : _invalidPreviewColor;

            foreach (var cell in shape.Cells)
            {
                int gx = originX + cell.x;
                int gy = originY + cell.y;

                if (_gridManager != null && _gridManager.IsValidCoordinate(gx, gy) && _cellViews[gx, gy] != null)
                {
                    _cellViews[gx, gy].SetHighlight(true, color);
                    _currentHighlightedCells.Add(new Vector2Int(gx, gy));
                }
            }
        }

        /// <summary>
        /// 표시 중인 모든 프리뷰 하이라이트를 제거합니다.
        /// </summary>
        public void ClearPreview()
        {
            EnsureCellViews();
            foreach (var coord in _currentHighlightedCells)
            {
                if (_gridManager != null && _gridManager.IsValidCoordinate(coord.x, coord.y) && _cellViews[coord.x, coord.y] != null)
                {
                    _cellViews[coord.x, coord.y].SetHighlight(false, Color.clear);
                }
            }

            _currentHighlightedCells.Clear();
        }

        #endregion

        #region Event Handlers

        private void HandleCellUpdated(int x, int y, Color color, ItemType itemType)
        {
            EnsureCellViews();
            if (_gridManager != null && _gridManager.IsValidCoordinate(x, y) && _cellViews[x, y] != null)
            {
                bool isOccupied = color.a > 0.01f || itemType != ItemType.None;
                _cellViews[x, y].SetState(isOccupied, color, itemType);
            }
        }

        private void HandleLinesCleared(int lineCount, List<Vector2Int> coords, List<ItemType> items)
        {
            EnsureCellViews();
            foreach (var coord in coords)
            {
                if (_gridManager != null && _gridManager.IsValidCoordinate(coord.x, coord.y) && _cellViews[coord.x, coord.y] != null)
                {
                    _cellViews[coord.x, coord.y].PlayBlastEffect();
                }
            }
        }

        private void HandleBoardReset()
        {
            ClearPreview();
            EnsureCellViews();
            for (int x = 0; x < GridManager.BoardSize; x++)
            {
                for (int y = 0; y < GridManager.BoardSize; y++)
                {
                    if (_cellViews[x, y] != null)
                    {
                        _cellViews[x, y].SetState(false, Color.clear, ItemType.None);
                    }
                }
            }
        }

        #endregion
    }
}
