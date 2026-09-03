using System;
using System.Collections.Generic;
using BlockBlast.Core;
using UnityEngine;

namespace BlockBlast.Gameplay
{
    /// <summary>
    /// 8x8 보드 그리드 데이터 상태 관리 및 라인 완성 검사/Blast 파괴 로직을 수행하는 클래스입니다.
    /// </summary>
    public sealed class GridManager : MonoBehaviour
    {
        public const int BoardSize = 8;

        /// <summary>
        /// 8x8 그리드의 개별 셀 데이터 구조체입니다.
        /// </summary>
        public struct GridCellData
        {
            public bool IsOccupied;
            public Color Color;
            public ItemType ItemType;

            public void Reset()
            {
                IsOccupied = false;
                Color = Color.clear;
                ItemType = ItemType.None;
            }
        }

        #region Private Fields

        private readonly GridCellData[,] _board = new GridCellData[BoardSize, BoardSize];

        #endregion

        #region Events

        public event Action<int, int, Color, ItemType> OnCellUpdated; // (x, y, color, item)
        public event Action<int, List<Vector2Int>, List<ItemType>> OnLinesCleared; // (lineCount, clearedCoords, triggeredItems)
        public event Action<ItemType, Vector2Int> OnItemTriggered; // (itemType, cellCoord)
        public event Action<BlockShapeData, int, int> OnBlockPlaced; // (shape, originX, originY)
        public event Action OnBoardReset;

        #endregion

        #region Public Properties

        public GridCellData[,] Board => _board;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResetBoard();
        }

        #endregion

        #region Public Methods - Board Operations

        /// <summary>
        /// 보드의 모든 셀을 비우고 초기화합니다.
        /// </summary>
        public void ResetBoard()
        {
            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    _board[x, y].Reset();
                    OnCellUpdated?.Invoke(x, y, Color.clear, ItemType.None);
                }
            }

            OnBoardReset?.Invoke();
        }

        /// <summary>
        /// 좌표가 8x8 보드 범위 내에 있는지 유효성을 검사합니다.
        /// </summary>
        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < BoardSize && y >= 0 && y < BoardSize;
        }

        /// <summary>
        /// 지정한 좌표의 셀 데이터를 반환합니다.
        /// </summary>
        public GridCellData GetCell(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
            {
                return default;
            }

            return _board[x, y];
        }

        /// <summary>
        /// 해당 좌표에 블록 배치가 가능한지 검사합니다.
        /// </summary>
        public bool CanPlaceShape(BlockShapeData shape, int gridX, int gridY)
        {
            if (shape == null || shape.Cells == null || shape.Cells.Count == 0)
            {
                return false;
            }

            foreach (var cell in shape.Cells)
            {
                int targetX = gridX + cell.x;
                int targetY = gridY + cell.y;

                if (!IsValidCoordinate(targetX, targetY))
                {
                    return false;
                }

                if (_board[targetX, targetY].IsOccupied)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 블록을 보드에 배치하고 줄 완성 검사 및 아이템 발동을 수행합니다.
        /// </summary>
        public bool PlaceShape(BlockShapeData shape, int gridX, int gridY, out int clearedLinesCount, out List<ItemType> triggeredItems)
        {
            clearedLinesCount = 0;
            triggeredItems = new List<ItemType>();

            if (!CanPlaceShape(shape, gridX, gridY))
            {
                return false;
            }

            // 1. 블록 셀 배치
            foreach (var cell in shape.Cells)
            {
                int targetX = gridX + cell.x;
                int targetY = gridY + cell.y;

                bool isItemCell = shape.IsItemAt(cell.x, cell.y);
                ItemType cellItem = isItemCell ? shape.EmbeddedItem : ItemType.None;

                _board[targetX, targetY].IsOccupied = true;
                _board[targetX, targetY].Color = shape.BlockColor;
                _board[targetX, targetY].ItemType = cellItem;

                OnCellUpdated?.Invoke(targetX, targetY, shape.BlockColor, cellItem);
            }

            OnBlockPlaced?.Invoke(shape, gridX, gridY);

            // 2. 완성된 라인(가로/세로) 탐색 및 Blast
            CheckAndClearLines(out clearedLinesCount, out triggeredItems);

            return true;
        }

        /// <summary>
        /// 가로/세로 완성된 모든 줄을 검사하고 Blast 처리합니다.
        /// </summary>
        public void CheckAndClearLines(out int clearedLinesCount, out List<ItemType> triggeredItems)
        {
            clearedLinesCount = 0;
            triggeredItems = new List<ItemType>();

            var fullRows = new List<int>();
            var fullCols = new List<int>();

            // 가로 줄 검사
            for (int y = 0; y < BoardSize; y++)
            {
                bool rowFull = true;
                for (int x = 0; x < BoardSize; x++)
                {
                    if (!_board[x, y].IsOccupied)
                    {
                        rowFull = false;
                        break;
                    }
                }

                if (rowFull)
                {
                    fullRows.Add(y);
                }
            }

            // 세로 줄 검사
            for (int x = 0; x < BoardSize; x++)
            {
                bool colFull = true;
                for (int y = 0; y < BoardSize; y++)
                {
                    if (!_board[x, y].IsOccupied)
                    {
                        colFull = false;
                        break;
                    }
                }

                if (colFull)
                {
                    fullCols.Add(x);
                }
            }

            clearedLinesCount = fullRows.Count + fullCols.Count;
            if (clearedLinesCount == 0)
            {
                return;
            }

            // 중복 없이 클리어 대상 셀 수집
            var cellsToClear = new HashSet<Vector2Int>();
            foreach (int y in fullRows)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    cellsToClear.Add(new Vector2Int(x, y));
                }
            }

            foreach (int x in fullCols)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    cellsToClear.Add(new Vector2Int(x, y));
                }
            }

            var clearedList = new List<Vector2Int>(cellsToClear);
            var itemsToTrigger = new List<(ItemType item, Vector2Int coord)>();

            // 1단계: 먼저 모든 셀을 완전히 초기화하고 아이템 목록을 수집
            foreach (var coord in clearedList)
            {
                var cell = _board[coord.x, coord.y];
                if (cell.ItemType != ItemType.None)
                {
                    triggeredItems.Add(cell.ItemType);
                    itemsToTrigger.Add((cell.ItemType, coord));
                }

                _board[coord.x, coord.y].Reset();
                OnCellUpdated?.Invoke(coord.x, coord.y, Color.clear, ItemType.None);
            }

            OnLinesCleared?.Invoke(clearedLinesCount, clearedList, triggeredItems);

            // 2단계: 보드가 완전히 정리된 후 아이템 이벤트를 발동
            foreach (var (item, coord) in itemsToTrigger)
            {
                OnItemTriggered?.Invoke(item, coord);
            }
        }

        /// <summary>
        /// 특정 블록 모양이 보드 상 임의의 위치에 배치 가능한지 검사합니다.
        /// </summary>
        public bool HasAnyValidPlacement(BlockShapeData shape)
        {
            if (shape == null)
            {
                return false;
            }

            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    if (CanPlaceShape(shape, x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 전달된 블록 목록 중 하나라도 놓을 수 있는지 검사합니다. (Deadlock 판정)
        /// </summary>
        public bool HasAnyValidMove(IEnumerable<BlockShapeData> shapes)
        {
            if (shapes == null)
            {
                return false;
            }

            foreach (var shape in shapes)
            {
                if (shape != null && HasAnyValidPlacement(shape))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Public Methods - Item Board Actions

        /// <summary>
        /// 3x3 범위 폭발을 실행합니다. (Bomb3x3 아이템)
        /// </summary>
        public int ClearArea3x3(int centerX, int centerY, out List<ItemType> triggeredItems)
        {
            triggeredItems = new List<ItemType>();
            var clearedList = new List<Vector2Int>();
            var itemsToTrigger = new List<(ItemType item, Vector2Int coord)>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int tx = centerX + dx;
                    int ty = centerY + dy;

                    if (IsValidCoordinate(tx, ty) && _board[tx, ty].IsOccupied)
                    {
                        if (_board[tx, ty].ItemType != ItemType.None)
                        {
                            triggeredItems.Add(_board[tx, ty].ItemType);
                            itemsToTrigger.Add((_board[tx, ty].ItemType, new Vector2Int(tx, ty)));
                        }

                        _board[tx, ty].Reset();
                        OnCellUpdated?.Invoke(tx, ty, Color.clear, ItemType.None);
                        clearedList.Add(new Vector2Int(tx, ty));
                    }
                }
            }

            if (clearedList.Count > 0)
            {
                OnLinesCleared?.Invoke(1, clearedList, triggeredItems);
            }

            foreach (var (item, coord) in itemsToTrigger)
            {
                OnItemTriggered?.Invoke(item, coord);
            }

            return clearedList.Count;
        }

        /// <summary>
        /// 가로 1줄 전체 폭발을 실행합니다. (HorizontalBlast 아이템)
        /// </summary>
        public int ClearRow(int targetY, out List<ItemType> triggeredItems)
        {
            triggeredItems = new List<ItemType>();
            var clearedList = new List<Vector2Int>();
            var itemsToTrigger = new List<(ItemType item, Vector2Int coord)>();

            if (!IsValidCoordinate(0, targetY))
            {
                return 0;
            }

            for (int x = 0; x < BoardSize; x++)
            {
                if (_board[x, targetY].IsOccupied)
                {
                    if (_board[x, targetY].ItemType != ItemType.None)
                    {
                        triggeredItems.Add(_board[x, targetY].ItemType);
                        itemsToTrigger.Add((_board[x, targetY].ItemType, new Vector2Int(x, targetY)));
                    }

                    _board[x, targetY].Reset();
                    OnCellUpdated?.Invoke(x, targetY, Color.clear, ItemType.None);
                    clearedList.Add(new Vector2Int(x, targetY));
                }
            }

            if (clearedList.Count > 0)
            {
                OnLinesCleared?.Invoke(1, clearedList, triggeredItems);
            }

            foreach (var (item, coord) in itemsToTrigger)
            {
                OnItemTriggered?.Invoke(item, coord);
            }

            return clearedList.Count;
        }

        /// <summary>
        /// 세로 1줄 전체 폭발을 실행합니다. (VerticalBlast 아이템)
        /// </summary>
        public int ClearColumn(int targetX, out List<ItemType> triggeredItems)
        {
            triggeredItems = new List<ItemType>();
            var clearedList = new List<Vector2Int>();
            var itemsToTrigger = new List<(ItemType item, Vector2Int coord)>();

            if (!IsValidCoordinate(targetX, 0))
            {
                return 0;
            }

            for (int y = 0; y < BoardSize; y++)
            {
                if (_board[targetX, y].IsOccupied)
                {
                    if (_board[targetX, y].ItemType != ItemType.None)
                    {
                        triggeredItems.Add(_board[targetX, y].ItemType);
                        itemsToTrigger.Add((_board[targetX, y].ItemType, new Vector2Int(targetX, y)));
                    }

                    _board[targetX, y].Reset();
                    OnCellUpdated?.Invoke(targetX, y, Color.clear, ItemType.None);
                    clearedList.Add(new Vector2Int(targetX, y));
                }
            }

            if (clearedList.Count > 0)
            {
                OnLinesCleared?.Invoke(1, clearedList, triggeredItems);
            }

            foreach (var (item, coord) in itemsToTrigger)
            {
                OnItemTriggered?.Invoke(item, coord);
            }

            return clearedList.Count;
        }

        /// <summary>
        /// 보드 전체 셀을 클리어합니다. (BoardClean 아이템)
        /// </summary>
        public int ClearAllBoard(out List<ItemType> triggeredItems)
        {
            triggeredItems = new List<ItemType>();
            var clearedList = new List<Vector2Int>();
            var itemsToTrigger = new List<(ItemType item, Vector2Int coord)>();

            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    if (_board[x, y].IsOccupied)
                    {
                        if (_board[x, y].ItemType != ItemType.None)
                        {
                            triggeredItems.Add(_board[x, y].ItemType);
                            itemsToTrigger.Add((_board[x, y].ItemType, new Vector2Int(x, y)));
                        }

                        _board[x, y].Reset();
                        OnCellUpdated?.Invoke(x, y, Color.clear, ItemType.None);
                        clearedList.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (clearedList.Count > 0)
            {
                OnLinesCleared?.Invoke(8, clearedList, triggeredItems);
            }

            foreach (var (item, coord) in itemsToTrigger)
            {
                OnItemTriggered?.Invoke(item, coord);
            }

            return clearedList.Count;
        }

        #endregion
    }
}
