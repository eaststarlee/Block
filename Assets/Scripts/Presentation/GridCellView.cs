using System.Collections;
using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 8x8 보드의 개별 셀 UI 렌더링, 테마 스프라이트 바인딩 및 파괴 애니메이션을 관리하는 클래스입니다.
    /// </summary>
    public sealed class GridCellView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI Elements")]
        [FormerlySerializedAs("bgImage")]
        [SerializeField] private Image _bgImage;

        [FormerlySerializedAs("fillImage")]
        [SerializeField] private Image _fillImage;

        [FormerlySerializedAs("highlightImage")]
        [SerializeField] private Image _highlightImage;

        [FormerlySerializedAs("itemIconImage")]
        [SerializeField] private Image _itemIconImage;

        [FormerlySerializedAs("itemEmojiText")]
        [SerializeField] private Text _itemEmojiText;

        [Header("Color Settings")]
        [FormerlySerializedAs("emptyCellColor")]
        [SerializeField] private Color _emptyCellColor = new Color(0.82f, 0.82f, 0.84f, 1f);

        [SerializeField] private int _gridX = -1;
        [SerializeField] private int _gridY = -1;

        #endregion

        #region Private Fields

        private bool _isOccupied;

        #endregion

        #region Public Properties

        public int GridX
        {
            get
            {
                if (_gridX < 0)
                {
                    SetupSelf();
                }

                return _gridX;
            }
        }

        public int GridY
        {
            get
            {
                if (_gridY < 0)
                {
                    SetupSelf();
                }

                return _gridY;
            }
        }

        public bool IsOccupied => _isOccupied;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSelf();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 컴포넌트 내부 계층 구조 및 좌표를 스스로 탐색하여 초기 설정합니다.
        /// </summary>
        public void SetupSelf()
        {
            // GameObject 이름(Cell_X_Y)에서 항상 정확한 좌표 파싱
            string objName = gameObject.name;
            if (objName.StartsWith("Cell_"))
            {
                string[] parts = objName.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int px) && int.TryParse(parts[2], out int py))
                {
                    _gridX = px;
                    _gridY = py;
                }
            }

            if (_bgImage == null)
            {
                _bgImage = GetComponent<Image>();
            }

            if (_fillImage == null)
            {
                Transform t = transform.Find("Fill");
                if (t != null)
                {
                    _fillImage = t.GetComponent<Image>();
                }
            }

            if (_highlightImage == null)
            {
                Transform t = transform.Find("Highlight");
                if (t != null)
                {
                    _highlightImage = t.GetComponent<Image>();
                }
            }

            if (_itemIconImage == null)
            {
                Transform t = transform.Find("ItemIcon");
                if (t != null)
                {
                    _itemIconImage = t.GetComponent<Image>();
                }
            }

            if (_itemEmojiText == null)
            {
                Transform t = transform.Find("ItemEmoji");
                if (t != null)
                {
                    _itemEmojiText = t.GetComponent<Text>();
                }
            }
        }

        /// <summary>
        /// 좌표를 지정하여 셀을 초기화합니다.
        /// </summary>
        public void Initialize(int x, int y)
        {
            _gridX = x;
            _gridY = y;
            SetupSelf();
            SetState(false, Color.clear, ItemType.None);
            SetHighlight(false, Color.clear);
        }

        /// <summary>
        /// 셀의 점유 상태, 색상 및 아이템 표시 상태를 갱신합니다.
        /// </summary>
        public void SetState(bool occupied, Color color, ItemType itemType)
        {
            _isOccupied = occupied;
            SetupSelf();

            var theme = ThemeManager.Instance;

            if (_bgImage != null)
            {
                if (theme != null && theme.EmptyCellSprite != null)
                {
                    _bgImage.sprite = theme.EmptyCellSprite;
                    _bgImage.color = Color.white;
                }
                else
                {
                    _bgImage.color = _emptyCellColor;
                }
            }

            Sprite customItemSprite = theme != null ? theme.GetItemSprite(itemType) : null;
            Sprite blockTileSprite = theme != null ? theme.BlockTileSprite : null;

            if (_fillImage != null)
            {
                _fillImage.gameObject.SetActive(occupied);
                if (occupied)
                {
                    if (itemType != ItemType.None && customItemSprite != null)
                    {
                        // 아이템 셀인 경우 피그마 아이템 스프라이트 직접 렌더링
                        _fillImage.sprite = customItemSprite;
                        _fillImage.color = Color.white;
                    }
                    else if (blockTileSprite != null)
                    {
                        // 일반 셀인 경우 피그마 블록 타일 스프라이트 렌더링
                        _fillImage.sprite = blockTileSprite;
                        _fillImage.color = Color.white;
                    }
                    else
                    {
                        _fillImage.sprite = null;
                        _fillImage.color = color;
                    }
                }
            }

            // 하위 아이콘 이미지/이모지 텍스트 오버레이 (피그마 스프라이트가 없을 때의 보조 수단)
            if (occupied && itemType != ItemType.None)
            {
                if (customItemSprite != null)
                {
                    if (_itemIconImage != null)
                    {
                        _itemIconImage.gameObject.SetActive(false);
                    }

                    if (_itemEmojiText != null)
                    {
                        _itemEmojiText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (_itemIconImage != null)
                    {
                        _itemIconImage.gameObject.SetActive(false);
                    }

                    if (_itemEmojiText != null)
                    {
                        _itemEmojiText.gameObject.SetActive(true);
                        _itemEmojiText.text = itemType.GetItemIconEmoji();
                    }
                }
            }
            else
            {
                if (_itemIconImage != null)
                {
                    _itemIconImage.gameObject.SetActive(false);
                }

                if (_itemEmojiText != null)
                {
                    _itemEmojiText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 드래그 호버 시 프리뷰 하이라이트 색상을 표시하거나 숨깁니다.
        /// </summary>
        public void SetHighlight(bool enable, Color previewColor)
        {
            SetupSelf();
            if (_highlightImage != null)
            {
                _highlightImage.gameObject.SetActive(enable);
                if (enable)
                {
                    _highlightImage.color = previewColor;
                }
            }
        }

        /// <summary>
        /// 라인 Blast 시 축소/소멸 연출 애니메이션을 실행합니다.
        /// </summary>
        public void PlayBlastEffect()
        {
            StartCoroutine(BlastRoutine());
        }

        #endregion

        #region Private Routines

        private IEnumerator BlastRoutine()
        {
            Transform t = transform;
            Vector3 originalScale = t.localScale;

            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(1.2f, 0f, progress);
                t.localScale = originalScale * scale;
                yield return null;
            }

            t.localScale = originalScale;
            SetState(false, Color.clear, ItemType.None);
        }

        #endregion
    }
}
