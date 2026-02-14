using System.Collections.Generic;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 저장된 스테이지 진행 데이터를 읽기 위한 추상화 인터페이스입니다.
    /// </summary>
    public interface IStageProgressRepository
    {
        IReadOnlyDictionary<int, StageProgressSnapshot> GetProgressMap();
        int GetTotalEarnedStars();
    }
}
