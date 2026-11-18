using System;
using System.Collections.Generic;

namespace StudyXR.Strike
{
    /// <summary>
    /// 판 위 주사위 면들로 "같은 숫자 짝"을 판정하는 규칙. Unity에 의존하지 않는다.
    /// 턴 진행(#5 JudgeBoard)이 장외·X를 뺀 면 목록을 넘기면 딸 숫자를 돌려주고,
    /// AI(#7)는 다음 1개가 짝이 될 확률을 멈춤 판단에 쓴다.
    /// </summary>
    public static class DiceRules
    {
        // 숫자 면 종류 수 (One~Five 5개 + X 1개 = 6, 시트의 확률 분모)
        private const int FaceKindCount = 6;

        /// <summary>
        /// 2개 이상 나온 숫자를 찾는다. 딴 숫자의 주사위를 판에서 모두 가져가는 근거가 된다.
        /// </summary>
        /// <param name="boardFaces">X·장외를 뺀 판 위 주사위 면들. 순서 무관.</param>
        /// <returns>짝이 된 숫자 목록 (One→Five 순). 없으면 빈 목록.</returns>
        public static List<DiceFace> FindPairedFaces(IReadOnlyList<DiceFace> boardFaces)
        {
            Span<int> countByFace = stackalloc int[FaceKindCount];
            CountFaces(boardFaces, countByFace);
            List<DiceFace> pairedFaces = new List<DiceFace>();

            for (int faceIndex = 0; faceIndex < countByFace.Length; faceIndex++)
            {
                bool isNumberFace = (DiceFace)faceIndex != DiceFace.X;
                bool isPaired = countByFace[faceIndex] >= 2;

                if (isNumberFace && isPaired)
                {
                    pairedFaces.Add((DiceFace)faceIndex);
                }
            }

            return pairedFaces;
        }

        /// <summary>
        /// 다음 주사위 1개가 판 위 숫자와 짝이 될 확률. 판에 서로 다른 숫자가 많을수록 높다.
        /// </summary>
        /// <param name="boardFaces">X·장외를 뺀 판 위 주사위 면들.</param>
        /// <returns>0~1. 판이 비면 0.</returns>
        public static float MatchChance(IReadOnlyList<DiceFace> boardFaces)
        {
            Span<int> countByFace = stackalloc int[FaceKindCount];
            CountFaces(boardFaces, countByFace);
            int distinctNumberCount = 0;

            for (int faceIndex = 0; faceIndex < countByFace.Length; faceIndex++)
            {
                bool isNumberFace = (DiceFace)faceIndex != DiceFace.X;
                bool isOnBoard = countByFace[faceIndex] > 0;

                if (isNumberFace && isOnBoard)
                {
                    distinctNumberCount++;
                }
            }

            return (float)distinctNumberCount / FaceKindCount;
        }

        /// <summary>
        /// 면 종류별 개수를 센다. 인덱스는 <see cref="DiceFace"/> 값.
        /// </summary>
        /// <param name="boardFaces">세어 볼 판 위 면들.</param>
        /// <param name="countByFace">결과를 채울 배열. 길이 <see cref="FaceKindCount"/>, 0으로 시작해야 한다.</param>
        private static void CountFaces(IReadOnlyList<DiceFace> boardFaces, Span<int> countByFace)
        {
            for (int index = 0; index < boardFaces.Count; index++)
            {
                countByFace[(int)boardFaces[index]]++;
            }
        }
    }
}
