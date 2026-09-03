using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Core
{
    /// <summary>
    /// 개별 블록의 형태, 크기, 색상 및 내장된 아이템 정보를 관리하는 데이터 모델 클래스입니다.
    /// </summary>
    [Serializable]
    public sealed class BlockShapeData
    {
        #region Serialized Fields

        [Header("Shape Information")]
        [Tooltip("모양 식별자 ID입니다.")]
        [FormerlySerializedAs("shapeId")]
        [SerializeField] private string _shapeId;

        [Tooltip("블록의 가로 그리드 칸 수입니다.")]
        [FormerlySerializedAs("width")]
        [SerializeField] private int _width;

        [Tooltip("블록의 세로 그리드 칸 수입니다.")]
        [FormerlySerializedAs("height")]
        [SerializeField] private int _height;

        [Tooltip("블록을 구성하는 로컬 셀 좌표 목록입니다.")]
        [FormerlySerializedAs("cells")]
        [SerializeField] private List<Vector2Int> _cells = new List<Vector2Int>();

        [Tooltip("블록의 기본 렌더링 색상입니다.")]
        [FormerlySerializedAs("blockColor")]
        [SerializeField] private Color _blockColor = new Color(0.2f, 0.6f, 1f, 1f);

        [Header("Item Information")]
        [Tooltip("블록 내 셀에 내장된 아이템 종류입니다.")]
        [FormerlySerializedAs("embeddedItem")]
        [SerializeField] private ItemType _embeddedItem = ItemType.None;

        [Tooltip("아이템이 위치한 블록 내 로컬 셀 좌표입니다.")]
        [FormerlySerializedAs("itemCellPosition")]
        [SerializeField] private Vector2Int _itemCellPosition = new Vector2Int(-1, -1);

        #endregion

        #region Public Properties

        public string ShapeId => _shapeId;
        public int Width => _width;
        public int Height => _height;
        public IReadOnlyList<Vector2Int> Cells => _cells;
        public Color BlockColor => _blockColor;
        public ItemType EmbeddedItem => _embeddedItem;
        public Vector2Int ItemCellPosition => _itemCellPosition;
        public bool HasItem => _embeddedItem != ItemType.None && _itemCellPosition.x >= 0;

        #endregion

        #region Constructors

        public BlockShapeData()
        {
        }

        public BlockShapeData(string id, int width, int height, IEnumerable<Vector2Int> cellCoords, Color color)
        {
            _shapeId = id;
            _width = width;
            _height = height;
            _cells = new List<Vector2Int>(cellCoords);
            _blockColor = color;
            _embeddedItem = ItemType.None;
            _itemCellPosition = new Vector2Int(-1, -1);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 현재 블록 데이터의 독립적인 복제본(Deep Copy)을 생성합니다.
        /// </summary>
        public BlockShapeData Clone()
        {
            var clone = new BlockShapeData(_shapeId, _width, _height, _cells, _blockColor)
            {
                _embeddedItem = this._embeddedItem,
                _itemCellPosition = this._itemCellPosition
            };
            return clone;
        }

        /// <summary>
        /// 블록 내 특정 로컬 셀 좌표에 아이템을 부착합니다.
        /// </summary>
        /// <param name="itemType">부착할 아이템 타입입니다.</param>
        /// <param name="cellPos">아이템이 위치할 셀 좌표입니다.</param>
        public void AttachItem(ItemType itemType, Vector2Int cellPos)
        {
            _embeddedItem = itemType;
            _itemCellPosition = cellPos;
        }

        /// <summary>
        /// 블록을 구성하는 셀 중 하나를 무작위로 선택하여 아이템을 부착합니다.
        /// </summary>
        /// <param name="itemType">부착할 아이템 타입입니다.</param>
        public void AttachItemRandomly(ItemType itemType)
        {
            if (_cells.Count == 0 || itemType == ItemType.None)
            {
                _embeddedItem = ItemType.None;
                _itemCellPosition = new Vector2Int(-1, -1);
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, _cells.Count);
            _embeddedItem = itemType;
            _itemCellPosition = _cells[randomIndex];
        }

        /// <summary>
        /// 지정한 로컬 좌표에 아이템이 존재하는지 확인합니다.
        /// </summary>
        /// <param name="x">로컬 X 좌표입니다.</param>
        /// <param name="y">로컬 Y 좌표입니다.</param>
        public bool IsItemAt(int x, int y)
        {
            return HasItem && _itemCellPosition.x == x && _itemCellPosition.y == y;
        }

        #endregion
    }
}
