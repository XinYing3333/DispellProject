using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("AudioSources")]
    public AudioSource bgmSource; // 背景音樂播放器
    public AudioSource sfxSource; // 音效播放器
    public AudioSource sfxLoopSource; // 音效播放器

    private Dictionary<SFXType, AudioClip> sfxLibrary = new Dictionary<SFXType, AudioClip>();
    private Dictionary<BGMType, AudioClip> bgmLibrary = new Dictionary<BGMType, AudioClip>();

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 確保音頻管理器不被刪除
            LoadAudioClips();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // **自動加載 Resources/Audio 內的音效**
    private void LoadAudioClips()
    {
        foreach (SFXType sfx in System.Enum.GetValues(typeof(SFXType)))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audios/SFX/{sfx}");
            if (clip != null)
            {
                sfxLibrary[sfx] = clip;
            }
        }

        foreach (BGMType bgm in System.Enum.GetValues(typeof(BGMType)))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audios/BGM/{bgm}");
            if (clip != null)
            {
                bgmLibrary[bgm] = clip;
            }
        }
    }

    // **播放 BGM**
    public void PlayBGM(BGMType bgmType)
    {
        if (bgmLibrary.TryGetValue(bgmType, out AudioClip bgmClip))
        {
            if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return; // 避免重複播放
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = musicVolume;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM {bgmType} 未找到！");
        }
    }
    
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // **播放音效**
    public void PlaySFXLoop(SFXType sfxType)
    {
        if (sfxLibrary.TryGetValue(sfxType, out AudioClip sfxClip))
        {
            sfxLoopSource.clip = sfxClip;
            sfxLoopSource.volume = sfxVolume;
            sfxLoopSource.Play();
        }
        else
        {
            Debug.LogWarning($"SFX {sfxType} 未找到！");
        }
    }
    
    public void StopSFXLoop()
    {
        sfxLoopSource.Stop();
    }
    
    public void PlaySFX(SFXType sfxType)
    {
        if (sfxLibrary.TryGetValue(sfxType, out AudioClip sfxClip))
        {
            sfxSource.PlayOneShot(sfxClip, sfxVolume);
        }
        else
        {
            Debug.LogWarning($"SFX {sfxType} 未找到！");
        }
    }

    // **調整音量**
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        bgmSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
}
