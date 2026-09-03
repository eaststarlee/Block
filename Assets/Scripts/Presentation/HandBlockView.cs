using System.Collections;
using System.Collections.Generic;
using BlockBlast.Core;
using BlockBlast.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 하단 손패 3개 중 개별 블록 슬롯의 렌더링 및 드래그 앤 드롭 인터랙션을 처리하는 클래스입니다.
    /// </summary>
    public sealed class HandBlockView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Serialized Fields

        [Header("Slot Settings")]
        [Tooltip("손패 슬롯 인덱스(0, 1, 2)입니다.")]
        [FormerlySerializedAs("slotIndex")]
        [SerializeField] private int _slotIndex;

        [Tooltip("드래그 시 손가락/마우스 포인터 위로 블록을 띄우는 오프셋 거리(픽셀)입니다.")]
        [FormerlySerializedAs("dragOffsetY")]
        [SerializeField] private float _dragOffsetY = 110f;

        [Tooltip("하단 슬롯 내부에 배치될 때의 미니 셀 크기(픽셀)입니다.")]
        [FormerlySerializedAs("slotCellSize")]
        [SerializeField] private float _slotCellSize = 42f;

        [Tooltip("드래그 중일 때 보드 셀 크기에 맞춰 확대되는 셀 크기(픽셀)입니다.")]
        [FormerlySerializedAs("boardCellSize")]
        [SerializeField] private float _boardCellSize = 102f;

        [Header("References")]
        [Tooltip("최상위 Canvas 참조입니다.")]
        [FormerlySerializedAs("rootCanvas")]
        [SerializeField] private Canvas _rootCanvas;

        [Tooltip("8x8 그리드 보드 뷰 참조입니다.")]
        [FormerlySerializedAs("boardView")]
        [SerializeField] private GridBoardView _boardView;

        [Tooltip("슬롯 원래 위치 RectTransform 앵커입니다.")]
        [FormerlySerializedAs("slotAnchor")]
        [SerializeField] private RectTransform _slotAnchor;

        [Tooltip("드래그 시 자유롭게 이동할 블록 비주얼 루트 RectTransform입니다.")]
        [FormerlySerializedAs("blockVisualRoot")]
        [SerializeField] private RectTransform _blockVisualRoot;

        #endregion

        #region Private Fields

        private BlockShapeData _currentShape;
        private readonly List<GameObject> _spawnedTileObjects = new List<GameObject>();
        private Coroutine _returnRoutine;
        private bool _isDragging;

        #endregion

        #region Public Properties

        public int SlotIndex => _slotIndex;
        public BlockShapeData CurrentShape => _currentShape;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSelf();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 컴포넌트 내부 및 슬롯 인덱스를 스스로 감지하고 초기 구성합니다.
        /// </summary>
        public void SetupSelf()
        {
            if (_slotAnchor == null)
            {
                _slotAnchor = (RectTransform)transform;
            }

            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }

            if (_boardView == null)
            {
                _boardView = Object.FindAnyObjectByType<GridBoardView>();
            }

            // GameObject 이름(HandSlot_0, HandSlot_1, HandSlot_2)에서 슬롯 인덱스 파싱
            if (gameObject.name.StartsWith("HandSlot_"))
            {
                string[] parts = gameObject.name.Split('_');
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedIndex))
                {
                    _slotIndex = parsedIndex;
                }
            }

            // 드래그 이벤트 수신용 투명 Image 확인
            var img = GetComponent<Image>();
            if (img == null)
            {
                img = gameObject.AddComponent<Image>();
            }

            img.color = new Color(1f, 1f, 1f, 0.001f);
            img.raycastTarget = true;

            // 드래그 전용 비주얼 루트 확인 및 생성
            if (_blockVisualRoot == null)
            {
                Transform existing = transform.Find("BlockVisualRoot");
                if (existing != null)
                {
                    _blockVisualRoot = (RectTransform)existing;
                }
                else
                {
                    var rootObj = new GameObject("BlockVisualRoot", typeof(RectTransform));
                    rootObj.transform.SetParent(_slotAnchor, false);
                    _blockVisualRoot = rootObj.GetComponent<RectTransform>();
                    _blockVisualRoot.anchoredPosition = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// 인덱스와 캔버스, 보드 뷰를 지정하여 수동 초기화합니다.
        /// </summary>
        public void Initialize(int index, Canvas canvas, GridBoardView gridView)
        {
            _slotIndex = index;
            _rootCanvas = canvas;
            _boardView = gridView;
            SetupSelf();
        }

        /// <summary>
        /// 슬롯에 새로운 블록 모양 데이터를 설정하고 타일을 렌더링합니다.
        /// </summary>
        /// <param name="shape">설정할 블록 데이터입니다. null일 경우 비활성화됩니다.</param>
        public void SetShape(BlockShapeData shape)
        {
            SetupSelf();

            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }

            _currentShape = shape;
            _isDragging = false;

            if (_blockVisualRoot != null)
            {
                _blockVisualRoot.SetParent(_slotAnchor, false);
                _blockVisualRoot.anchoredPosition = Vector2.zero;
                _blockVisualRoot.localScale = Vector3.one;
            }

            ClearSpawnedTiles();

            if (shape == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            RenderShape(shape, _slotCellSize);
        }

        #endregion

        #region Drag and Drop Event Handlers

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentShape == null || _blockVisualRoot == null)
            {
                return;
            }

            _isDragging = true;
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }

            // LayoutGroup의 위치 고정을 벗어나 자유롭게 이동하도록 최상단 Canvas로 임시 부모 변경
            if (_rootCanvas != null)
            {
                _blockVisualRoot.SetParent(_rootCanvas.transform, true);
                _blockVisualRoot.SetAsLastSibling();
            }

            // 보드 크기에 맞게 스케일 확대
            float scaleMultiplier = _boardCellSize / _slotCellSize;
            _blockVisualRoot.localScale = Vector3.one * scaleMultiplier;

            UpdateVisualPosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _currentShape == null)
            {
                return;
            }

            UpdateVisualPosition(eventData.position);
            CheckBoardHover(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging || _currentShape == null)
            {
                return;
            }

            _isDragging = false;

            if (_boardView != null)
            {
                _boardView.ClearPreview();
            }

            bool placed = TryPlaceOnBoard(eventData.position);

            if (!placed)
            {
                // 실패 시 슬롯 원래 위치로 복귀 애니메이션 실행
                if (gameObject.activeInHierarchy)
                {
                    _returnRoutine = StartCoroutine(ReturnToSlotRoutine());
                }
                else
                {
                    ResetToSlotImmediately();
                }
            }
        }

        #endregion

        #region Private Methods

        private void ClearSpawnedTiles()
        {
            foreach (var tile in _spawnedTileObjects)
            {
                if (tile != null)
                {
                    Destroy(tile);
                }
            }

            _spawnedTileObjects.Clear();
        }

        private void RenderShape(BlockShapeData shape, float cellSize)
        {
            if (_blockVisualRoot == null)
            {
                return;
            }

            var theme = ThemeManager.Instance;
            Sprite blockTileSprite = theme != null ? theme.BlockTileSprite : null;

            // 중앙 정렬을 위한 중심 오프셋 계산
            float halfW = (shape.Width - 1) * 0.5f;
            float halfH = (shape.Height - 1) * 0.5f;

            foreach (var cell in shape.Cells)
            {
                var tile = new GameObject($"Tile_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(_blockVisualRoot, false);
                _spawnedTileObjects.Add(tile);

                var rt = tile.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(cellSize - 4f, cellSize - 4f);
                rt.anchoredPosition = new Vector2((cell.x - halfW) * cellSize, (cell.y - halfH) * cellSize);

                var img = tile.GetComponent<Image>();
                img.raycastTarget = false; // 드래그 이벤트는 부모 슬롯이 감지

                if (shape.IsItemAt(cell.x, cell.y))
                {
                    Sprite customItemSprite = theme != null ? theme.GetItemSprite(shape.EmbeddedItem) : null;

                    if (customItemSprite != null)
                    {
                        img.sprite = customItemSprite;
                        img.color = Color.white;
                    }
                    else if (blockTileSprite != null)
                    {
                        img.sprite = blockTileSprite;
                        img.color = Color.white;
                    }
                    else
                    {
                        img.color = shape.BlockColor;
                    }
                }
                else
                {
                    if (blockTileSprite != null)
                    {
                        img.sprite = blockTileSprite;
                        img.color = Color.white;
                    }
                    else
                    {
                        img.color = shape.BlockColor;
                    }
                }
            }
        }

        private void UpdateVisualPosition(Vector2 screenPoint)
        {
            if (_blockVisualRoot == null)
            {
                return;
            }

            Camera cam = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _rootCanvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_rootCanvas.GetComponent<RectTransform>(), screenPoint, cam, out Vector3 worldPos))
            {
                float canvasScale = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
                worldPos.y += (_dragOffsetY * canvasScale);
                _blockVisualRoot.position = worldPos;
            }
        }

        private Vector2 GetCheckScreenPosition(Vector2 pointerScreenPos)
        {
            float scale = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
            return pointerScreenPos + new Vector2(0f, _dragOffsetY * scale);
        }

        private void CheckBoardHover(Vector2 screenPoint)
        {
            if (_boardView == null || GameManager.Instance == null || _currentShape == null)
            {
                return;
            }

            Camera cam = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _rootCanvas.worldCamera : null;
            Vector2 checkScreenPos = GetCheckScreenPosition(screenPoint);

            if (_boardView.TryGetGridCoordFromScreenPoint(checkScreenPos, cam, out Vector2Int gridOrigin))
            {
                int originX = gridOrigin.x - Mathf.FloorToInt((_currentShape.Width - 1) * 0.5f);
                int originY = gridOrigin.y - Mathf.FloorToInt((_currentShape.Height - 1) * 0.5f);

                bool canPlace = GameManager.Instance.Grid.CanPlaceShape(_currentShape, originX, originY);
                _boardView.ShowPreview(_currentShape, originX, originY, canPlace);
            }
            else
            {
                _boardView.ClearPreview();
            }
        }

        private bool TryPlaceOnBoard(Vector2 screenPoint)
        {
            if (_boardView == null || GameManager.Instance == null || _currentShape == null)
            {
                return false;
            }

            Camera cam = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _rootCanvas.worldCamera : null;
            Vector2 checkScreenPos = GetCheckScreenPosition(screenPoint);

            if (_boardView.TryGetGridCoordFromScreenPoint(checkScreenPos, cam, out Vector2Int gridOrigin))
            {
                int originX = gridOrigin.x - Mathf.FloorToInt((_currentShape.Width - 1) * 0.5f);
                int originY = gridOrigin.y - Mathf.FloorToInt((_currentShape.Height - 1) * 0.5f);

                return GameManager.Instance.TryPlaceHandBlock(_slotIndex, originX, originY);
            }

            return false;
        }

        private void ResetToSlotImmediately()
        {
            if (_blockVisualRoot != null)
            {
                _blockVisualRoot.SetParent(_slotAnchor, false);
                _blockVisualRoot.anchoredPosition = Vector2.zero;
                _blockVisualRoot.localScale = Vector3.one;
            }
        }

        private IEnumerator ReturnToSlotRoutine()
        {
            if (_blockVisualRoot == null)
            {
                yield break;
            }

            Vector3 startPos = _blockVisualRoot.position;
            Vector3 startScale = _blockVisualRoot.localScale;

            Vector3 targetWorldPos = _slotAnchor.position;
            Vector3 targetScale = Vector3.one;

            float elapsed = 0f;
            float duration = 0.16f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _blockVisualRoot.position = Vector3.Lerp(startPos, targetWorldPos, t);
                _blockVisualRoot.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            ResetToSlotImmediately();
            _returnRoutine = null;
        }

        #endregion
    }
}
