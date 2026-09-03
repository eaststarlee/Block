using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast.Core
{
    /// <summary>
    /// 게임에서 사용되는 기본 폴리오미노(Polyomino) 블록 모양 데이터 카탈로그를 관리하는 정적 클래스입니다.
    /// </summary>
    public static class ShapeCatalog
    {
        #region Static Color Palette

        public static readonly Color ColorBlue = new Color(0.18f, 0.55f, 0.95f, 1f);
        public static readonly Color ColorCyan = new Color(0.12f, 0.78f, 0.88f, 1f);
        public static readonly Color ColorGreen = new Color(0.25f, 0.78f, 0.35f, 1f);
        public static readonly Color ColorYellow = new Color(0.96f, 0.76f, 0.15f, 1f);
        public static readonly Color ColorOrange = new Color(0.95f, 0.48f, 0.15f, 1f);
        public static readonly Color ColorPurple = new Color(0.62f, 0.32f, 0.88f, 1f);
        public static readonly Color ColorRed = new Color(0.92f, 0.28f, 0.32f, 1f);
        public static readonly Color ColorPink = new Color(0.92f, 0.4f, 0.65f, 1f);

        #endregion

        #region Private Static Fields

        private static List<BlockShapeData> s_definitions;

        #endregion

        #region Public Properties

        /// <summary>
        /// 전체 정의된 블록 모양 목록입니다.
        /// </summary>
        public static IReadOnlyList<BlockShapeData> Definitions
        {
            get
            {
                if (s_definitions == null || s_definitions.Count == 0)
                {
                    InitializeCatalog();
                }

                return s_definitions;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 모든 블록 모양(14종)의 지오메트리 및 색상 정의를 초기화합니다.
        /// </summary>
        public static void InitializeCatalog()
        {
            s_definitions = new List<BlockShapeData>();

            // 1. 점 (1x1)
            AddShape("Dot_1x1", 1, 1, new[] { new Vector2Int(0, 0) }, ColorYellow);

            // 2. 2칸 막대
            AddShape("Line_2x1_H", 2, 1, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, ColorCyan);
            AddShape("Line_1x2_V", 1, 2, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }, ColorCyan);

            // 3. 3칸 막대
            AddShape("Line_3x1_H", 3, 1, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, ColorBlue);
            AddShape("Line_1x3_V", 1, 3, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, ColorBlue);

            // 4. 4칸 막대
            AddShape("Line_4x1_H", 4, 1, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }, ColorPurple);
            AddShape("Line_1x4_V", 1, 4, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) }, ColorPurple);

            // 5. 5칸 막대
            AddShape("Line_5x1_H", 5, 1, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) }, ColorRed);
            AddShape("Line_1x5_V", 1, 5, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) }, ColorRed);

            // 6. 사각형 (2x2)
            AddShape("Square_2x2", 2, 2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, ColorYellow);

            // 7. 대형 사각형 (3x3)
            AddShape("Square_3x3", 3, 3, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
            }, ColorRed);

            // 8. 2x2 작은 L (3칸 코너)
            AddShape("Corner_2x2_0", 2, 2, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, ColorGreen);
            AddShape("Corner_2x2_1", 2, 2, new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, ColorGreen);
            AddShape("Corner_2x2_2", 2, 2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }, ColorGreen);
            AddShape("Corner_2x2_3", 2, 2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) }, ColorGreen);

            // 9. L자 (3x2, 4칸)
            AddShape("L_3x2_0", 2, 3, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2) }, ColorOrange);
            AddShape("L_3x2_1", 2, 3, new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 2) }, ColorOrange);
            AddShape("L_3x2_2", 3, 2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1) }, ColorOrange);
            AddShape("L_3x2_3", 3, 2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1) }, ColorOrange);

            // 10. J자 (3x2, 4칸)
            AddShape("J_3x2_0", 2, 3, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, ColorOrange);
            AddShape("J_3x2_1", 2, 3, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2) }, ColorOrange);
            AddShape("J_3x2_2", 3, 2, new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(0, 0) }, ColorOrange);
            AddShape("J_3x2_3", 3, 2, new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 0) }, ColorOrange);

            // 11. T자 (3x2, 4칸)
            AddShape("T_3x2_Up", 3, 2, new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }, ColorPurple);
            AddShape("T_3x2_Down", 3, 2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) }, ColorPurple);
            AddShape("T_2x3_Left", 2, 3, new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 2) }, ColorPurple);
            AddShape("T_2x3_Right", 2, 3, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2) }, ColorPurple);

            // 12. Z & S자 (3x2, 4칸)
            AddShape("Z_3x2", 3, 2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) }, ColorPink);
            AddShape("S_3x2", 3, 2, new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, ColorPink);
            AddShape("Z_2x3_V", 2, 3, new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2) }, ColorPink);
            AddShape("S_2x3_V", 2, 3, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 2) }, ColorPink);

            // 13. 대형 L (3x3, 5칸 코너)
            AddShape("BigCorner_3x3_0", 3, 3, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) }, ColorGreen);
            AddShape("BigCorner_3x3_1", 3, 3, new[] { new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(0, 2), new Vector2Int(1, 2) }, ColorGreen);
            AddShape("BigCorner_3x3_2", 3, 3, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, ColorGreen);
            AddShape("BigCorner_3x3_3", 3, 3, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2) }, ColorGreen);

            // 14. 십자 (Plus, 5칸)
            AddShape("Plus_3x3", 3, 3, new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) }, ColorCyan);
        }

        /// <summary>
        /// 카탈로그에 등록된 블록 중 무작위 1개의 복제본을 반환합니다.
        /// </summary>
        public static BlockShapeData GetRandomShape()
        {
            if (s_definitions == null || s_definitions.Count == 0)
            {
                InitializeCatalog();
            }

            int index = Random.Range(0, s_definitions.Count);
            return s_definitions[index].Clone();
        }

        #endregion

        #region Private Methods

        private static void AddShape(string id, int width, int height, Vector2Int[] cells, Color color)
        {
            s_definitions.Add(new BlockShapeData(id, width, height, cells, color));
        }

        #endregion
    }
}
