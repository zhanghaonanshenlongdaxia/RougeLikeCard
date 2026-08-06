using UnityEngine;

namespace CardGame.Audio
{
    /// <summary>
    /// 简单音频管理器：管理BGM循环播放和SFX一次性播放。
    /// 挂载在场景中的持久GameObject上，或通过DontDestroyOnLoad保持。
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager Instance { get; private set; }

        [Header("BGM Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip battleBGM;
        [SerializeField] private AudioClip baseBGM;
        [SerializeField] private AudioClip mapBGM;
        [SerializeField] private AudioClip menuBGM;

        [SerializeField] private AudioClip uiClickSFX;
        [SerializeField] private AudioClip swordSlashSFX;
        [SerializeField] private AudioClip takeDamageSFX;
        [SerializeField] private AudioClip shieldBlockSFX;
        [SerializeField] private AudioClip drawCardSFX;
        [SerializeField] private AudioClip deathSFX;

        [Header("Settings")]
        [Range(0f, 1f)] public float bgmVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.Log($"[Audio] Duplicate GameAudioManager destroyed in scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[Audio] GameAudioManager.Awake in scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.volume = bgmVolume;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.volume = sfxVolume;
            }

            // Auto-load audio clips if not assigned
            LoadClips();
            Debug.Log($"[Audio] Clips loaded: battle={battleBGM!=null} base={baseBGM!=null} map={mapBGM!=null} menu={menuBGM!=null}");

            // Auto-play menu BGM on first load
            PlayBGM(SceneBGM.Menu);
            Debug.Log($"[Audio] PlayBGM(Menu) called, bgmSource.playing={bgmSource.isPlaying}");
        }

        private void LoadClips()
        {
            battleBGM = battleBGM ?? LoadClip("Assets/Res/Audio/BGM/battle_bgm.wav");
            baseBGM = baseBGM ?? LoadClip("Assets/Res/Audio/BGM/base_bgm.wav");
            mapBGM = mapBGM ?? LoadClip("Assets/Res/Audio/BGM/map_bgm.wav");
            menuBGM = menuBGM ?? LoadClip("Assets/Res/Audio/BGM/menu_bgm.wav");

            uiClickSFX = uiClickSFX ?? LoadClip("Assets/Res/Audio/SFX/ui_click.wav");
            swordSlashSFX = swordSlashSFX ?? LoadClip("Assets/Res/Audio/SFX/sword_slash.wav");
            takeDamageSFX = takeDamageSFX ?? LoadClip("Assets/Res/Audio/SFX/take_damage.wav");
            shieldBlockSFX = shieldBlockSFX ?? LoadClip("Assets/Res/Audio/SFX/shield_block.wav");
            drawCardSFX = drawCardSFX ?? LoadClip("Assets/Res/Audio/SFX/draw_card.wav");
            deathSFX = deathSFX ?? LoadClip("Assets/Res/Audio/SFX/death.wav");
        }

        private AudioClip LoadClip(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
#else
            return null;
#endif
        }

        #region BGM Control
        public void PlayBGM(SceneBGM bgm)
        {
            AudioClip clip = bgm switch
            {
                SceneBGM.Menu => menuBGM,
                SceneBGM.Base => baseBGM,
                SceneBGM.Map => mapBGM,
                SceneBGM.Battle => battleBGM,
                _ => null
            };

            if (clip == null || bgmSource == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        public void SetBGMVolume(float vol)
        {
            bgmVolume = vol;
            if (bgmSource != null) bgmSource.volume = vol;
        }
        #endregion

        #region SFX Control
        public void PlaySFX(SFXType sfx)
        {
            AudioClip clip = sfx switch
            {
                SFXType.UIClick => uiClickSFX,
                SFXType.SwordSlash => swordSlashSFX,
                SFXType.TakeDamage => takeDamageSFX,
                SFXType.ShieldBlock => shieldBlockSFX,
                SFXType.DrawCard => drawCardSFX,
                SFXType.Death => deathSFX,
                _ => null
            };

            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = vol;
        }
        #endregion
    }

    public enum SceneBGM { Menu, Base, Map, Battle }
    public enum SFXType { UIClick, SwordSlash, TakeDamage, ShieldBlock, DrawCard, Death }
}
