using Kingdom.App;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class KingdomRegressionMenu
    {
        [MenuItem("Tools/Kingdom/Run WorldMap Meta Popup Regression (20)")]
        private static void RunWorldMapMetaPopupRegression20()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[KingdomRegressionMenu] Enter Play Mode first.");
                return;
            }

            KingdomAppManager manager = Object.FindFirstObjectByType<KingdomAppManager>();
            if (manager == null)
            {
                Debug.LogWarning("[KingdomRegressionMenu] KingdomAppManager not found in active scene.");
                return;
            }

            manager.StartWorldMapMetaPopupRegression(20, 0.08f);
        }

        [MenuItem("Tools/Kingdom/Run Meta Persistence + Hero Apply Regression")]
        private static void RunMetaPersistenceAndHeroApplyRegression()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[KingdomRegressionMenu] Enter Play Mode first.");
                return;
            }

            KingdomAppManager manager = Object.FindFirstObjectByType<KingdomAppManager>();
            if (manager == null)
            {
                Debug.LogWarning("[KingdomRegressionMenu] KingdomAppManager not found in active scene.");
                return;
            }

            manager.StartMetaPersistenceAndHeroApplyRegression();
        }

        [MenuItem("Tools/Kingdom/Run Hero Role Smoke Regression")]
        private static void RunHeroRoleSmokeRegression()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[KingdomRegressionMenu] Enter Play Mode first.");
                return;
            }

            KingdomAppManager manager = Object.FindFirstObjectByType<KingdomAppManager>();
            if (manager == null)
            {
                Debug.LogWarning("[KingdomRegressionMenu] KingdomAppManager not found in active scene.");
                return;
            }

            manager.StartHeroRoleSmokeRegression();
        }

        [MenuItem("Tools/Kingdom/Run Combat Integration Smoke Regression")]
        private static void RunCombatIntegrationSmokeRegression()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[KingdomRegressionMenu] Enter Play Mode first.");
                return;
            }

            KingdomAppManager manager = Object.FindFirstObjectByType<KingdomAppManager>();
            if (manager == null)
            {
                Debug.LogWarning("[KingdomRegressionMenu] KingdomAppManager not found in active scene.");
                return;
            }

            manager.StartCombatIntegrationSmokeRegression();
        }

        [MenuItem("Tools/Kingdom/Run Barracks Melee Smoke Regression")]
        private static void RunBarracksMeleeSmokeRegression()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[KingdomRegressionMenu] Enter Play Mode first.");
                return;
            }

            KingdomAppManager manager = Object.FindFirstObjectByType<KingdomAppManager>();
            if (manager == null)
            {
                Debug.LogWarning("[KingdomRegressionMenu] KingdomAppManager not found in active scene.");
                return;
            }

            manager.StartBarracksMeleeSmokeRegression();
        }
    }
}
