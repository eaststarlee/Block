using BlockBlast.Core;
using UnityEngine;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BlockBlast.Gameplay
{
    /// <summary>
    /// Unity 에디터 및 런타임 환경에서 주요 기능(아이템, 라인 클리어, 점수, 타이머)을 즉각 테스트하기 위한 디버거 클래스입니다.
    /// </summary>
    public sealed class BlockBlastDebugger : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Debugger Settings")]
        [Tooltip("키보드 단축키를 통한 디버깅 활성화 여부입니다.")]
        [FormerlySerializedAs("enableKeyShortcuts")]
        [SerializeField] private bool _enableKeyShortcuts = true;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_enableKeyShortcuts || GameManager.Instance == null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            // [1, 2, 3] 키: 상단 인벤토리 슬롯 0, 1, 2 아이템 사용
            if (kb.digit1Key.wasPressedThisFrame)
            {
                GameManager.Instance.Items.UseInventoryItem(0);
            }

            if (kb.digit2Key.wasPressedThisFrame)
            {
                GameManager.Instance.Items.UseInventoryItem(1);
            }

            if (kb.digit3Key.wasPressedThisFrame)
            {
                GameManager.Instance.Items.UseInventoryItem(2);
            }

            // [R] 키: 손패 리셋
            if (kb.rKey.wasPressedThisFrame)
            {
                GameManager.Instance.Hand.ResetHand();
            }

            // [C] 키: 보드 전체 클린
            if (kb.cKey.wasPressedThisFrame)
            {
                GameManager.Instance.Grid.ClearAllBoard(out _);
            }

            // [T] 키: 시간 10초 추가
            if (kb.tKey.wasPressedThisFrame)
            {
                GameManager.Instance.Timer.AddTime(10f);
            }

            // [Space] 키: 게임 오버 상태일 때 새 게임 재시작
            if (kb.spaceKey.wasPressedThisFrame && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                GameManager.Instance.StartNewGame();
            }
#endif
        }

        #endregion

        #region Context Menu Debug Methods

        /// <summary>
        /// (3,3) 그리드 좌표에 즉발형 3x3 폭탄을 발동합니다.
        /// </summary>
        [ContextMenu("Debug: Add Instant Bomb3x3 to Grid (3,3)")]
        public void DebugAddBomb3x3()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.Grid.ClearArea3x3(3, 3, out _);
        }

        /// <summary>
        /// 인벤토리에 보드 클리어 아이템을 추가합니다.
        /// </summary>
        [ContextMenu("Debug: Add Inventory Item (BoardClean)")]
        public void DebugAddBoardClean()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.Items.AddInventoryItem(ItemType.BoardClean);
        }

        /// <summary>
        /// 인벤토리에 손패 리셋 아이템을 추가합니다.
        /// </summary>
        [ContextMenu("Debug: Add Inventory Item (HandReset)")]
        public void DebugAddHandReset()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.Items.AddInventoryItem(ItemType.HandReset);
        }

        /// <summary>
        /// 인벤토리에 점수 2배 버프 아이템을 추가합니다.
        /// </summary>
        [ContextMenu("Debug: Add Inventory Item (ScoreDouble10s)")]
        public void DebugAddScoreDouble()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.Items.AddInventoryItem(ItemType.ScoreDouble10s);
        }

        /// <summary>
        /// 테스트를 위해 보드 0번 행 전체를 채우고 라인 파괴를 트리거합니다.
        /// </summary>
        [ContextMenu("Debug: Fill Line 0 for Test Blast")]
        public void DebugFillLine0()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            for (int x = 0; x < GridManager.BoardSize; x++)
            {
                GameManager.Instance.Grid.Board[x, 0].IsOccupied = true;
                GameManager.Instance.Grid.Board[x, 0].Color = Color.cyan;
            }

            GameManager.Instance.Grid.CheckAndClearLines(out _, out _);
        }

        #endregion
    }
}
