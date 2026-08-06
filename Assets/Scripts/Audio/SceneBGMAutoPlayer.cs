using UnityEngine;
using CardGame.Audio;

namespace CardGame.Audio
{
    /// <summary>
    /// 挂在场景的Camera或空GameObject上，场景加载后自动播放对应BGM。
    /// 不依赖SceneChanger，任何方式进入场景都会触发。
    /// </summary>
    public class SceneBGMAutoPlayer : MonoBehaviour
    {
        [SerializeField] private SceneBGM sceneBGM = SceneBGM.Menu;

        private void Start()
        {
            if (GameAudioManager.Instance != null)
            {
                GameAudioManager.Instance.PlayBGM(sceneBGM);
                Debug.Log($"[Audio] SceneBGMAutoPlayer: PlayBGM({sceneBGM}) in scene {gameObject.scene.name}");
            }
            else
            {
                Debug.LogWarning($"[Audio] SceneBGMAutoPlayer: GameAudioManager.Instance is null in scene {gameObject.scene.name}");
            }
        }
    }
}
