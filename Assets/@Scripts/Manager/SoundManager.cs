using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

public enum SoundType
{
    None,
    Car,
    Footstep,
    Grilling,
    Increase,
    LevelUp,
    MoneyGet,
    Stack,
    StackCustomer,
    TrashStack,
    TrashThrow,
    Upgrade,
}

[System.Serializable]
public class SoundClipData
{
    public SoundType soundType;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float defaultVolume = 1f;
    [Range(0f, 2f)]
    public float minInterval = 0.05f;
    
    [Header("리듬 설정")]
    public bool useRhythm = false;
    [Range(0.5f, 2f)]
    public float minPitch = 0.8f;
    [Range(0.5f, 2f)]
    public float maxPitch = 1.2f;
    [Range(0.1f, 2f)]
    public float minVolumeMultiplier = 0.7f;
    [Range(0.1f, 2f)]
    public float maxVolumeMultiplier = 1.3f;
    [Range(0f, 0.2f)]
    public float rhythmTimeVariation = 0.05f; // 타이밍 변화
}

[System.Serializable]
public class RhythmPattern
{
    [Range(0.5f, 2f)]
    public float[] pitchPattern;
    [Range(0.1f, 2f)]
    public float[] volumePattern;
    public float[] timingPattern; // 각 재생 간의 추가 딜레이
}

public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSourcePrefab;
    
    [Header("Sound Settings")]
    [SerializeField] private List<SoundClipData> soundClips;
    [SerializeField] private int initialPoolSize = 5;
    [SerializeField] private int maxPoolSize = 20;
    
    [Header("리듬 패턴")]
    [SerializeField] private List<RhythmPattern> rhythmPatterns;
    
    // 성능 최적화를 위한 구조들
    private Dictionary<SoundType, SoundClipData> clipDataDict = new Dictionary<SoundType, SoundClipData>();
    private Queue<AudioSource> availableSources = new Queue<AudioSource>();
    private List<AudioSource> activeSources = new List<AudioSource>();
    
    // 중복 재생 방지
    private Dictionary<SoundType, float> lastPlayTimes = new Dictionary<SoundType, float>();
    
    // 현재 재생 중인 특정 사운드 관리 (OneShot 방식)
    private Dictionary<SoundType, AudioSource> playingSounds = new Dictionary<SoundType, AudioSource>();
    
    // 리듬 관련 변수
    private Dictionary<SoundType, int> rhythmCounters = new Dictionary<SoundType, int>();
    private Dictionary<SoundType, int> patternIndices = new Dictionary<SoundType, int>();
    
    private bool isInitialized = false;

    #region Initialization
	private void Awake()
	{
	    if (!isInitialized)
	    {
	        InitializeSoundManager();
	        isInitialized = true;
	    }
	}

	private void InitializeSoundManager()
	{
	    foreach (var soundData in soundClips)
	    {
	        if (soundData.clip != null)
	        {
	            clipDataDict[soundData.soundType] = soundData;
	            lastPlayTimes[soundData.soundType] = -1f;
	            rhythmCounters[soundData.soundType] = 0;
	            patternIndices[soundData.soundType] = 0;
	        }
	    }
	    
	    for (int i = 0; i < initialPoolSize; i++)
	    {
	        CreateNewAudioSource();
	    }
	}
	#endregion

	#region Audio Source Pool Management
	private AudioSource CreateNewAudioSource()
	{
	    AudioSource newSource = Instantiate(sfxSourcePrefab, transform);
	    newSource.playOnAwake = false;
	    availableSources.Enqueue(newSource);
	    return newSource;
	}

	private AudioSource GetAvailableAudioSource()
	{
	    if (availableSources.Count > 0)
	    {
	        AudioSource source = availableSources.Dequeue();
	        activeSources.Add(source);
	        return source;
	    }
	    
	    if (activeSources.Count + availableSources.Count < maxPoolSize)
	    {
	        AudioSource newSource = CreateNewAudioSource();
	        availableSources.Dequeue();
	        activeSources.Add(newSource);
	        return newSource;
	    }
	    
	    AudioSource oldestSource = activeSources[0];
	    activeSources.RemoveAt(0);
	    oldestSource.Stop();
	    activeSources.Add(oldestSource);
	    return oldestSource;
	}

	private void ReturnAudioSourceToPool(AudioSource source)
	{
	    if (activeSources.Remove(source))
	    {
	        source.clip = null;
	        source.volume = 1f;
	        source.pitch = 1f;
	        availableSources.Enqueue(source);
	    }
	}
	#endregion

	#region Sound Playback
	public void PlaySfx(SoundType soundType, float volumeMultiplier = 1f)
	{
	    if (!clipDataDict.TryGetValue(soundType, out SoundClipData soundData))
	    {
	        Debug.LogWarning($"Sound {soundType} not found in clipDataDict");
	        return;
	    }
	    
	    float currentTime = Time.unscaledTime;
	    if (lastPlayTimes.TryGetValue(soundType, out float lastTime))
	    {
	        if (currentTime - lastTime < soundData.minInterval)
	            return;
	    }

	    lastPlayTimes[soundType] = currentTime;

	    AudioSource source = GetAvailableAudioSource();
	    source.clip = soundData.clip;
	    
	    if (soundData.useRhythm)
	    {
	        ApplyRhythmToSource(source, soundData, soundType, volumeMultiplier);
	    }
	    else
	    {
	        source.volume = soundData.defaultVolume * volumeMultiplier;
	        source.pitch = 1f;
	    }

	    source.Play();
	    
	    StartCoroutine(ReturnToPoolWhenFinished(source));
	}

	public void PlaySfxExclusive(SoundType soundType, float volumeMultiplier = 1f)
	{
	    if (!clipDataDict.TryGetValue(soundType, out SoundClipData soundData))
	    {
	        Debug.LogWarning($"Sound {soundType} not found in clipDataDict");
	        return;
	    }
	    
	    if (playingSounds.TryGetValue(soundType, out AudioSource currentSource))
	    {
	        if (currentSource != null && currentSource.isPlaying)
	        {
	            currentSource.Stop();
	            ReturnAudioSourceToPool(currentSource);
	        }
	    }

	    AudioSource source = GetAvailableAudioSource();
	    source.clip = soundData.clip;
	    
	    if (soundData.useRhythm)
	    {
	        ApplyRhythmToSource(source, soundData, soundType, volumeMultiplier);
	    }
	    else
	    {
	        source.volume = soundData.defaultVolume * volumeMultiplier;
	        source.pitch = 1f;
	    }
	    
	    source.Play();

	    playingSounds[soundType] = source;
	    
	    StartCoroutine(ReturnToPoolWhenFinishedExclusive(source, soundType));
	}
	#endregion

	#region Rhythm System
	private void ApplyRhythmToSource(AudioSource source, SoundClipData soundData, SoundType soundType, float volumeMultiplier)
	{
	    int counter = rhythmCounters[soundType];
	    
	    float pitch = 1f;
	    float volume = soundData.defaultVolume * volumeMultiplier;
	    
	    if (rhythmPatterns.Count > 0 && patternIndices.ContainsKey(soundType))
	    {
	        int patternIndex = patternIndices[soundType];
	        if (patternIndex < rhythmPatterns.Count)
	        {
	            var pattern = rhythmPatterns[patternIndex];
	            
	            if (pattern.pitchPattern != null && pattern.pitchPattern.Length > 0)
	            {
	                pitch = pattern.pitchPattern[counter % pattern.pitchPattern.Length];
	            }
	            
	            if (pattern.volumePattern != null && pattern.volumePattern.Length > 0)
	            {
	                volume *= pattern.volumePattern[counter % pattern.volumePattern.Length];
	            }
	        }
	    }
	    else
	    {
	        pitch = Random.Range(soundData.minPitch, soundData.maxPitch);
	        volume *= Random.Range(soundData.minVolumeMultiplier, soundData.maxVolumeMultiplier);
	    }
	    
	    source.pitch = pitch;
	    source.volume = volume;
	    
	    rhythmCounters[soundType] = (counter + 1) % 8;
	}

	public void SetRhythmPattern(SoundType soundType, int patternIndex)
	{
	    if (patternIndex >= 0 && patternIndex < rhythmPatterns.Count)
	    {
	        patternIndices[soundType] = patternIndex;
	    }
	}

	public void ResetRhythmCounter(SoundType soundType)
	{
	    if (rhythmCounters.ContainsKey(soundType))
	    {
	        rhythmCounters[soundType] = 0;
	    }
	}

	public void ResetAllRhythmCounters()
	{
	    foreach (var key in rhythmCounters.Keys.ToArray())
	    {
	        rhythmCounters[key] = 0;
	    }
	}

	public Coroutine PlayRhythmSequence(SoundType soundType, int count, float baseInterval = 0.1f, float volumeMultiplier = 1f)
	{
	    return StartCoroutine(PlayRhythmSequenceCoroutine(soundType, count, baseInterval, volumeMultiplier));
	}

	private IEnumerator PlayRhythmSequenceCoroutine(SoundType soundType, int count, float baseInterval, float volumeMultiplier)
	{
	    if (!clipDataDict.TryGetValue(soundType, out SoundClipData soundData))
	    {
	        yield break;
	    }

	    for (int i = 0; i < count; i++)
	    {
	        AudioSource source = GetAvailableAudioSource();
	        source.clip = soundData.clip;
	        
	        if (soundData.useRhythm)
	        {
	            ApplyRhythmToSource(source, soundData, soundType, volumeMultiplier);
	            
	            float timingVariation = Random.Range(-soundData.rhythmTimeVariation, soundData.rhythmTimeVariation);
	            float waitTime = baseInterval + timingVariation;
	            
	            source.Play();
	            StartCoroutine(ReturnToPoolWhenFinished(source));
	            
	            if (i < count - 1)
	            {
	                yield return new WaitForSeconds(waitTime);
	            }
	        }
	        else
	        {
	            source.volume = soundData.defaultVolume * volumeMultiplier;
	            source.pitch = 1f;
	            source.Play();
	            StartCoroutine(ReturnToPoolWhenFinished(source));
	            
	            if (i < count - 1)
	            {
	                yield return new WaitForSeconds(baseInterval);
	            }
	        }
	    }
	}
	#endregion

	#region BGM Control
	public void PlayBgm(SoundType soundType, float volumeMultiplier = 1f, bool loop = true)
	{
	    if (!clipDataDict.TryGetValue(soundType, out SoundClipData soundData))
	    {
	        Debug.LogWarning($"BGM {soundType} not found in clipDataDict");
	        return;
	    }

	    bgmSource.clip = soundData.clip;
	    bgmSource.volume = soundData.defaultVolume * volumeMultiplier;
	    bgmSource.loop = loop;
	    bgmSource.Play();
	}

	public void StopBgm()
	{
	    bgmSource.Stop();
	}
	#endregion

	#region Stop Controls
	public void StopAllSfx()
	{
	    foreach (var source in activeSources)
	    {
	        source.Stop();
	    }
	    
	    while (activeSources.Count > 0)
	    {
	        AudioSource source = activeSources[0];
	        activeSources.RemoveAt(0);
	        ReturnAudioSourceToPool(source);
	    }
	    
	    playingSounds.Clear();
	}

	public void StopSfx(SoundType soundType)
	{
	    if (playingSounds.TryGetValue(soundType, out AudioSource source))
	    {
	        if (source != null && source.isPlaying)
	        {
	            source.Stop();
	            ReturnAudioSourceToPool(source);
	            playingSounds.Remove(soundType);
	        }
	    }
	}
	#endregion

	#region Coroutines
	private IEnumerator ReturnToPoolWhenFinished(AudioSource source)
	{
	    yield return new WaitWhile(() => source.isPlaying);
	    ReturnAudioSourceToPool(source);
	}

	private IEnumerator ReturnToPoolWhenFinishedExclusive(AudioSource source, SoundType soundType)
	{
	    yield return new WaitWhile(() => source.isPlaying);
	    
	    if (playingSounds.TryGetValue(soundType, out AudioSource registeredSource) && 
	        registeredSource == source)
	    {
	        playingSounds.Remove(soundType);
	    }
	    
	    ReturnAudioSourceToPool(source);
	}
	#endregion

	#region Debug
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public void LogPoolStatus()
	{
	    Debug.Log($"Available Sources: {availableSources.Count}, Active Sources: {activeSources.Count}");
	}
	#endregion
}