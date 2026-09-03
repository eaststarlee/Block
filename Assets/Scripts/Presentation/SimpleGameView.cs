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
    /// 메인 메뉴, 인게임 화면, 일시정지 팝업, 설정 팝업, 게임오버 팝업의 활성화(SetActive) 상태와 전체 UI 연동을 총괄 제어하는 뷰 클래스입니다.
    /// </summary>
    public sealed class SimpleGameView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Screen Panels (Hierarchy GameObjects)")]
        [Tooltip("하이어라키의 메인 메뉴(타이틀) 패널 게임오브젝트입니다.")]
        [FormerlySerializedAs("mainMenuPanel")]
        [SerializeField] private GameObject _mainMenuPanel;

        [Tooltip("하이어라키의 인게임 플레이 화면(보드, 손패 등) 루트 게임오브젝트입니다.")]
        [FormerlySerializedAs("inGameScreen")]
        [SerializeField] private GameObject _inGameScreen;

        [Tooltip("하이어라키의 설정(Settings) 팝업 게임오브젝트입니다.")]
        [FormerlySerializedAs("settingsPopup")]
        [SerializeField] private GameObject _settingsPopup;

        [Tooltip("하이어라키의 일시정지(Pause) 팝업 게임오브젝트입니다.")]
        [FormerlySerializedAs("pausePopup")]
        [SerializeField] private GameObject _pausePopup;

        [Tooltip("하이어라키의 게임오버(GameOver) 팝업 게임오브젝트입니다.")]
        [FormerlySerializedAs("gameOverPanel")]
        [SerializeField] private GameObject _gameOverPanel;

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

        [Header("In-Game Top Bar UI References")]
        [Tooltip("인게임 상단 최고 점수 텍스트입니다.")]
        [FormerlySerializedAs("highScoreText")]
        [SerializeField] private Text _highScoreText;

        [Tooltip("인게임 상단 설정/일시정지 버튼입니다.")]
        [FormerlySerializedAs("topBarSettingsButton")]
        [SerializeField] private Button _topBarSettingsButton;

        [Header("In-Game Score And Timer UI")]
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
        [FormerlySerializedAs("gameOverReasonText")]
        [SerializeField] private Text _gameOverReasonText;

        [FormerlySerializedAs("gameOverFinalScoreText")]
        [SerializeField] private Text _gameOverFinalScoreText;

        [FormerlySerializedAs("restartButton")]
        [SerializeField] private Button _restartButton;

        [Tooltip("게임오버 팝업의 [Main Menu] 홈으로 가기 버튼입니다.")]
        [FormerlySerializedAs("gameOverHomeButton")]
        [SerializeField] private Button _gameOverHomeButton;

        #endregion

        #region Private Fields

        private Coroutine _noticeRoutine;
        private int _lastColorIndex = -1;

        private MainMenuView _mainMenuView;
        private SettingsPopupView _settingsPopupView;
        private PausePopupView _pausePopupView;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            FindHierarchyReferences();
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
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(OnClickRestart);
            }

            if (_gameOverHomeButton != null)
            {
                _gameOverHomeButton.onClick.RemoveAllListeners();
                _gameOverHomeButton.onClick.AddListener(OnClickGameOverHome);
            }

            if (_topBarSettingsButton != null)
            {
                _topBarSettingsButton.onClick.RemoveAllListeners();
                _topBarSettingsButton.onClick.AddListener(OnClickTopBarSettings);
            }

            if (_mainMenuView != null)
            {
                _mainMenuView.OnSettingsClicked += HandleMainMenuSettingsClicked;
            }

            // 시작 시 상태에 맞게 체크표시(SetActive) 일괄 동기화
            if (GameManager.Instance != null)
            {
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
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

            // 1. UI BackgroundPanel 색상 적용
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

            // 2. 메인 카메라 배경색도 100% 동일하게 동기화
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

        #region Private Hierarchy Reference Finding

        private void FindHierarchyReferences()
        {
            // 1. 패널 GameObject 자동 탐색
            if (_mainMenuPanel == null)
            {
                Transform t = transform.Find("MainMenuPanel");
                if (t != null) _mainMenuPanel = t.gameObject;
            }

            if (_inGameScreen == null)
            {
                Transform t = transform.Find("MainGameScreen");
                if (t == null) t = transform.Find("InGameScreen");
                if (t != null) _inGameScreen = t.gameObject;
            }

            if (_settingsPopup == null)
            {
                Transform t = transform.Find("SettingsPopup");
                if (t != null) _settingsPopup = t.gameObject;
            }

            if (_pausePopup == null)
            {
                Transform t = transform.Find("PausePopup");
                if (t != null) _pausePopup = t.gameObject;
            }

            if (_gameOverPanel == null)
            {
                Transform t = transform.Find("GameOverPanel");
                if (t != null) _gameOverPanel = t.gameObject;
            }

            // 2. 패널 컴포넌트 캐싱
            if (_mainMenuPanel != null)
            {
                _mainMenuView = _mainMenuPanel.GetComponent<MainMenuView>();
            }

            if (_settingsPopup != null)
            {
                _settingsPopupView = _settingsPopup.GetComponent<SettingsPopupView>();
            }

            if (_pausePopup != null)
            {
                _pausePopupView = _pausePopup.GetComponent<PausePopupView>();
            }

            // 3. 배경 이미지
            if (_backgroundImage == null)
            {
                Transform bgT = transform.Find("BackgroundPanel");
                if (bgT == null) bgT = transform.Find("Background");
                if (bgT != null) _backgroundImage = bgT.GetComponent<Image>();
            }

            // 4. 슬롯 및 보드 탐색
            var foundHandSlots = GetComponentsInChildren<HandBlockView>(true);
            _handSlots = new HandBlockView[3];
            foreach (var slot in foundHandSlots)
            {
                if (slot != null && slot.SlotIndex >= 0 && slot.SlotIndex < 3)
                {
                    _handSlots[slot.SlotIndex] = slot;
                }
            }

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

            // 5. TopBar 설정 버튼
            if (_topBarSettingsButton == null)
            {
                Transform topBarT = transform.Find("TopBar");
                if (topBarT != null)
                {
                    Transform settingsIconT = topBarT.Find("SettingsIcon");
                    if (settingsIconT != null)
                    {
                        _topBarSettingsButton = settingsIconT.GetComponent<Button>();
                        if (_topBarSettingsButton == null)
                        {
                            _topBarSettingsButton = settingsIconT.gameObject.AddComponent<Button>();
                        }
                    }
                }
            }
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
                _highScoreText.text = highScore.ToString("N0");
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
            SetPanelActive(_pausePopup, false);
            SetPanelActive(_settingsPopup, false);
            SetPanelActive(_gameOverPanel, true);

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
            switch (state)
            {
                case GameState.MainMenu:
                    SetPanelActive(_mainMenuPanel, true);
                    SetPanelActive(_inGameScreen, false);
                    SetPanelActive(_settingsPopup, false);
                    SetPanelActive(_pausePopup, false);
                    SetPanelActive(_gameOverPanel, false);

                    if (_mainMenuView != null)
                    {
                        _mainMenuView.RefreshDisplay();
                    }

                    ApplyRandomBackgroundColor();
                    break;

                case GameState.Playing:
                    SetPanelActive(_mainMenuPanel, false);
                    SetPanelActive(_inGameScreen, true);
                    SetPanelActive(_settingsPopup, false);
                    SetPanelActive(_pausePopup, false);
                    SetPanelActive(_gameOverPanel, false);
                    break;

                case GameState.Paused:
                    SetPanelActive(_pausePopup, true);
                    break;

                case GameState.GameOver:
                    SetPanelActive(_pausePopup, false);
                    SetPanelActive(_settingsPopup, false);
                    SetPanelActive(_gameOverPanel, true);
                    break;
            }
        }

        private void SetPanelActive(GameObject panel, bool isActive)
        {
            if (panel != null)
            {
                panel.SetActive(isActive);
            }
        }

        private void OnClickRestart()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
        }

        private void OnClickGameOverHome()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }

        private void OnClickTopBarSettings()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                GameManager.Instance.PauseGame(true);
            }
            else if (GameManager.Instance.CurrentState == GameState.MainMenu)
            {
                SetPanelActive(_settingsPopup, true);
                if (_settingsPopupView != null)
                {
                    _settingsPopupView.RefreshUI();
                }
            }
        }

        private void HandleMainMenuSettingsClicked()
        {
            SetPanelActive(_settingsPopup, true);
            if (_settingsPopupView != null)
            {
                _settingsPopupView.RefreshUI();
            }
        }

        #endregion
    }
}
