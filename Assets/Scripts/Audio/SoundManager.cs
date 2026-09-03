using System;
using BlockBlast.Core;
using BlockBlast.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockBlast.Audio
{
    /// <summary>
    /// 게임 내 효과음(블록 배치, 폭탄 폭발, 아이템 획득 및 일반 아이템별 개별 사용음, 게임오버)의 재생을 관리하는 오디오 매니저 클래스입니다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundManager : MonoBehaviour
    {
        #region Static Fields

        private static SoundManager s_instance;

        #endregion

        #region Serialized Fields

        [Header("Audio Settings")]
        [Tooltip("효과음 마스터 볼륨 (0.0 ~ 1.0)입니다.")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("masterVolume")]
        [FormerlySerializedAs("_masterVolume")]
        [SerializeField] private float _masterVolume = 1.0f;

        [Header("Sound Manager")]
        [Tooltip("Block Place - 블록을 보드에 배치할 때 재생할 효과음입니다.")]
        [FormerlySerializedAs("customClipPlace")]
        [FormerlySerializedAs("_customClipPlace")]
        [SerializeField] private AudioClip _blockPlaceClip;

        [Tooltip("Bomb - 3x3 폭탄 및 가로/세로 관통 폭탄 폭발 시 재생할 효과음입니다.")]
        [FormerlySerializedAs("customClipBomb")]
        [FormerlySerializedAs("_customClipBomb")]
        [SerializeField] private AudioClip _bombClip;

        [Tooltip("Item Get - 인벤토리 아이템을 획득했을 때 재생할 효과음입니다.")]
        [FormerlySerializedAs("customClipItemGain")]
        [FormerlySerializedAs("_customClipItemGain")]
        [SerializeField] private AudioClip _itemGetClip;

        [Tooltip("Item - Board Clean (보드 전체 클리어 아이템 사용 시 재생할 효과음입니다.)")]
        [FormerlySerializedAs("boardCleanClip")]
        [FormerlySerializedAs("_boardCleanClip")]
        [SerializeField] private AudioClip _itemBoardCleanClip;

        [Tooltip("Item - Hand Reset (손패 리셋 아이템 사용 시 재생할 효과음입니다.)")]
        [FormerlySerializedAs("handResetClip")]
        [FormerlySerializedAs("_handResetClip")]
        [SerializeField] private AudioClip _itemHandResetClip;

        [Tooltip("Item - Score Double (10초간 점수 2배 버프 아이템 사용 시 재생할 효과음입니다.)")]
        [FormerlySerializedAs("scoreDoubleClip")]
        [FormerlySerializedAs("_scoreDoubleClip")]
        [SerializeField] private AudioClip _itemScoreDoubleClip;

        [Tooltip("Game Over - 게임오버 시 재생할 효과음입니다.")]
        [FormerlySerializedAs("customClipGameOver")]
        [FormerlySerializedAs("_customClipGameOver")]
        [SerializeField] private AudioClip _gameOverClip;

        #endregion

        #region Private Fields

        private AudioSource _audioSource;

        #endregion

        #region Public Properties

        public static SoundManager Instance => s_instance;

        public float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = Mathf.Clamp01(value);
        }

        public AudioClip BlockPlaceClip => _blockPlaceClip;
        public AudioClip BombClip => _bombClip;
        public AudioClip ItemGetClip => _itemGetClip;
        public AudioClip ItemBoardCleanClip => _itemBoardCleanClip;
        public AudioClip ItemHandResetClip => _itemHandResetClip;
        public AudioClip ItemScoreDoubleClip => _itemScoreDoubleClip;
        public AudioClip GameOverClip => _gameOverClip;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
            }
            else if (s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                var gm = GameManager.Instance;
                if (gm.Grid != null)
                {
                    gm.Grid.OnBlockPlaced += (shape, x, y) => PlayBlockPlace();
                }

                if (gm.Items != null)
                {
                    gm.Items.OnInstantItemTriggered += (item, msg) =>
                    {
                        if (item == ItemType.Bomb3x3 || item == ItemType.HorizontalBlast || item == ItemType.VerticalBlast)
                        {
                            PlayBomb();
                        }
                    };

                    gm.Items.OnInventoryUpdated += (inventory) =>
                    {
                        if (inventory.Count > 0)
                        {
                            PlayItemGet();
                        }
                    };

                    gm.Items.OnInventoryItemUsed += (item) => PlayInventoryItemUse(item);
                }

                gm.OnGameOver += (reason, score) => PlayGameOver();
            }
        }

        #endregion

        #region Public Playback Methods

        /// <summary>
        /// Block Place 효과음을 재생합니다.
        /// </summary>
        public void PlayBlockPlace()
        {
            PlaySound(_blockPlaceClip, 0.7f);
        }

        /// <summary>
        /// Bomb 폭발 효과음을 재생합니다.
        /// </summary>
        public void PlayBomb()
        {
            PlaySound(_bombClip, 1.0f);
        }

        /// <summary>
        /// Item Get 효과음을 재생합니다.
        /// </summary>
        public void PlayItemGet()
        {
            PlaySound(_itemGetClip, 0.8f);
        }

        /// <summary>
        /// 일반(인벤토리) 아이템 타입에 따른 개별 사용 효과음을 재생합니다.
        /// </summary>
        /// <param name="itemType">사용된 인벤토리 아이템 종류입니다.</param>
        public void PlayInventoryItemUse(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.BoardClean:
                    PlaySound(_itemBoardCleanClip, 0.9f);
                    break;

                case ItemType.HandReset:
                    PlaySound(_itemHandResetClip, 0.9f);
                    break;

                case ItemType.ScoreDouble10s:
                    PlaySound(_itemScoreDoubleClip, 0.9f);
                    break;
            }
        }

        /// <summary>
        /// Game Over 효과음을 재생합니다.
        /// </summary>
        public void PlayGameOver()
        {
            PlaySound(_gameOverClip, 0.85f);
        }

        /// <summary>
        /// 지정한 AudioClip을 마스터 볼륨을 적용하여 원샷 재생합니다. 미할당(null) 시 재생되지 않습니다.
        /// </summary>
        /// <param name="clip">재생할 AudioClip입니다.</param>
        /// <param name="volumeScale">개별 볼륨 스케일 (0.0 ~ 1.0)입니다.</param>
        public void PlaySound(AudioClip clip, float volumeScale = 1f)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip, volumeScale * _masterVolume);
            }
        }

        #endregion
    }
}
