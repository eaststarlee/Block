using System;
using System.Collections.Generic;
using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Gameplay
{
    /// <summary>
    /// 플레이어의 손패(3개 슬롯) 블록 생성, 소비 및 아이템 부착 확률을 관리하는 클래스입니다.
    /// </summary>
    public sealed class HandManager : MonoBehaviour
    {
        public const int HandSlotCount = 3;

        #region Serialized Fields

        [Header("Instant Item Spawn Rates")]
        [Tooltip("3x3 폭탄 블록 등장 확률 (0% ~ 100%)입니다.")]
        [Range(0f, 100f)]
        [FormerlySerializedAs("chanceBomb3x3")]
        [SerializeField] private float _chanceBomb3x3 = 5f;

        [Tooltip("가로 폭발 블록 등장 확률 (0% ~ 100%)입니다.")]
        [Range(0f, 100f)]
        [FormerlySerializedAs("chanceHorizontalBlast")]
        [SerializeField] private float _chanceHorizontalBlast = 5f;

        [Tooltip("세로 폭발 블록 등장 확률 (0% ~ 100%)입니다.")]
        [Range(0f, 100f)]
        [FormerlySerializedAs("chanceVerticalBlast")]
        [SerializeField] private float _chanceVerticalBlast = 5f;

        [Tooltip("시간 추가 (+10초) 블록 등장 확률 (0% ~ 100%)입니다.")]
        [Range(0f, 100f)]
        [FormerlySerializedAs("chanceTimeBonus10s")]
        [SerializeField] private float _chanceTimeBonus10s = 5f;

        [Header("Inventory Item Spawn Rates")]
        [Tooltip("보드 클리어 (전체 판 정리) 등장 확률 (0% ~ 100%)입니다.")]
        [Range(0f, 100f)]
        [FormerlySerializedAs("chanceBoardClean")]
        [SerializeField] private float _chanceBoardClean = 2f;

        [Tooltip("손패 리셋 등장 확률 (0% ~ 100%)입니다.")]
        [Range(0f, 100f)]
        [FormerlySerializedAs("chanceHandReset")]
        [SerializeField] private float _chanceHandReset = 5f;

        [Tooltip("점수 2배 버프 (10초) 등장 확률 (0% ~ 100%)입니다.")]
        [Range(0f, 100f)]
        [FormerlySerializedAs("chanceScoreDouble10s")]
        [SerializeField] private float _chanceScoreDouble10s = 5f;

        #endregion

        #region Private Fields

        private readonly BlockShapeData[] _handSlots = new BlockShapeData[HandSlotCount];

        #endregion

        #region Events

        public event Action<int, BlockShapeData> OnHandSlotUpdated; // (slotIndex, shapeData)
        public event Action<int> OnHandSlotConsumed; // slotIndex
        public event Action OnHandRefilled;

        #endregion

        #region Public Properties

        public IReadOnlyList<BlockShapeData> HandSlots => _handSlots;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ShapeCatalog.InitializeCatalog();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 손패 3개 슬롯에 새로운 블록을 생성하여 충당합니다.
        /// </summary>
        public void ReplenishHand()
        {
            for (int i = 0; i < HandSlotCount; i++)
            {
                BlockShapeData shape = ShapeCatalog.GetRandomShape();

                // 개별 아이템 확률(%)에 따라 아이템 부착 여부 결정
                ItemType rolledItem = RollItemType();
                if (rolledItem != ItemType.None)
                {
                    shape.AttachItemRandomly(rolledItem);
                }

                _handSlots[i] = shape;
                OnHandSlotUpdated?.Invoke(i, shape);
            }

            OnHandRefilled?.Invoke();
        }

        /// <summary>
        /// 특정 슬롯의 블록을 소비(배치 완료) 처리합니다. 모든 슬롯이 소진되면 자동으로 다시 충당됩니다.
        /// </summary>
        /// <param name="slotIndex">소비할 슬롯 번호(0~2)입니다.</param>
        public void ConsumeSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= HandSlotCount)
            {
                return;
            }

            _handSlots[slotIndex] = null;
            OnHandSlotConsumed?.Invoke(slotIndex);
            OnHandSlotUpdated?.Invoke(slotIndex, null);

            // 3개 슬롯이 모두 소진되었는지 확인
            if (IsAllSlotsEmpty())
            {
                ReplenishHand();
            }
        }

        /// <summary>
        /// 손패 블록 3개를 즉시 새로운 모양으로 리셋합니다. (HandReset 아이템 사용 시)
        /// </summary>
        public void ResetHand()
        {
            ReplenishHand();
        }

        /// <summary>
        /// 현재 모든 손패 슬롯이 비어있는지 확인합니다.
        /// </summary>
        public bool IsAllSlotsEmpty()
        {
            for (int i = 0; i < HandSlotCount; i++)
            {
                if (_handSlots[i] != null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 현재 남아있는 유효한 블록 모양 목록을 반환합니다. (Deadlock 판정용)
        /// </summary>
        public List<BlockShapeData> GetRemainingShapes()
        {
            var list = new List<BlockShapeData>();
            for (int i = 0; i < HandSlotCount; i++)
            {
                if (_handSlots[i] != null)
                {
                    list.Add(_handSlots[i]);
                }
            }

            return list;
        }

        /// <summary>
        /// 설정된 개별 확률(%)에 따라 아이템 타입을 추첨합니다.
        /// </summary>
        public ItemType RollItemType()
        {
            float roll = UnityEngine.Random.Range(0f, 100f);
            float current = 0f;

            current += _chanceBomb3x3;
            if (roll < current)
            {
                return ItemType.Bomb3x3;
            }

            current += _chanceHorizontalBlast;
            if (roll < current)
            {
                return ItemType.HorizontalBlast;
            }

            current += _chanceVerticalBlast;
            if (roll < current)
            {
                return ItemType.VerticalBlast;
            }

            current += _chanceTimeBonus10s;
            if (roll < current)
            {
                return ItemType.TimeBonus10s;
            }

            current += _chanceBoardClean;
            if (roll < current)
            {
                return ItemType.BoardClean;
            }

            current += _chanceHandReset;
            if (roll < current)
            {
                return ItemType.HandReset;
            }

            current += _chanceScoreDouble10s;
            if (roll < current)
            {
                return ItemType.ScoreDouble10s;
            }

            return ItemType.None; // 일반 블록
        }

        #endregion
    }
}
