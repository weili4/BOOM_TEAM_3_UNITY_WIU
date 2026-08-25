using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AUDIO MIXER GROUPS")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("BGM AUDIO SOURCES FOR CROSSFADING")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;

    private AudioSource activeBgmSource;
    private Coroutine crossfadeRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        if (bgmSourceA == null) bgmSourceA = gameObject.AddComponent<AudioSource>();
        if (bgmSourceB == null) bgmSourceB = gameObject.AddComponent<AudioSource>();

        bgmSourceA.loop = true;
        bgmSourceB.loop = true;

        if (bgmMixerGroup != null)
        {
            bgmSourceA.outputAudioMixerGroup = bgmMixerGroup;
            bgmSourceB.outputAudioMixerGroup = bgmMixerGroup;
        }

        activeBgmSource = bgmSourceA;
    }

    public void PlayMusic(AudioClip newTrack, float fadeDuration = 0.4f)
    {
        if (newTrack == null) return;

        // check if new track is already playing
        if (activeBgmSource != null && activeBgmSource.isPlaying && activeBgmSource.clip == newTrack)
        {
            return;
        }

        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
        }

        crossfadeRoutine = StartCoroutine(CrossfadeMusicRoutine(newTrack, fadeDuration));
    }

    public void StopMusic(float fadeDuration = 0.4f)
    {
        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
        }

        crossfadeRoutine = StartCoroutine(FadeOutMusicRoutine(fadeDuration));
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip newTrack, float fadeDuration)
    {
        AudioSource newSource = (activeBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
        AudioSource oldSource = activeBgmSource;

        newSource.clip = newTrack;
        newSource.time = 0f; // ALWAYS START NEW SONG FROM THE VERY BEGINNING
        newSource.volume = 0f;
        newSource.Play();

        float elapsed = 0f;
        float startOldVolume = oldSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;

            newSource.volume = Mathf.Lerp(0f, 1f, progress);
            oldSource.volume = Mathf.Lerp(startOldVolume, 0f, progress);

            yield return null;
        }

        newSource.volume = 1f;
        oldSource.volume = 0f;
        oldSource.Stop();

        activeBgmSource = newSource;
    }

    private IEnumerator FadeOutMusicRoutine(float fadeDuration)
    {
        if (activeBgmSource == null || !activeBgmSource.isPlaying) yield break;

        float startVolume = activeBgmSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            activeBgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        activeBgmSource.volume = 0f;
        activeBgmSource.Stop();
        activeBgmSource.clip = null;
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        GameObject sfxObj = new GameObject("TempSFX_" + clip.name);
        sfxObj.transform.position = position;

        AudioSource audioSource = sfxObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;

        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        audioSource.Play();
        Destroy(sfxObj, clip.length);
    }

    public void SetMasterVolume(float sliderValue)
    {
        if (mainAudioMixer == null) return;
        float db = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        mainAudioMixer.SetFloat("MasterVolume", db);
    }

    public void SetBGMVolume(float sliderValue)
    {
        if (mainAudioMixer == null) return;
        float db = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        mainAudioMixer.SetFloat("BGMVolume", db);
    }

    public void SetSFXVolume(float sliderValue)
    {
        if (mainAudioMixer == null) return;
        float db = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        mainAudioMixer.SetFloat("SFXVolume", db);
    }
}