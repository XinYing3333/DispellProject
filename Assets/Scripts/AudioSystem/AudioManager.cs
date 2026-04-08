using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("AudioSources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource sfxLoopSource;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    public string bgmParam = "BGM";
    public string sfxParam = "SFX";

    [Header("Volume UI Sliders")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    private Dictionary<SFXType, AudioClip> sfxLibrary = new();
    private Dictionary<BGMType, AudioClip> bgmLibrary = new();

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    
    [Header("Testing on start")]
    public BGMType testBGM;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioClips();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    private void Start()
    {
        LoadVolumePref();
        ApplyVolumes();
        
        PlayBGM(testBGM);
    }

    private void Update()
    {
        ApplyVolumes();
    }

    public void OnSceneLoaded()
    {
        // 重新找到當前場景的 Slider（根據名稱或 Tag）
        //bgmSlider = GameObject.FindGameObjectWithTag("BGMSlider").GetComponent<Slider>();
        //sfxSlider = GameObject.FindGameObjectWithTag("SFXSlider").GetComponent<Slider>();

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            bgmSlider.value = bgmVolume;
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxSlider.value = sfxVolume;
        }

        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }

    private void LoadAudioClips()
    {
        foreach (SFXType sfx in System.Enum.GetValues(typeof(SFXType)))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audios/SFX/{sfx}");
            if (clip != null)
                sfxLibrary[sfx] = clip;
        }

        foreach (BGMType bgm in System.Enum.GetValues(typeof(BGMType)))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audios/BGM/{bgm}");
            if (clip != null)
                bgmLibrary[bgm] = clip;
        }
    }

    public void LoadVolumePref()
    {
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);
    }

    private void ApplyVolumes()
    {
        if (bgmSlider != null) bgmSlider.value = bgmVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
        
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }

    public void PlayBGM(BGMType bgmType, float fadeDuration = 0.5f)
    {
        if (bgmLibrary.TryGetValue(bgmType, out AudioClip bgmClip))
        {
            // 若相同音軌正在播放則跳過
            if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return;

            // 使用 Sequence 處理漸層序列
            Sequence bgmSequence = DOTween.Sequence();

            if (bgmSource.isPlaying)
            {
                // 1. 漸淡出
                bgmSequence.Append(bgmSource.DOFade(0, fadeDuration).SetEase(Ease.Linear));
                bgmSequence.AppendCallback(() =>
                {
                    bgmSource.clip = bgmClip;
                    bgmSource.Play();
                });
            }
            else
            {
                // 若目前沒音樂，直接初始化新音軌
                bgmSource.clip = bgmClip;
                bgmSource.volume = 0;
                bgmSource.Play();
            }

            // 2. 漸淡入 (目標音量為 1，因為實際音量受 AudioMixer 控制)
            bgmSequence.Append(bgmSource.DOFade(1, fadeDuration).SetEase(Ease.Linear));
        }
        else
        {
            Debug.LogWarning($"BGM {bgmType} 未找到！");
        }
    }

    public void StopBGM() => bgmSource.Stop();

    public void PlaySFXLoop(SFXType sfxType)
    {
        if (sfxLibrary.TryGetValue(sfxType, out AudioClip sfxClip))
        {
            sfxLoopSource.clip = sfxClip;
            sfxLoopSource.loop = true;
            sfxLoopSource.Play();
        }
        else
        {
            Debug.LogWarning($"SFX {sfxType} 未找到！");
        }
    }

    public void StopSFXLoop() => sfxLoopSource.Stop();

    public void Test()//---------------------------------------------------------------------------------
    {
        PlaySFX(SFXType.Click);
    }

    public void PlaySFX(SFXType sfxType)
    {
        if (sfxLibrary.TryGetValue(sfxType, out AudioClip sfxClip))
            sfxSource.PlayOneShot(sfxClip);
        else
            Debug.LogWarning($"SFX {sfxType} 未找到！");
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = value;
        float dB = Mathf.Lerp(-80f, 0f, value);
        audioMixer.SetFloat(bgmParam, dB);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        float dB = Mathf.Lerp(-80f, 0f, value);
        audioMixer.SetFloat(sfxParam, dB);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public void SetSelectSFX()
    {
        PlaySFX(SFXType.Click);
    }
}
