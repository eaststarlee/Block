using System;
using BlockBlast.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 인게임 일시정지(Pause) 팝업을 제어하는 뷰 클래스입니다.
    /// [Resume] 계속하기, [Restart] 재시작, [Main Menu] 메인 메뉴로 나가기 버튼을 관리합니다.
    /// </summary>
    public sealed class PausePopupView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Popup Controls")]
        [Tooltip("게임 계속하기(Resume) 버튼입니다.")]
        [FormerlySerializedAs("resumeButton")]
        [SerializeField] private Button _resumeButton;

        [Tooltip("현재 게임 재시작(Restart) 버튼입니다.")]
        [FormerlySerializedAs("restartButton")]
        [SerializeField] private Button _restartButton;

        [Tooltip("메인 메뉴로 나가기(Main Menu) 버튼입니다.")]
        [FormerlySerializedAs("mainMenuButton")]
        [SerializeField] private Button _mainMenuButton;

        #endregion

        #region Events

        public event Action OnResumeClicked;
        public event Action OnRestartClicked;
        public event Action OnMainMenuClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSelf();
        }

        private void Start()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveAllListeners();
                _resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveAllListeners();
                _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 내부 컴포넌트 참조를 스스로 탐색하여 자동 바인딩합니다.
        /// </summary>
        public void SetupSelf()
        {
            if (_resumeButton == null)
            {
                Transform t = transform.Find("Content/ResumeButton");
                if (t == null) t = transform.Find("ResumeButton");
                if (t != null) _resumeButton = t.GetComponent<Button>();
            }

            if (_restartButton == null)
            {
                Transform t = transform.Find("Content/RestartButton");
                if (t == null) t = transform.Find("RestartButton");
                if (t != null) _restartButton = t.GetComponent<Button>();
            }

            if (_mainMenuButton == null)
            {
                Transform t = transform.Find("Content/MainMenuButton");
                if (t == null) t = transform.Find("MainMenuButton");
                if (t != null) _mainMenuButton = t.GetComponent<Button>();
            }
        }

        /// <summary>
        /// 일시정지 팝업을 열거나 닫습니다.
        /// </summary>
        /// <param name="show">표시 여부입니다.</param>
        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }

        #endregion

        #region Private Handlers

        private void HandleResumeClicked()
        {
            Show(false);
            OnResumeClicked?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame(false);
            }
        }

        private void HandleRestartClicked()
        {
            Show(false);
            OnRestartClicked?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
        }

        private void HandleMainMenuClicked()
        {
            Show(false);
            OnMainMenuClicked?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }

        #endregion
    }
}
