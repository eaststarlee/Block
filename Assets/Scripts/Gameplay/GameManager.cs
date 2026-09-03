using System;
using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Gameplay
{
    /// <summary>
    /// 게임의 전체 라이프사이클 및 화면 상태(MainMenu, Playing, Paused, GameOver)와 핵심 하위 시스템을 조율하는 관리자 클래스입니다.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        #region Static Fields

        private static GameManager s_instance;

        #endregion

        #region Serialized Fields

        [Header("Subsystem References")]
        [Tooltip("8x8 보드 및 라인 파괴 로직 관리자입니다.")]
        [FormerlySerializedAs("gridManager")]
        [SerializeField] private GridManager _gridManager;

        [Tooltip("하단 손패 3개 블록 생성 및 상태 관리자입니다.")]
        [FormerlySerializedAs("handManager")]
        [SerializeField] private HandManager _handManager;

        [Tooltip("아이템 효과 실행 및 인벤토리 관리자입니다.")]
        [FormerlySerializedAs("itemManager")]
        [SerializeField] private ItemManager _itemManager;

        [Tooltip("점수, 콤보 및 최고 점수 관리자입니다.")]
        [FormerlySerializedAs("scoreManager")]
        [SerializeField] private ScoreManager _scoreManager;

        [Tooltip("제한시간 카운트다운 관리자입니다.")]
        [FormerlySerializedAs("timeManager")]
        [SerializeField] private TimeManager _timeManager;

        #endregion

        #region Private Fields

        private GameState _gameState = GameState.MainMenu;

        #endregion

        #region Events

        public event Action<GameState> OnGameStateChanged;
        public event Action<string, int> OnGameOver; // (reason, finalScore)

        #endregion

        #region Public Properties

        public static GameManager Instance => s_instance;

        public GameState CurrentState => _gameState;
        public GridManager Grid => _gridManager;
        public HandManager Hand => _handManager;
        public ItemManager Items => _itemManager;
        public ScoreManager Score => _scoreManager;
        public TimeManager Timer => _timeManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
            }
            else if (s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // SaveManager 자동 확인 및 부착
            if (SaveManager.Instance == null && GetComponent<SaveManager>() == null)
            {
                gameObject.AddComponent<SaveManager>();
            }

            // 하위 컴포넌트 자동 탐색 (인스펙터 미할당 대비)
            if (_gridManager == null)
            {
                _gridManager = GetComponentInChildren<GridManager>();
            }

            if (_handManager == null)
            {
                _handManager = GetComponentInChildren<HandManager>();
            }

            if (_itemManager == null)
            {
                _itemManager = GetComponentInChildren<ItemManager>();
            }

            if (_scoreManager == null)
            {
                _scoreManager = GetComponentInChildren<ScoreManager>();
            }

            if (_timeManager == null)
            {
                _timeManager = GetComponentInChildren<TimeManager>();
            }

            if (GetComponent<Presentation.ThemeManager>() == null)
            {
                gameObject.AddComponent<Presentation.ThemeManager>();
            }

            if (_itemManager != null)
            {
                _itemManager.Initialize(_gridManager, _handManager, _scoreManager, _timeManager);
            }
        }

        private void Start()
        {
            if (_timeManager != null)
            {
                _timeManager.OnTimeExpired += HandleTimeExpired;
            }

            if (_handManager != null)
            {
                _handManager.OnHandRefilled += CheckForDeadlock;
            }

            // 앱 실행 시 메인 메뉴 상태로 초기화
            EnterMainMenu();
        }

        private void OnDestroy()
        {
            if (_timeManager != null)
            {
                _timeManager.OnTimeExpired -= HandleTimeExpired;
            }

            if (_handManager != null)
            {
                _handManager.OnHandRefilled -= CheckForDeadlock;
            }
        }

        #endregion

        #region Public State Transition Methods

        /// <summary>
        /// 메인 메뉴 화면으로 진입합니다.
        /// </summary>
        public void EnterMainMenu()
        {
            _gameState = GameState.MainMenu;

            if (_timeManager != null)
            {
                _timeManager.SetRunning(false);
            }

            OnGameStateChanged?.Invoke(_gameState);
        }

        /// <summary>
        /// 메인 메뉴에서 [Play] 버튼을 눌러 새 게임을 시작합니다.
        /// </summary>
        public void StartGameFromMenu()
        {
            StartNewGame();
        }

        /// <summary>
        /// 새로운 게임 세션을 시작하고 모든 하위 시스템을 초기화합니다.
        /// </summary>
        public void StartNewGame()
        {
            _gameState = GameState.Playing;
            OnGameStateChanged?.Invoke(_gameState);

            if (_gridManager != null)
            {
                _gridManager.ResetBoard();
            }

            if (_scoreManager != null)
            {
                _scoreManager.ResetScore();
            }

            if (_itemManager != null)
            {
                _itemManager.ResetInventory();
            }

            if (_timeManager != null)
            {
                _timeManager.StartTimer(60f);
            }

            if (_handManager != null)
            {
                _handManager.ReplenishHand();
            }
        }

        /// <summary>
        /// 인게임 플레이 중 일시정지 또는 재개를 전환합니다.
        /// </summary>
        /// <param name="isPaused">true일 경우 일시정지, false일 경우 재개합니다.</param>
        public void PauseGame(bool isPaused)
        {
            if (_gameState != GameState.Playing && _gameState != GameState.Paused)
            {
                return;
            }

            if (isPaused)
            {
                _gameState = GameState.Paused;
                if (_timeManager != null)
                {
                    _timeManager.SetRunning(false);
                }
            }
            else
            {
                _gameState = GameState.Playing;
                if (_timeManager != null)
                {
                    _timeManager.SetRunning(true);
                }
            }

            OnGameStateChanged?.Invoke(_gameState);
        }

        /// <summary>
        /// 인게임 플레이 또는 일시정지 상태에서 메인 메뉴로 복귀합니다.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (_timeManager != null)
            {
                _timeManager.SetRunning(false);
            }

            EnterMainMenu();
        }

        #endregion

        #region Public Methods - Gameplay

        /// <summary>
        /// 손패의 블록을 보드 특정 좌표에 배치 시도합니다.
        /// </summary>
        /// <param name="handSlotIndex">배치할 손패 슬롯 번호(0~2)입니다.</param>
        /// <param name="gridX">보드 기준 X 좌표입니다.</param>
        /// <param name="gridY">보드 기준 Y 좌표입니다.</param>
        /// <returns>배치 성공 여부를 반환합니다.</returns>
        public bool TryPlaceHandBlock(int handSlotIndex, int gridX, int gridY)
        {
            if (_gameState != GameState.Playing)
            {
                return false;
            }

            if (handSlotIndex < 0 || handSlotIndex >= HandManager.HandSlotCount)
            {
                return false;
            }

            var shape = _handManager.HandSlots[handSlotIndex];
            if (shape == null)
            {
                return false;
            }

            if (!_gridManager.CanPlaceShape(shape, gridX, gridY))
            {
                return false;
            }

            // 1. 블록 배치 점수 가산
            _scoreManager.AddPlacementScore(shape.Cells.Count);

            // 2. 보드에 블록 배치 및 라인 Blast 실행
            _gridManager.PlaceShape(shape, gridX, gridY, out int clearedLines, out _);

            // 3. 라인 클리어 점수 가산
            if (clearedLines > 0)
            {
                _scoreManager.AddLineClearScore(clearedLines);
            }

            // 4. 손패 슬롯 소비 (3개 모두 비면 자동으로 새 3개 충당)
            _handManager.ConsumeSlot(handSlotIndex);

            // 5. 남은 블록을 놓을 수 있는지 검사 (데드락 판정)
            CheckForDeadlock();

            return true;
        }

        /// <summary>
        /// 게임 오버를 발생시키고 사유와 점수를 전달합니다.
        /// </summary>
        /// <param name="reason">게임 오버 사유 메시지입니다.</param>
        public void TriggerGameOver(string reason)
        {
            if (_gameState == GameState.GameOver)
            {
                return;
            }

            _gameState = GameState.GameOver;

            if (_timeManager != null)
            {
                _timeManager.SetRunning(false);
            }

            OnGameStateChanged?.Invoke(_gameState);
            OnGameOver?.Invoke(reason, _scoreManager != null ? _scoreManager.CurrentScore : 0);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 손패의 블록 중 보드에 놓을 수 있는 자리가 남아있는지 검사합니다.
        /// </summary>
        private void CheckForDeadlock()
        {
            if (_gameState != GameState.Playing)
            {
                return;
            }

            var remainingShapes = _handManager.GetRemainingShapes();
            if (remainingShapes.Count > 0 && !_gridManager.HasAnyValidMove(remainingShapes))
            {
                TriggerGameOver("더 이상 놓을 수 있는 자리가 없습니다!");
            }
        }

        private void HandleTimeExpired()
        {
            if (_gameState != GameState.Playing)
            {
                return;
            }

            TriggerGameOver("제한시간(1분)이 종료되었습니다!");
        }

        #endregion
    }
}
