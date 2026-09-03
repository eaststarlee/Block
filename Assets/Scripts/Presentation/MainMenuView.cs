using System;
using BlockBlast.Core;
using BlockBlast.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 메인 메뉴(타이틀 화면) UI를 제어하는 뷰 클래스입니다.
    /// 타이틀 표시, 최고 기록 위젯, [Play] 시작 버튼 및 [Settings] 버튼 이벤트를 관리합니다.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [Tooltip("최고 점수 텍스트입니다.")]
        [FormerlySerializedAs("highScoreText")]
        [SerializeField] private Text _highScoreText;

        [Tooltip("별(Star) 아이콘 이미지입니다.")]
        [FormerlySerializedAs("starIconImage")]
        [SerializeField] private Image _starIconImage;

        [Tooltip("게임 시작(Play) 버튼입니다.")]
        [FormerlySerializedAs("playButton")]
        [SerializeField] private Button _playButton;

        [Tooltip("설정(Settings) 버튼입니다.")]
        [FormerlySerializedAs("settingsButton")]
        [SerializeField] private Button _settingsButton;

        [Tooltip("게임 종료(Quit) 버튼입니다.")]
        [FormerlySerializedAs("quitButton")]
        [SerializeField] private Button _quitButton;

        #endregion

        #region Events

        public event Action OnPlayClicked;
        public event Action OnSettingsClicked;
        public event Action OnQuitClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSelf();
        }

        private void Start()
        {
            RefreshDisplay();

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnHighScoreUpdated += HandleHighScoreUpdated;
                SaveManager.Instance.OnDataLoaded += HandleDataLoaded;
            }

            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(HandlePlayButtonClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(HandleSettingsButtonClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveAllListeners();
                _quitButton.onClick.AddListener(HandleQuitButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnHighScoreUpdated -= HandleHighScoreUpdated;
                SaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 내부 컴포넌트 참조를 스스로 탐색하여 자동 바인딩합니다.
        /// </summary>
        public void SetupSelf()
        {
            if (_highScoreText == null)
            {
                Transform t = transform.Find("HighScoreGroup/HighScoreText");
                if (t != null)
                {
                    _highScoreText = t.GetComponent<Text>();
                }
            }

            if (_starIconImage == null)
            {
                Transform t = transform.Find("HighScoreGroup/StarIcon");
                if (t != null)
                {
                    _starIconImage = t.GetComponent<Image>();
                }
            }

            if (_playButton == null)
            {
                Transform t = transform.Find("PlayButton");
                if (t != null)
                {
                    _playButton = t.GetComponent<Button>();
                }
            }

            if (_settingsButton == null)
            {
                Transform t = transform.Find("SettingsButton");
                if (t != null)
                {
                    _settingsButton = t.GetComponent<Button>();
                }
            }

            if (_quitButton == null)
            {
                Transform t = transform.Find("QuitButton");
                if (t != null)
                {
                    _quitButton = t.GetComponent<Button>();
                }
            }

            // 테마 별 아이콘 적용
            if (_starIconImage != null && ThemeManager.Instance != null && ThemeManager.Instance.StarIconSprite != null)
            {
                _starIconImage.sprite = ThemeManager.Instance.StarIconSprite;
                _starIconImage.preserveAspect = true;
            }
        }

        /// <summary>
        /// SaveManager의 최신 데이터를 읽어와 화면의 최고 점수를 갱신합니다.
        /// </summary>
        public void RefreshDisplay()
        {
            SetupSelf();

            int highScore = SaveManager.Instance != null ? SaveManager.Instance.HighScore : 0;

            if (_highScoreText != null)
            {
                _highScoreText.text = highScore.ToString("N0");
            }
        }

        /// <summary>
        /// 메인 메뉴 패널의 활성화 여부를 설정합니다.
        /// </summary>
        /// <param name="show">표시 여부입니다.</param>
        public void Show(bool show)
        {
            gameObject.SetActive(show);
            if (show)
            {
                RefreshDisplay();
            }
        }

        #endregion

        #region Private Handlers

        private void HandlePlayButtonClicked()
        {
            OnPlayClicked?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGameFromMenu();
            }
        }

        private void HandleSettingsButtonClicked()
        {
            OnSettingsClicked?.Invoke();
        }

        private void HandleQuitButtonClicked()
        {
            OnQuitClicked?.Invoke();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleHighScoreUpdated(int newHighScore)
        {
            if (_highScoreText != null)
            {
                _highScoreText.text = newHighScore.ToString("N0");
            }
        }

        private void HandleDataLoaded(PlayerData data)
        {
            RefreshDisplay();
        }

        #endregion
    }
}
