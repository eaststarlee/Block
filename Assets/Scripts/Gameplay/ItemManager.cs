using System;
using System.Collections.Generic;
using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Gameplay
{
    /// <summary>
    /// 즉발형 연쇄 폭발 큐 처리 및 3슬롯 인벤토리 수동 아이템 사용을 관리하는 클래스입니다.
    /// </summary>
    public sealed class ItemManager : MonoBehaviour
    {
        public const int MaxInventorySlots = 3;

        #region Serialized Fields

        [Header("Manager References")]
        [Tooltip("보드 조작 관리자 참조입니다.")]
        [FormerlySerializedAs("gridManager")]
        [SerializeField] private GridManager _gridManager;

        [Tooltip("손패 조작 관리자 참조입니다.")]
        [FormerlySerializedAs("handManager")]
        [SerializeField] private HandManager _handManager;

        [Tooltip("점수 관리자 참조입니다.")]
        [FormerlySerializedAs("scoreManager")]
        [SerializeField] private ScoreManager _scoreManager;

        [Tooltip("타이머 관리자 참조입니다.")]
        [FormerlySerializedAs("timeManager")]
        [SerializeField] private TimeManager _timeManager;

        #endregion

        #region Private Fields

        private readonly List<ItemType> _inventory = new List<ItemType>(MaxInventorySlots);
        private readonly Queue<(ItemType itemType, Vector2Int coord)> _pendingInstantItems = new Queue<(ItemType, Vector2Int)>();
        private bool _isProcessingInstantItems;

        #endregion

        #region Events

        public event Action<IReadOnlyList<ItemType>> OnInventoryUpdated;
        public event Action<ItemType> OnItemEvaporated; // 슬롯 가득 찬 상태에서 추가 획득 시 증발
        public event Action<ItemType, string> OnInstantItemTriggered; // (itemType, noticeMessage)
        public event Action<ItemType> OnInventoryItemUsed;
        public event Action<string> OnNoticeAnnouncement; // 하단 멘트 안내

        #endregion

        #region Public Properties

        public IReadOnlyList<ItemType> Inventory => _inventory;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_gridManager == null)
            {
                _gridManager = UnityEngine.Object.FindAnyObjectByType<GridManager>();
            }

            if (_handManager == null)
            {
                _handManager = UnityEngine.Object.FindAnyObjectByType<HandManager>();
            }

            if (_scoreManager == null)
            {
                _scoreManager = UnityEngine.Object.FindAnyObjectByType<ScoreManager>();
            }

            if (_timeManager == null)
            {
                _timeManager = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
            }

            if (_gridManager != null)
            {
                _gridManager.OnItemTriggered -= HandleItemTriggeredFromGrid;
                _gridManager.OnItemTriggered += HandleItemTriggeredFromGrid;
            }
        }

        private void OnDestroy()
        {
            if (_gridManager != null)
            {
                _gridManager.OnItemTriggered -= HandleItemTriggeredFromGrid;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 하위 매니저 참조를 주입하여 초기화합니다.
        /// </summary>
        public void Initialize(GridManager grid, HandManager hand, ScoreManager score, TimeManager timer)
        {
            _gridManager = grid;
            _handManager = hand;
            _scoreManager = score;
            _timeManager = timer;

            if (_gridManager != null)
            {
                _gridManager.OnItemTriggered -= HandleItemTriggeredFromGrid;
                _gridManager.OnItemTriggered += HandleItemTriggeredFromGrid;
            }
        }

        /// <summary>
        /// 인벤토리 및 대기 중인 즉발형 아이템 큐를 완전히 초기화합니다.
        /// </summary>
        public void ResetInventory()
        {
            _inventory.Clear();
            _pendingInstantItems.Clear();
            _isProcessingInstantItems = false;
            OnInventoryUpdated?.Invoke(_inventory);
        }

        /// <summary>
        /// 보드에서 라인 파괴 시 아이템 셀이 감지되었을 때 호출됩니다.
        /// </summary>
        /// <param name="itemType">감지된 아이템 타입입니다.</param>
        /// <param name="cellCoord">발생한 그리드 셀 좌표입니다.</param>
        public void HandleItemTriggeredFromGrid(ItemType itemType, Vector2Int cellCoord)
        {
            if (itemType == ItemType.None)
            {
                return;
            }

            ItemCategory category = itemType.GetCategory();

            if (category == ItemCategory.Instant)
            {
                // 1. 즉발형 아이템: 큐에 삽입 후 순차적 비재귀 실행 (스택오버플로우 방지)
                _pendingInstantItems.Enqueue((itemType, cellCoord));
                ProcessPendingInstantItems();
            }
            else if (category == ItemCategory.Inventory)
            {
                // 2. 인벤토리형 아이템: 인벤토리에 추가 (3칸 초과 시 증발)
                AddInventoryItem(itemType);
            }
        }

        /// <summary>
        /// 인벤토리에 아이템을 추가합니다. (최대 3개, 꽉 찼을 경우 증발 알림)
        /// </summary>
        /// <param name="itemType">추가할 아이템 타입입니다.</param>
        public void AddInventoryItem(ItemType itemType)
        {
            if (_inventory.Count < MaxInventorySlots)
            {
                _inventory.Add(itemType);
                OnInventoryUpdated?.Invoke(_inventory);

                string msg = $"{itemType.GetItemName()} Get Item!";
                OnNoticeAnnouncement?.Invoke(msg);
            }
            else
            {
                // 3개 슬롯 초과 -> 증발 처리
                OnItemEvaporated?.Invoke(itemType);
                string msg = $"Inventory Over! [{itemType.GetItemName()}] Item Loss!";
                OnNoticeAnnouncement?.Invoke(msg);
            }
        }

        /// <summary>
        /// 지정한 인벤토리 슬롯의 아이템을 수동으로 사용합니다.
        /// </summary>
        /// <param name="slotIndex">사용할 인벤토리 슬롯 인덱스(0~2)입니다.</param>
        /// <returns>사용 성공 여부를 반환합니다.</returns>
        public bool UseInventoryItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _inventory.Count)
            {
                return false;
            }

            ItemType itemToUse = _inventory[slotIndex];
            _inventory.RemoveAt(slotIndex);
            OnInventoryUpdated?.Invoke(_inventory);

            ExecuteInventoryItem(itemToUse);
            OnInventoryItemUsed?.Invoke(itemToUse);

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 대기 큐에 쌓인 즉발형 아이템들을 순차적으로 실행합니다. (연쇄 폭발 안전 처리)
        /// </summary>
        private void ProcessPendingInstantItems()
        {
            if (_isProcessingInstantItems)
            {
                return;
            }

            _isProcessingInstantItems = true;
            try
            {
                while (_pendingInstantItems.Count > 0)
                {
                    var (itemType, coord) = _pendingInstantItems.Dequeue();
                    ExecuteInstantItem(itemType, coord);
                }
            }
            finally
            {
                _isProcessingInstantItems = false;
            }
        }

        /// <summary>
        /// 즉발형 아이템 효과를 실행합니다.
        /// </summary>
        private void ExecuteInstantItem(ItemType itemType, Vector2Int coord)
        {
            string message = string.Empty;

            switch (itemType)
            {
                case ItemType.Bomb3x3:
                    message = "3x3 Boom!!";
                    if (_gridManager != null)
                    {
                        _gridManager.ClearArea3x3(coord.x, coord.y, out _);
                    }
                    break;

                case ItemType.HorizontalBlast:
                    message = "Horizontal Boom!!";
                    if (_gridManager != null)
                    {
                        _gridManager.ClearRow(coord.y, out _);
                    }
                    break;

                case ItemType.VerticalBlast:
                    message = "Vertical Boom!!";
                    if (_gridManager != null)
                    {
                        _gridManager.ClearColumn(coord.x, out _);
                    }
                    break;

                case ItemType.TimeBonus10s:
                    message = "+10s Time !!";
                    if (_timeManager != null)
                    {
                        _timeManager.AddTime(10f);
                    }
                    break;
            }

            OnInstantItemTriggered?.Invoke(itemType, message);
            OnNoticeAnnouncement?.Invoke(message);
        }

        /// <summary>
        /// 인벤토리 아이템 효과를 실행합니다.
        /// </summary>
        private void ExecuteInventoryItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.BoardClean:
                    if (_gridManager != null)
                    {
                        _gridManager.ClearAllBoard(out _);
                    }
                    OnNoticeAnnouncement?.Invoke("Board ALL Clear!");
                    break;

                case ItemType.HandReset:
                    if (_handManager != null)
                    {
                        _handManager.ResetHand();
                    }
                    OnNoticeAnnouncement?.Invoke("Block Reset!");
                    break;

                case ItemType.ScoreDouble10s:
                    if (_scoreManager != null)
                    {
                        _scoreManager.ActivateDoubleScore(10f);
                    }
                    OnNoticeAnnouncement?.Invoke("Point x2 - 10s ");
                    break;
            }
        }

        #endregion
    }
}
