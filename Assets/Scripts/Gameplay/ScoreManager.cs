using System;
using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Gameplay
{
    /// <summary>
    /// 플레이어 점수 계산, 연속 클리어 스트릭(콤보), 2배 점수 버프 및 최고 점수 저장을 관리하는 클래스입니다.
    /// </summary>
    public sealed class ScoreManager : MonoBehaviour
    {
        private const string HighScoreKey = "BlockBlast_HighScore_Record";

        #region Serialized Fields

        [Header("Score Settings")]
        [Tooltip("블록을 구성하는 단일 셀 1칸당 획득하는 기본 점수입니다.")]
        [FormerlySerializedAs("pointsPerCell")]
        [SerializeField] private int _pointsPerCell = 10;

        [Tooltip("라인 1줄 파괴(Blast) 시 획득하는 기본 점수입니다.")]
        [FormerlySerializedAs("baseLinePoints")]
        [SerializeField] private int _baseLinePoints = 100;

        #endregion

        #region Private Fields

        private int _currentScore;
        private int _highScore;
        private int _streakCount;
        private float _doubleScoreTimer;

        #endregion

        #region Events

        public event Action<int, int, bool> OnScoreChanged; // (currentScore, gainedPoints, isDouble)
        public event Action<int> OnHighScoreChanged;
        public event Action<bool, float> OnDoubleScoreStateChanged; // (isActive, remainingTime)

        #endregion

        #region Public Properties

        public int CurrentScore => _currentScore;
        public int HighScore => _highScore;
        public bool IsDoubleScoreActive => _doubleScoreTimer > 0f;
        public float DoubleScoreRemainingTime => _doubleScoreTimer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        private void Update()
        {
            if (_doubleScoreTimer > 0f)
            {
                _doubleScoreTimer -= Time.deltaTime;
                if (_doubleScoreTimer <= 0f)
                {
                    _doubleScoreTimer = 0f;
                    OnDoubleScoreStateChanged?.Invoke(false, 0f);
                }
                else
                {
                    OnDoubleScoreStateChanged?.Invoke(true, _doubleScoreTimer);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 현재 점수, 스트릭 콤보 및 버프 상태를 초기화합니다.
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            _streakCount = 0;
            _doubleScoreTimer = 0f;
            OnScoreChanged?.Invoke(_currentScore, 0, false);
            OnDoubleScoreStateChanged?.Invoke(false, 0f);
        }

        /// <summary>
        /// 점수 2배 획득 버프를 활성화합니다.
        /// </summary>
        /// <param name="duration">버프 지속 시간(초)입니다. 기본값 10초입니다.</param>
        public void ActivateDoubleScore(float duration = 10f)
        {
            _doubleScoreTimer = duration;
            OnDoubleScoreStateChanged?.Invoke(true, _doubleScoreTimer);
        }

        /// <summary>
        /// 보드에 블록을 성공적으로 배치했을 때 셀 개수 비례 점수를 가산합니다.
        /// </summary>
        /// <param name="cellCount">배치한 블록의 셀 칸 수입니다.</param>
        public void AddPlacementScore(int cellCount)
        {
            int baseGain = cellCount * _pointsPerCell;
            int finalGain = IsDoubleScoreActive ? baseGain * 2 : baseGain;

            ApplyScore(finalGain);
        }

        /// <summary>
        /// 라인 Blast 시 완성된 줄 수와 턴 연속 클리어 스트릭을 반영하여 점수를 가산합니다.
        /// </summary>
        /// <param name="linesCleared">동시에 클리어된 줄 수입니다.</param>
        public void AddLineClearScore(int linesCleared)
        {
            if (linesCleared <= 0)
            {
                _streakCount = 0;
                return;
            }

            _streakCount++;

            // 다중 라인 동시 파괴 가산 공식: 1줄=100, 2줄=300, 3줄=600, 4줄=1000...
            int multiLineBonus = linesCleared * (linesCleared + 1) / 2 * _baseLinePoints;

            // 연속 턴 클리어 스트릭 보너스 (각 턴마다 +20%)
            float streakMultiplier = 1f + (_streakCount - 1) * 0.2f;
            int totalGain = Mathf.RoundToInt(multiLineBonus * streakMultiplier);

            if (IsDoubleScoreActive)
            {
                totalGain *= 2;
            }

            ApplyScore(totalGain);
        }

        #endregion

        #region Private Methods

        private void ApplyScore(int pointsToAdd)
        {
            _currentScore += pointsToAdd;

            if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                PlayerPrefs.SetInt(HighScoreKey, _highScore);
                PlayerPrefs.Save();
                OnHighScoreChanged?.Invoke(_highScore);
            }

            OnScoreChanged?.Invoke(_currentScore, pointsToAdd, IsDoubleScoreActive);
        }

        #endregion
    }
}
