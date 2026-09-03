using System;
using System.IO;
using UnityEngine;

namespace BlockBlast.Core
{
    /// <summary>
    /// 모바일 샌드박스 파일 시스템 및 PlayerPrefs를 활용해 플레이어 데이터를 안전하게 영구 저장하고 관리하는 매니저 클래스입니다.
    /// </summary>
    public sealed class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "savedata.json";
        private const string PlayerPrefsBackupKey = "BlockBlast_SaveData_Json";

        #region Static Fields

        private static SaveManager s_instance;

        #endregion

        #region Private Fields

        private PlayerData _data = new PlayerData();
        private string _saveFilePath;

        #endregion

        #region Events

        public event Action<PlayerData> OnDataLoaded;
        public event Action<PlayerData> OnDataSaved;
        public event Action<int> OnHighScoreUpdated;
        public event Action<bool> OnAudioMuteChanged;
        public event Action<bool> OnVibrationSettingChanged;

        #endregion

        #region Public Properties

        public static SaveManager Instance => s_instance;

        public PlayerData Data => _data;

        public int HighScore => _data.HighScore;
        public bool IsAudioMuted => _data.IsAudioMuted;
        public bool IsVibrationEnabled => _data.IsVibrationEnabled;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            LoadData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // 모바일에서 앱이 백그라운드로 내려갈 때 자동 저장
            if (pauseStatus)
            {
                SaveData();
            }
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }

        #endregion

        #region Public Methods - Save and Load

        /// <summary>
        /// 파일 시스템 또는 PlayerPrefs로부터 저장 데이터를 로드합니다.
        /// </summary>
        public void LoadData()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    string json = File.ReadAllText(_saveFilePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        _data = JsonUtility.FromJson<PlayerData>(json);
                    }
                }
                else if (PlayerPrefs.HasKey(PlayerPrefsBackupKey))
                {
                    string backupJson = PlayerPrefs.GetString(PlayerPrefsBackupKey);
                    if (!string.IsNullOrEmpty(backupJson))
                    {
                        _data = JsonUtility.FromJson<PlayerData>(backupJson);
                    }
                }
                else
                {
                    _data = new PlayerData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to load data: {ex.Message}. Initializing new data.");
                _data = new PlayerData();
            }

            OnDataLoaded?.Invoke(_data);
        }

        /// <summary>
        /// 현재 플레이어 데이터를 파일 시스템 및 PlayerPrefs에 즉시 영구 저장합니다.
        /// </summary>
        public void SaveData()
        {
            if (_data == null)
            {
                return;
            }

            try
            {
                _data.LastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonUtility.ToJson(_data, true);

                File.WriteAllText(_saveFilePath, json);
                PlayerPrefs.SetString(PlayerPrefsBackupKey, json);
                PlayerPrefs.Save();

                OnDataSaved?.Invoke(_data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save data: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods - Record and Settings Updates

        /// <summary>
        /// 최고 점수를 비교하여 신기록일 경우 갱신하고 저장합니다.
        /// </summary>
        /// <param name="newScore">달성한 점수입니다.</param>
        /// <returns>신기록 달성 여부를 반환합니다.</returns>
        public bool UpdateHighScore(int newScore)
        {
            if (_data == null)
            {
                return false;
            }

            if (newScore > _data.HighScore)
            {
                _data.HighScore = newScore;
                SaveData();
                OnHighScoreUpdated?.Invoke(_data.HighScore);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 오디오 음소거(Mute) 상태를 설정하고 저장합니다.
        /// </summary>
        /// <param name="isMuted">음소거 여부입니다.</param>
        public void SetAudioMuted(bool isMuted)
        {
            if (_data == null)
            {
                return;
            }

            _data.IsAudioMuted = isMuted;
            SaveData();
            OnAudioMuteChanged?.Invoke(_data.IsAudioMuted);
        }

        /// <summary>
        /// 모바일 진동(Haptics) 활성화 여부를 설정하고 저장합니다.
        /// </summary>
        /// <param name="isEnabled">진동 활성화 여부입니다.</param>
        public void SetVibrationEnabled(bool isEnabled)
        {
            if (_data == null)
            {
                return;
            }

            _data.IsVibrationEnabled = isEnabled;
            SaveData();
            OnVibrationSettingChanged?.Invoke(_data.IsVibrationEnabled);
        }

        /// <summary>
        /// 모바일 기기 진동 피드백을 트리거합니다. (설정에서 활성화되어 있을 때만 실행)
        /// </summary>
        public void TriggerVibration()
        {
            if (_data == null || !_data.IsVibrationEnabled)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// 모든 저장 데이터를 기본값으로 초기화합니다.
        /// </summary>
        public void ResetAllData()
        {
            _data = new PlayerData();
            SaveData();
            OnDataLoaded?.Invoke(_data);
            OnHighScoreUpdated?.Invoke(_data.HighScore);
            OnAudioMuteChanged?.Invoke(_data.IsAudioMuted);
            OnVibrationSettingChanged?.Invoke(_data.IsVibrationEnabled);
        }

        #endregion
    }
}
