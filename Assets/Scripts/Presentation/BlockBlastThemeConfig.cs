using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 피그마(Figma)에서 추출한 비주얼 스프라이트 에셋들을 등록하고 관리하는 ScriptableObject 테마 설정 클래스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BlockBlastTheme", menuName = "Block Blast/Theme Config", order = 1)]
    public sealed class BlockBlastThemeConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Board And Cell Sprites")]
        [Tooltip("8x8 보드의 빈 셀 스프라이트입니다.")]
        [FormerlySerializedAs("emptyCellSprite")]
        [SerializeField] private Sprite _emptyCellSprite;

        [Tooltip("8x8 보드 전체 배경 프레임 스프라이트입니다.")]
        [FormerlySerializedAs("boardFrameSprite")]
        [SerializeField] private Sprite _boardFrameSprite;

        [Tooltip("전체 게임 화면 배경 이미지 스프라이트입니다.")]
        [FormerlySerializedAs("gameBackgroundSprite")]
        [SerializeField] private Sprite _gameBackgroundSprite;

        [Header("Block Tile Sprites")]
        [Tooltip("블록을 구성하는 개별 타일 스프라이트입니다. (흰색 베이스 권장 - 컬러 자동 틴팅)")]
        [FormerlySerializedAs("blockTileSprite")]
        [SerializeField] private Sprite _blockTileSprite;

        [Header("Instant Item Icons")]
        [FormerlySerializedAs("bomb3x3Icon")]
        [SerializeField] private Sprite _bomb3x3Icon;

        [FormerlySerializedAs("horizontalBlastIcon")]
        [SerializeField] private Sprite _horizontalBlastIcon;

        [FormerlySerializedAs("verticalBlastIcon")]
        [SerializeField] private Sprite _verticalBlastIcon;

        [FormerlySerializedAs("timeBonus10sIcon")]
        [SerializeField] private Sprite _timeBonus10sIcon;

        [Header("Inventory Item Icons")]
        [FormerlySerializedAs("boardCleanIcon")]
        [SerializeField] private Sprite _boardCleanIcon;

        [FormerlySerializedAs("handResetIcon")]
        [SerializeField] private Sprite _handResetIcon;

        [FormerlySerializedAs("scoreDouble10sIcon")]
        [SerializeField] private Sprite _scoreDouble10sIcon;

        [Header("UI Sprites")]
        [FormerlySerializedAs("starIcon")]
        [SerializeField] private Sprite _starIcon;

        [FormerlySerializedAs("settingsIcon")]
        [SerializeField] private Sprite _settingsIcon;

        [FormerlySerializedAs("itemSlotBgSprite")]
        [SerializeField] private Sprite _itemSlotBgSprite;

        #endregion

        #region Public Properties

        public Sprite EmptyCellSprite => _emptyCellSprite;
        public Sprite BoardFrameSprite => _boardFrameSprite;
        public Sprite GameBackgroundSprite => _gameBackgroundSprite;
        public Sprite BlockTileSprite => _blockTileSprite;
        public Sprite Bomb3x3Icon => _bomb3x3Icon;
        public Sprite HorizontalBlastIcon => _horizontalBlastIcon;
        public Sprite VerticalBlastIcon => _verticalBlastIcon;
        public Sprite TimeBonus10sIcon => _timeBonus10sIcon;
        public Sprite BoardCleanIcon => _boardCleanIcon;
        public Sprite HandResetIcon => _handResetIcon;
        public Sprite ScoreDouble10sIcon => _scoreDouble10sIcon;
        public Sprite StarIcon => _starIcon;
        public Sprite SettingsIcon => _settingsIcon;
        public Sprite ItemSlotBgSprite => _itemSlotBgSprite;

        #endregion

        #region Public Methods

        /// <summary>
        /// 지정한 아이템 타입에 대응하는 테마 스프라이트 에셋을 반환합니다.
        /// </summary>
        /// <param name="itemType">조회할 아이템 타입입니다.</param>
        public Sprite GetItemSprite(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Bomb3x3:
                    return _bomb3x3Icon;

                case ItemType.HorizontalBlast:
                    return _horizontalBlastIcon;

                case ItemType.VerticalBlast:
                    return _verticalBlastIcon;

                case ItemType.TimeBonus10s:
                    return _timeBonus10sIcon;

                case ItemType.BoardClean:
                    return _boardCleanIcon;

                case ItemType.HandReset:
                    return _handResetIcon;

                case ItemType.ScoreDouble10s:
                    return _scoreDouble10sIcon;

                default:
                    return null;
            }
        }

        #endregion
    }
}
