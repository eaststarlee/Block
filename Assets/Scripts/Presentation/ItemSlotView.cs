using BlockBlast.Core;
using BlockBlast.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 상단 3개 아이템 인벤토리 슬롯의 개별 뷰 및 클릭 사용 인터랙션을 관리하는 클래스입니다.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Slot References")]
        [FormerlySerializedAs("slotIndex")]
        [SerializeField] private int _slotIndex;

        [FormerlySerializedAs("slotButton")]
        [SerializeField] private Button _slotButton;

        [FormerlySerializedAs("bgImage")]
        [SerializeField] private Image _bgImage;

        [FormerlySerializedAs("iconImage")]
        [SerializeField] private Image _iconImage;

        [Header("Color Settings")]
        [FormerlySerializedAs("emptyBgColor")]
        [SerializeField] private Color _emptyBgColor = new Color(0.2f, 0.25f, 0.2f, 0.85f);

        [FormerlySerializedAs("filledBgColor")]
        [SerializeField] private Color _filledBgColor = new Color(1f, 1f, 1f, 1f);

        #endregion

        #region Private Fields

        private ItemType _currentItem = ItemType.None;

        #endregion

        #region Public Properties

        public int SlotIndex => _slotIndex;
        public ItemType CurrentItem => _currentItem;

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
            if (_slotIndex == 0 && gameObject.name.Contains("_"))
            {
                string[] parts = gameObject.name.Split('_');
                if (parts.Length > 1 && int.TryParse(parts[1], out int idx))
                {
                    _slotIndex = idx;
                }
            }

            if (_slotButton == null)
            {
                _slotButton = GetComponent<Button>();
            }

            if (_bgImage == null)
            {
                _bgImage = GetComponent<Image>();
            }

            // 아이콘 렌더링용 Image 자동 탐색 및 생성
            if (_iconImage == null)
            {
                Transform t = transform.Find("IconImage");
                if (t == null)
                {
                    t = transform.Find("CustomIcon");
                }

                if (t == null)
                {
                    var iconObj = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(transform, false);
                    var rt = iconObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.12f, 0.12f);
                    rt.anchorMax = new Vector2(0.88f, 0.88f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    t = iconObj.transform;
                }

                _iconImage = t.GetComponent<Image>();
            }

            if (_iconImage != null)
            {
                _iconImage.raycastTarget = false;
                _iconImage.preserveAspect = true;
            }

            // 기존 텍스트 오브젝트가 있다면 비활성화
            Transform emojiT = transform.Find("Emoji");
            if (emojiT != null)
            {
                emojiT.gameObject.SetActive(false);
            }

            Transform labelT = transform.Find("Label");
            if (labelT != null)
            {
                labelT.gameObject.SetActive(false);
            }

            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveAllListeners();
                _slotButton.onClick.AddListener(OnClickSlot);
            }
        }

        /// <summary>
        /// 슬롯 인덱스를 지정하여 초기화합니다.
        /// </summary>
        public void Initialize(int index)
        {
            _slotIndex = index;
            SetupSelf();
            SetItem(ItemType.None);
        }

        /// <summary>
        /// 슬롯에 보관할 아이템을 바인딩하고 UI 비주얼을 갱신합니다.
        /// </summary>
        /// <param name="itemType">바인딩할 아이템 타입입니다.</param>
        public void SetItem(ItemType itemType)
        {
            SetupSelf();
            _currentItem = itemType;
            bool hasItem = itemType != ItemType.None;

            var theme = ThemeManager.Instance;
            Sprite customSprite = theme != null ? theme.GetItemSprite(itemType) : null;

            if (_bgImage != null)
            {
                if (theme != null && theme.ItemSlotBgSprite != null)
                {
                    _bgImage.sprite = theme.ItemSlotBgSprite;
                }

                _bgImage.color = hasItem ? _filledBgColor : _emptyBgColor;
            }

            if (_iconImage != null)
            {
                if (hasItem && customSprite != null)
                {
                    _iconImage.gameObject.SetActive(true);
                    _iconImage.sprite = customSprite;
                    _iconImage.color = Color.white;
                }
                else
                {
                    _iconImage.gameObject.SetActive(false);
                }
            }

            if (_slotButton != null)
            {
                _slotButton.interactable = hasItem;
            }
        }

        #endregion

        #region Private Event Handlers

        private void OnClickSlot()
        {
            if (_currentItem == ItemType.None || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.Items.UseInventoryItem(_slotIndex);
        }

        #endregion
    }
}
