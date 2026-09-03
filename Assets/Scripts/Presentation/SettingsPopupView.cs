using System;
using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlockBlast.Presentation
{
    /// <summary>
    /// 모바일 설정 팝업(오디오 Mute On/Off 토글, 모바일 진동 Haptics On/Off 토글, 닫기)을 제어하는 뷰 클래스입니다.
    /// </summary>
    public sealed class SettingsPopupView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Toggle Buttons")]
        [Tooltip("오디오 음소거(Mute) On/Off 토글 버튼입니다.")]
        [FormerlySerializedAs("audioToggleButton")]
        [SerializeField] private Button _audioToggleButton;

        [Tooltip("오디오 상태 텍스트 (Sound: ON / Sound: OFF)입니다.")]
        [FormerlySerializedAs("audioStatusText")]
        [SerializeField] private Text _audioStatusText;

        [Tooltip("모바일 진동(Vibration) On/Off 토글 버튼입니다.")]
        [FormerlySerializedAs("vibrationToggleButton")]
        [SerializeField] private Button _vibrationToggleButton;

        [Tooltip("진동 상태 텍스트 (Vibration: ON / Vibration: OFF)입니다.")]
        [FormerlySerializedAs("vibrationStatusText")]
        [SerializeField] private Text _vibrationStatusText;

        [Header("Popup Controls")]
        [Tooltip("설정 팝업 닫기(X) 버튼입니다.")]
        [FormerlySerializedAs("closeButton")]
        [SerializeField] private Button _closeButton;

        [Header("Toggle Colors")]
        [SerializeField] private Color _enabledColor = new Color(0.2f, 0.75f, 0.35f, 1f);
        [SerializeField] private Color _disabledColor = new Color(0.65f, 0.25f, 0.25f, 1f);

        #endregion

        #region Events

        public event Action OnCloseClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSelf();
        }

        private void Start()
        {
            RefreshUI();

            if (_audioToggleButton != null)
            {
                _audioToggleButton.onClick.RemoveAllListeners();
                _audioToggleButton.onClick.AddListener(HandleAudioToggleClicked);
            }

            if (_vibrationToggleButton != null)
            {
                _vibrationToggleButton.onClick.RemoveAllListeners();
                _vibrationToggleButton.onClick.AddListener(HandleVibrationToggleClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 내부 컴포넌트 참조를 스스로 탐색하여 자동 바인딩합니다.
        /// </summary>
        public void SetupSelf()
        {
            if (_audioToggleButton == null)
            {
                Transform t = transform.Find("Content/AudioToggleGroup/AudioToggleButton");
                if (t == null) t = transform.Find("AudioToggleButton");
                if (t != null) _audioToggleButton = t.GetComponent<Button>();
            }

            if (_audioStatusText == null && _audioToggleButton != null)
            {
                _audioStatusText = _audioToggleButton.GetComponentInChildren<Text>();
            }

            if (_vibrationToggleButton == null)
            {
                Transform t = transform.Find("Content/VibrationToggleGroup/VibrationToggleButton");
                if (t == null) t = transform.Find("VibrationToggleButton");
                if (t != null) _vibrationToggleButton = t.GetComponent<Button>();
            }

            if (_vibrationStatusText == null && _vibrationToggleButton != null)
            {
                _vibrationStatusText = _vibrationToggleButton.GetComponentInChildren<Text>();
            }

            if (_closeButton == null)
            {
                Transform t = transform.Find("Content/CloseButton");
                if (t == null) t = transform.Find("CloseButton");
                if (t != null) _closeButton = t.GetComponent<Button>();
            }
        }

        /// <summary>
        /// SaveManager의 현재 설정값을 읽어와 토글 상태를 갱신합니다.
        /// </summary>
        public void RefreshUI()
        {
            SetupSelf();

            bool isMuted = SaveManager.Instance != null && SaveManager.Instance.IsAudioMuted;
            bool isVibrationOn = SaveManager.Instance == null || SaveManager.Instance.IsVibrationEnabled;

            // 오디오 상태 반영 (isMuted가 false면 Sound ON)
            if (_audioStatusText != null)
            {
                _audioStatusText.text = isMuted ? "🔊 Sound: OFF" : "🔊 Sound: ON";
            }

            if (_audioToggleButton != null)
            {
                var img = _audioToggleButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isMuted ? _disabledColor : _enabledColor;
                }
            }

            // 진동 상태 반영
            if (_vibrationStatusText != null)
            {
                _vibrationStatusText.text = isVibrationOn ? "📳 Vibration: ON" : "📳 Vibration: OFF";
            }

            if (_vibrationToggleButton != null)
            {
                var img = _vibrationToggleButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isVibrationOn ? _enabledColor : _disabledColor;
                }
            }
        }

        /// <summary>
        /// 설정 팝업을 열거나 닫습니다.
        /// </summary>
        /// <param name="show">표시 여부입니다.</param>
        public void Show(bool show)
        {
            gameObject.SetActive(show);
            if (show)
            {
                RefreshUI();
            }
        }

        #endregion

        #region Private Handlers

        private void HandleAudioToggleClicked()
        {
            if (SaveManager.Instance == null)
            {
                return;
            }

            bool newMuteState = !SaveManager.Instance.IsAudioMuted;
            SaveManager.Instance.SetAudioMuted(newMuteState);
            RefreshUI();
        }

        private void HandleVibrationToggleClicked()
        {
            if (SaveManager.Instance == null)
            {
                return;
            }

            bool newVibState = !SaveManager.Instance.IsVibrationEnabled;
            SaveManager.Instance.SetVibrationEnabled(newVibState);

            if (newVibState)
            {
                SaveManager.Instance.TriggerVibration();
            }

            RefreshUI();
        }

        private void HandleCloseClicked()
        {
            Show(false);
            OnCloseClicked?.Invoke();
        }

        #endregion
    }
}
