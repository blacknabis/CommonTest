using UnityEngine;

namespace Kingdom.App
{
    /// <summary>
    /// 앱 시작 시 KingdomAppManager 싱글톤을 생성하는 부트스트래퍼.
    /// [RuntimeInitializeOnLoadMethod]를 통해 첫 씬 로드 전에 실행됩니다.
    /// </summary>
    public static class KingdomBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Debug.Log("[KingdomBootstrapper] Bootstrapping...");
            
            // AppManager 싱글톤을 생성하여 sceneLoaded 콜백 등록
            var manager = KingdomAppManager.Instance;
            manager.OnAppStart();
        }
    }
}
