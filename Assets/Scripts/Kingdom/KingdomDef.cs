namespace Kingdom.App
{
    /// <summary>
    /// 프로젝트에서 사용하는 씬 열거형.
    /// 씬 파일 이름 및 Scene 클래스 이름과 일치해야 합니다.
    /// </summary>
    public enum SCENES
    {
        InitScene,      // 초기화 씬
        TitleScene,     // 타이틀/메인메뉴 씬
        WorldMapScene,  // 월드맵 (스테이지 선택, 영웅, 업그레이드)
        GameScene       // 인게임 전투 씬
    }
}
