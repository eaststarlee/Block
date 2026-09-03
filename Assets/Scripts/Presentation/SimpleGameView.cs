using System.Collections;
using System.Collections.Generic;
using BlockBlast.Core;
using BlockBlast.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 메인 게임 UI 화면 전체(점수, 타이머, 아이템 인벤토리, 배경 색상 전환, 안내 배너, 게임오버 팝업)를 총괄 제어하는 뷰 클래스입니다.
    /// </summary>
    public sealed class SimpleGameView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Background Settings")]
        [FormerlySerializedAs("backgroundImage")]
        [SerializeField] private Image _backgroundImage;

        [FormerlySerializedAs("mainCamera")]
        [SerializeField] private Camera _mainCamera;

        [Tooltip("게임 시작(또는 재시작)할 때마다 이 팔레트 중 하나가 무작위로 배경색으로 지정됩니다.")]
        [FormerlySerializedAs("backgroundPalette")]
        [SerializeField] private Color[] _backgroundPalette = new Color[]
        {
            new Color(0.18f, 0.22f, 0.28f, 1f), // 1. 차콜 네이비
            new Color(0.24f, 0.18f, 0.28f, 1f), // 2. 다크 바이올렛
            new Color(0.16f, 0.26f, 0.24f, 1f), // 3. 딥 에메랄드
            new Color(0.28f, 0.20f, 0.18f, 1f), // 4. 웜 다크 브라운
            new Color(0.20f, 0.20f, 0.24f, 1f), // 5. 슬레이트 블루
            new Color(0.28f, 0.16f, 0.22f, 1f), // 6. 딥 와인
            new Color(0.29f, 0.35f, 0.22f, 1f)  // 7. 올리브 카키 (기본 디자인 색상)
        };

        [Header("UI References")]
        [FormerlySerializedAs("highScoreText")]
        [SerializeField] private Text _highScoreText;

        [FormerlySerializedAs("currentScoreText")]
        [SerializeField] private Text _currentScoreText;

        [FormerlySerializedAs("timerText")]
        [SerializeField] private Text _timerText;

        [FormerlySerializedAs("doubleScoreBadge")]
        [SerializeField] private GameObject _doubleScoreBadge;

        [Header("Inventory Slots")]
        [FormerlySerializedAs("itemSlots")]
        [SerializeField] private ItemSlotView[] _itemSlots = new ItemSlotView[3];

        [Header("Board And Hand")]
        [FormerlySerializedAs("boardView")]
        [SerializeField] private GridBoardView _boardView;

        [FormerlySerializedAs("handSlots")]
        [SerializeField] private HandBlockView[] _handSlots = new HandBlockView[3];

        [Header("Notice Banner")]
        [FormerlySerializedAs("noticeBannerText")]
        [SerializeField] private Text _noticeBannerText;

        [FormerlySerializedAs("noticeCanvasGroup")]
        [SerializeField] private CanvasGroup _noticeCanvasGroup;

        [Header("Game Over Popup")]
        [FormerlySerializedAs("gameOverPanel")]
        [SerializeField] private GameObject _gameOverPanel;

        [FormerlySerializedAs("gameOverReasonText")]
        [SerializeField] private Text _gameOverReasonText;

        [FormerlySerializedAs("gameOverFinalScoreText")]
        [SerializeField] private Text _gameOverFinalScoreText;

        [FormerlySerializedAs("restartButton")]
        [SerializeField] private Button _restartButton;

        #endregion

        #region Private Fields

        private Coroutine _noticeRoutine;
        private int _lastColorIndex = -1;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            // 배경 Image 자동 탐색
            if (_backgroundImage == null)
            {
                Transform bgT = transform.Find("BackgroundPanel");
                if (bgT == null)
                {
                    bgT = transform.Find("Background");
                }

                if (bgT != null)
                {
                    _backgroundImage = bgT.GetComponent<Image>();
                }

                if (_backgroundImage == null)
                {
                    var foundBgs = GetComponentsInChildren<Image>(true);
                    foreach (var img in foundBgs)
                    {
                        if (img.gameObject.name.ToLower().Contains("background"))
                            break;
                    }
                }
            }

            // 손패 슬롯 SlotIndex 기준 0, 1, 2 매핑
            var foundHandSlots = GetComponentsInChildren<HandBlockView>(true);
            _handSlots = new HandBlockView[3];
            foreach (var slot in foundHandSlots)
            {
                if (slot != null && slot.SlotIndex >= 0 && slot.SlotIndex < 3)
                {
                    _handSlots[slot.SlotIndex] = slot;
                }
            }

            // 인벤토리 슬롯 SlotIndex 기준 0, 1, 2 매핑
            var foundItemSlots = GetComponentsInChildren<ItemSlotView>(true);
            _itemSlots = new ItemSlotView[3];
            foreach (var slot in foundItemSlots)
            {
                if (slot != null && slot.SlotIndex >= 0 && slot.SlotIndex < 3)
                {
                    _itemSlots[slot.SlotIndex] = slot;
                }
            }

            if (_boardView == null)
            {
                _boardView = GetComponentInChildren<GridBoardView>(true);
            }
        }

        private void Start()
        {
            ApplyRandomBackgroundColor();

            if (GameManager.Instance != null)
            {
                BindEvents();
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnClickRestart);
            }

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            SetNoticeText("블록을 맞춰 라인을 파괴하고\n아이템을 획득하세요!", 3f);
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 템플릿 팔레트에서 무작위 배경색을 선택하여 UI 배경 및 카메라 배경에 100% 동기화 적용합니다.
        /// </summary>
        public void ApplyRandomBackgroundColor()
        {
            if (_backgroundPalette == null || _backgroundPalette.Length == 0)
            {
                return;
            }

            int newIndex = _lastColorIndex;
            if (_backgroundPalette.Length > 1)
            {
                while (newIndex == _lastColorIndex)
                {
                    newIndex = UnityEngine.Random.Range(0, _backgroundPalette.Length);
                }
            }
            else
            {
                newIndex = 0;
            }

            _lastColorIndex = newIndex;
            Color chosenColor = _backgroundPalette[newIndex];

            // 1. UI BackgroundPanel 색상 적용 및 풀스크린 오프셋 확장
            if (_backgroundImage != null)
            {
                _backgroundImage.color = chosenColor;

                RectTransform rt = _backgroundImage.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(-1000f, -1000f);
                    rt.offsetMax = new Vector2(1000f, 1000f);
                }
            }

            // 2. 메인 카메라 배경색도 100% 동일하게 동기화 (외곽 여백 잘림 완벽 해결)
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera != null)
            {
                _mainCamera.backgroundColor = chosenColor;
            }
        }

        /// <summary>
        /// 하단 안내 배너에 텍스트를 표시하고 일정 시간 후 페이드아웃합니다.
        /// </summary>
        /// <param name="text">표시할 안내 메시지입니다.</param>
        /// <param name="duration">유지 시간(초)입니다.</param>
        public void SetNoticeText(string text, float duration = 2.5f)
        {
            if (_noticeBannerText == null)
            {
                return;
            }

            if (_noticeRoutine != null)
            {
                StopCoroutine(_noticeRoutine);
            }

            _noticeRoutine = StartCoroutine(NoticeRoutine(text, duration));
        }

        #endregion

        #region Private Event Bindings

        private void BindEvents()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            if (gm.Score != null)
            {
                gm.Score.OnScoreChanged -= HandleScoreChanged;
                gm.Score.OnScoreChanged += HandleScoreChanged;

                gm.Score.OnHighScoreChanged -= HandleHighScoreChanged;
                gm.Score.OnHighScoreChanged += HandleHighScoreChanged;

                gm.Score.OnDoubleScoreStateChanged -= HandleDoubleScoreChanged;
                gm.Score.OnDoubleScoreStateChanged += HandleDoubleScoreChanged;

                UpdateScoreUI(gm.Score.CurrentScore, gm.Score.HighScore);
            }

            if (gm.Timer != null)
            {
                gm.Timer.OnTimeChanged -= HandleTimeChanged;
                gm.Timer.OnTimeChanged += HandleTimeChanged;
            }

            if (gm.Items != null)
            {
                gm.Items.OnInventoryUpdated -= HandleInventoryUpdated;
                gm.Items.OnInventoryUpdated += HandleInventoryUpdated;

                gm.Items.OnNoticeAnnouncement -= HandleNoticeAnnouncement;
                gm.Items.OnNoticeAnnouncement += HandleNoticeAnnouncement;

                HandleInventoryUpdated(gm.Items.Inventory);
            }

            if (gm.Hand != null)
            {
                gm.Hand.OnHandSlotUpdated -= HandleHandSlotUpdated;
                gm.Hand.OnHandSlotUpdated += HandleHandSlotUpdated;

                for (int i = 0; i < gm.Hand.HandSlots.Count; i++)
                {
                    HandleHandSlotUpdated(i, gm.Hand.HandSlots[i]);
                }
            }

            gm.OnGameOver -= HandleGameOver;
            gm.OnGameOver += HandleGameOver;

            gm.OnGameStateChanged -= HandleGameStateChanged;
            gm.OnGameStateChanged += HandleGameStateChanged;
        }

        private void UnbindEvents()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            if (gm.Score != null)
            {
                gm.Score.OnScoreChanged -= HandleScoreChanged;
                gm.Score.OnHighScoreChanged -= HandleHighScoreChanged;
                gm.Score.OnDoubleScoreStateChanged -= HandleDoubleScoreChanged;
            }

            if (gm.Timer != null)
            {
                gm.Timer.OnTimeChanged -= HandleTimeChanged;
            }

            if (gm.Items != null)
            {
                gm.Items.OnInventoryUpdated -= HandleInventoryUpdated;
                gm.Items.OnNoticeAnnouncement -= HandleNoticeAnnouncement;
            }

            if (gm.Hand != null)
            {
                gm.Hand.OnHandSlotUpdated -= HandleHandSlotUpdated;
            }

            gm.OnGameOver -= HandleGameOver;
            gm.OnGameStateChanged -= HandleGameStateChanged;
        }

        #endregion

        #region Private Handlers

        private void HandleScoreChanged(int currentScore, int gainedPoints, bool isDouble)
        {
            var gm = GameManager.Instance;
            int hi = gm != null && gm.Score != null ? gm.Score.HighScore : 0;
            UpdateScoreUI(currentScore, hi);
        }

        private void HandleHighScoreChanged(int newHighScore)
        {
            var gm = GameManager.Instance;
            int cur = gm != null && gm.Score != null ? gm.Score.CurrentScore : 0;
            UpdateScoreUI(cur, newHighScore);
        }

        private void UpdateScoreUI(int score, int highScore)
        {
            if (_currentScoreText != null)
            {
                _currentScoreText.text = score.ToString("N0");
            }

            if (_highScoreText != null)
            {
                _highScoreText.text = $"★ {highScore:N0}";
            }
        }

        private void HandleDoubleScoreChanged(bool isActive, float remainingTime)
        {
            if (_doubleScoreBadge != null)
            {
                _doubleScoreBadge.SetActive(isActive);
            }
        }

        private void HandleTimeChanged(float remainingTime, string formattedTime)
        {
            if (_timerText != null)
            {
                _timerText.text = formattedTime;

                // 10초 이하 경고 시 붉은색 강조
                if (remainingTime <= 10f)
                {
                    _timerText.color = new Color(1f, 0.3f, 0.3f, 1f);
                }
                else
                {
                    _timerText.color = Color.white;
                }
            }
        }

        private void HandleInventoryUpdated(IReadOnlyList<ItemType> items)
        {
            for (int i = 0; i < _itemSlots.Length; i++)
            {
                if (_itemSlots[i] != null)
                {
                    ItemType type = (items != null && i < items.Count) ? items[i] : ItemType.None;
                    _itemSlots[i].SetItem(type);
                }
            }
        }

        private void HandleHandSlotUpdated(int slotIndex, BlockShapeData shape)
        {
            if (_handSlots != null && slotIndex >= 0 && slotIndex < _handSlots.Length && _handSlots[slotIndex] != null)
            {
                _handSlots[slotIndex].SetShape(shape);
            }
        }

        private void HandleNoticeAnnouncement(string message)
        {
            SetNoticeText(message, 2.5f);
        }

        private IEnumerator NoticeRoutine(string text, float duration)
        {
            _noticeBannerText.text = text;
            if (_noticeCanvasGroup != null)
            {
                _noticeCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(duration);

            if (_noticeCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    _noticeCanvasGroup.alpha = Mathf.Lerp(1f, 0.2f, elapsed / 0.5f);
                    yield return null;
                }
            }
        }

        private void HandleGameOver(string reason, int finalScore)
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            if (_gameOverReasonText != null)
            {
                _gameOverReasonText.text = reason;
            }

            if (_gameOverFinalScoreText != null)
            {
                _gameOverFinalScoreText.text = $"최종 점수: {finalScore:N0}";
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                if (_gameOverPanel != null)
                {
                    _gameOverPanel.SetActive(false);
                }

                // 플레이 시작 및 재시작 시마다 템플릿 내에서 랜덤 색상 변경 (카메라 + UI 전체)
                ApplyRandomBackgroundColor();
            }
        }

        private void OnClickRestart()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
        }

        #endregion
    }
}
