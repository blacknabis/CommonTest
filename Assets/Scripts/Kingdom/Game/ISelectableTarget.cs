using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// 클릭 선택이 가능한 대상에 대한 공통 인터페이스.
    /// </summary>
    public interface ISelectableTarget
    {
        /// <summary>
        /// 화면에 표시될 이름 (예: 슬라임, 아처 타워 등)
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 대상의 현재 월드 위치
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// 현재 체력 비율 (0.0 ~ 1.0)
        /// </summary>
        float HpRatio { get; }

        /// <summary>
        /// 현재 체력 값
        /// </summary>
        float CurrentHp { get; }

        /// <summary>
        /// 최대 체력 값
        /// </summary>
        float MaxHp { get; }

        /// <summary>
        /// 현재 선택 대상의 공격력 표기값
        /// </summary>
        float AttackPower { get; }

        /// <summary>
        /// 현재 선택 대상의 방어력 표기값
        /// </summary>
        float DefensePower { get; }

        /// <summary>
        /// 생존 여부
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 유닛 타입 (Enemy, Hero, Tower, Soldier 등)
        /// </summary>
        string UnitType { get; }

        /// <summary>
        /// 선택 시점에 호출 (피드백 활성화 등)
        /// </summary>
        void OnSelected();

        /// <summary>
        /// 선택 해제 시점에 호출 (피드백 비활성화 등)
        /// </summary>
        void OnDeselected();
    }
}
