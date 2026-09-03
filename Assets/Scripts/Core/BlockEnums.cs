using System;
using UnityEngine;

namespace BlockBlast.Core
{
    /// <summary>
    /// 게임 내에서 사용할 수 있는 아이템의 종류를 정의합니다.
    /// </summary>
    public enum ItemType
    {
        None = 0,

        // --- 1. 즉발형 아이템 (Blast 시 즉시 발동) ---
        Bomb3x3 = 1,          // 3x3 범위 폭발
        HorizontalBlast = 2,  // 가로 1줄 전체 폭발
        VerticalBlast = 3,    // 세로 1줄 전체 폭발
        TimeBonus10s = 4,     // 시간 +10초 추가

        // --- 2. 일반/인벤토리 아이템 (Blast 시 아이템창 저장 후 원하는 시점 사용) ---
        BoardClean = 5,       // 전체 보드 클린
        HandReset = 6,        // 손패 3개 리셋
        ScoreDouble10s = 7    // 10초간 점수 2배 이벤트
    }

    /// <summary>
    /// 아이템의 카테고리(즉발형 vs 인벤토리 보관형)를 정의합니다.
    /// </summary>
    public enum ItemCategory
    {
        None = 0,
        Instant = 1,    // 즉발형
        Inventory = 2   // 인벤토리 보관형
    }

    /// <summary>
    /// 게임의 전반적인 진행 상태를 정의합니다.
    /// </summary>
    public enum GameState
    {
        MainMenu,   // 메인 타이틀 화면
        Playing,    // 인게임 플레이 중
        Paused,     // 인게임 일시정지 팝업
        GameOver    // 게임 오버
    }

    /// <summary>
    /// 아이템 타입에 따른 유틸리티 메서드를 제공하는 헬퍼 클래스입니다.
    /// </summary>
    public static class ItemHelper
    {
        /// <summary>
        /// 아이템의 카테고리(즉발형/인벤토리형)를 반환합니다.
        /// </summary>
        /// <param name="itemType">분류할 아이템 타입입니다.</param>
        public static ItemCategory GetCategory(this ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Bomb3x3:
                case ItemType.HorizontalBlast:
                case ItemType.VerticalBlast:
                case ItemType.TimeBonus10s:
                    return ItemCategory.Instant;

                case ItemType.BoardClean:
                case ItemType.HandReset:
                case ItemType.ScoreDouble10s:
                    return ItemCategory.Inventory;

                default:
                    return ItemCategory.None;
            }
        }

        /// <summary>
        /// 아이템의 표시용 한글 이름을 반환합니다.
        /// </summary>
        /// <param name="itemType">이름을 조회할 아이템 타입입니다.</param>
        public static string GetItemName(this ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Bomb3x3:
                    return "3x3 폭발";

                case ItemType.HorizontalBlast:
                    return "가로 폭발";

                case ItemType.VerticalBlast:
                    return "세로 폭발";

                case ItemType.TimeBonus10s:
                    return "+10초 추가";

                case ItemType.BoardClean:
                    return "전체 클린";

                case ItemType.HandReset:
                    return "손패 리셋";

                case ItemType.ScoreDouble10s:
                    return "2배 점수 (10초)";

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 스프라이트 에셋이 없을 때 사용할 기본 이모지 아이콘을 반환합니다.
        /// </summary>
        /// <param name="itemType">아이콘을 조회할 아이템 타입입니다.</param>
        public static string GetItemIconEmoji(this ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Bomb3x3:
                    return "💣";

                case ItemType.HorizontalBlast:
                    return "↔️";

                case ItemType.VerticalBlast:
                    return "↕️";

                case ItemType.TimeBonus10s:
                    return "⏱️";

                case ItemType.BoardClean:
                    return "🧹";

                case ItemType.HandReset:
                    return "🔄";

                case ItemType.ScoreDouble10s:
                    return "✖️2";

                default:
                    return string.Empty;
            }
        }
    }
}
