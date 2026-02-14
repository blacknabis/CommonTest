using Common.Patterns;
using UnityEngine;

namespace Kingdom.Save
{
    /// <summary>
    /// UserSaveData 단일 인스턴스를 관리하는 글로벌 저장 매니저.
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        private UserSaveData _saveData;

        public UserSaveData SaveData
        {
            get
            {
                if (_saveData == null)
                {
                    _saveData = new UserSaveData();
                }

                return _saveData;
            }
        }

        protected override void OnSingletonAwake()
        {
            _saveData = new UserSaveData();
            Debug.Log("[SaveManager] Initialized.");
        }

        public void Reload()
        {
            _saveData = new UserSaveData();
        }

        public void ResetAll()
        {
            SaveData.ResetAll();
        }
    }
}
