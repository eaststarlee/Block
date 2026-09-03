using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 게임 내 모든 UI 요소에 피그마 커스텀 에셋(테마)을 일괄 공급하고 적용하는 매니저 클래스입니다.
    /// </summary>
    public sealed class ThemeManager : MonoBehaviour
    {
        #region Static Fields

        private static ThemeManager s_instance;

        #endregion

        #region Serialized Fields

        [Header("Theme Configuration")]
        [Tooltip("피그마 디자인 에셋이 등록된 ScriptableObject 파일입니다.")]
        [FormerlySerializedAs("themeConfig")]
        [SerializeField] private BlockBlastThemeConfig _themeConfig;

        #endregion

        #region Public Properties

        public static ThemeManager Instance => s_instance;

        public BlockBlastThemeConfig Theme => _themeConfig;

        public Sprite EmptyCellSprite => _themeConfig != null ? _themeConfig.EmptyCellSprite : null;
        public Sprite BoardFrameSprite => _themeConfig != null ? _themeConfig.BoardFrameSprite : null;
        public Sprite BlockTileSprite => _themeConfig != null ? _themeConfig.BlockTileSprite : null;
        public Sprite BackgroundSprite => _themeConfig != null ? _themeConfig.GameBackgroundSprite : null;
        public Sprite ItemSlotBgSprite => _themeConfig != null ? _themeConfig.ItemSlotBgSprite : null;
        public Sprite StarIconSprite => _themeConfig != null ? _themeConfig.StarIcon : null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
            }
            else if (s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 미할당 시 기본 테마 에셋 자동 로드
            if (_themeConfig == null)
            {
#if UNITY_EDITOR
                _themeConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<BlockBlastThemeConfig>("Assets/Design/BlockBlastTheme.asset");
#endif
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 새로운 테마 설정을 적용합니다.
        /// </summary>
        /// <param name="newTheme">적용할 테마 ScriptableObject입니다.</param>
        public void SetTheme(BlockBlastThemeConfig newTheme)
        {
            _themeConfig = newTheme;
        }

        /// <summary>
        /// 아이템 타입에 대응하는 테마 스프라이트를 조회합니다.
        /// </summary>
        /// <param name="itemType">조회할 아이템 타입입니다.</param>
        public Sprite GetItemSprite(ItemType itemType)
        {
            return _themeConfig != null ? _themeConfig.GetItemSprite(itemType) : null;
        }

        #endregion
    }
}
