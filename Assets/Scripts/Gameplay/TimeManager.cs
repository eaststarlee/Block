using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Gameplay
{
    /// <summary>
    /// 게임 세션의 제한시간(기본 60초) 카운트다운 및 시간 보너스를 관리하는 클래스입니다.
    /// </summary>
    public sealed class TimeManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Timer Settings")]
        [Tooltip("게임 시작 시 기본 제한시간(초)입니다. 기본값 60초입니다.")]
        [FormerlySerializedAs("initialDuration")]
        [SerializeField] private float _initialDuration = 60f;

        #endregion

        #region Private Fields

        private float _remainingTime;
        private bool _isRunning;

        #endregion

        #region Events

        public event Action<float, string> OnTimeChanged; // (remainingSeconds, formattedString e.g. "00:45")
        public event Action<float> OnTimeAdded; // (addedSeconds)
        public event Action OnTimeExpired;

        #endregion

        #region Public Properties

        public float RemainingTime => _remainingTime;
        public bool IsRunning => _isRunning;
        public string FormattedTime => FormatTimeString(_remainingTime);

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _isRunning = false;
                OnTimeChanged?.Invoke(0f, "00:00");
                OnTimeExpired?.Invoke();
            }
            else
            {
                OnTimeChanged?.Invoke(_remainingTime, FormattedTime);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 타이머를 시작합니다.
        /// </summary>
        /// <param name="duration">설정할 제한시간(초)입니다. 음수일 경우 기본 설정 시간을 사용합니다.</param>
        public void StartTimer(float duration = -1f)
        {
            _remainingTime = duration > 0f ? duration : _initialDuration;
            _isRunning = true;
            OnTimeChanged?.Invoke(_remainingTime, FormattedTime);
        }

        /// <summary>
        /// 타이머 진행 여부(일시정지/재개)를 설정합니다.
        /// </summary>
        /// <param name="running">실행 여부입니다.</param>
        public void SetRunning(bool running)
        {
            _isRunning = running;
        }

        /// <summary>
        /// 타이머를 기본 시간으로 재설정하고 일시정지 상태로 변경합니다.
        /// </summary>
        public void ResetTimer()
        {
            _remainingTime = _initialDuration;
            _isRunning = false;
            OnTimeChanged?.Invoke(_remainingTime, FormattedTime);
        }

        /// <summary>
        /// 현재 남은 시간에 보너스 시간을 추가합니다. (+10초 아이템 등)
        /// </summary>
        /// <param name="seconds">추가할 초 단위 시간입니다.</param>
        public void AddTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _remainingTime += seconds;
            OnTimeAdded?.Invoke(seconds);
            OnTimeChanged?.Invoke(_remainingTime, FormattedTime);
        }

        /// <summary>
        /// 초 단위 시간을 "MM:SS" 형식의 문자열로 변환합니다.
        /// </summary>
        /// <param name="seconds">변환할 초 단위 시간입니다.</param>
        public static string FormatTimeString(float seconds)
        {
            int totalSec = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int mins = totalSec / 60;
            int secs = totalSec % 60;
            return $"{mins:00}:{secs:00}";
        }

        #endregion
    }
}
