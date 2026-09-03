using System;
using UnityEngine;

namespace BlockBlast.Core
{
    /// <summary>
    /// 모바일 런타임 환경에서 유지할 플레이어의 저장 데이터 모델 클래스입니다.
    /// </summary>
    [Serializable]
    public sealed class PlayerData
    {
        #region Save Fields

        [Header("Record Data")]
        [Tooltip("역대 최고 점수 기록입니다.")]
        [SerializeField] private int _highScore;

        [Header("Setting Data")]
        [Tooltip("오디오 음소거(Mute) 여부입니다. true일 경우 무음 처리됩니다.")]
        [SerializeField] private bool _isAudioMuted;

        [Tooltip("모바일 진동(Haptics) 활성화 여부입니다.")]
        [SerializeField] private bool _isVibrationEnabled = true;

        [Header("Metadata")]
        [Tooltip("마지막으로 플레이한 일시 문자열입니다.")]
        [SerializeField] private string _lastPlayedDate;

        #endregion

        #region Public Properties

        public int HighScore
        {
            get => _highScore;
            set => _highScore = value;
        }

        public bool IsAudioMuted
        {
            get => _isAudioMuted;
            set => _isAudioMuted = value;
        }

        public bool IsVibrationEnabled
        {
            get => _isVibrationEnabled;
            set => _isVibrationEnabled = value;
        }

        public string LastPlayedDate
        {
            get => _lastPlayedDate;
            set => _lastPlayedDate = value;
        }

        #endregion

        #region Constructors

        public PlayerData()
        {
            _highScore = 0;
            _isAudioMuted = false;
            _isVibrationEnabled = true;
            _lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        #endregion
    }
}
