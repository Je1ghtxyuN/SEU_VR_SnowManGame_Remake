using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PetVoiceSystem : MonoBehaviour
{
    public static PetVoiceSystem Instance { get; private set; }

    // 供 PetController 查询：是否正在说话 (用于决定是否张嘴或转向玩家)
    public bool IsSpeaking
    {
        get { return audioSource != null && audioSource.isPlaying; }
    }

    [System.Serializable]
    public struct VoiceClip
    {
        public string id;
        public AudioClip clip;
        [TextArea] public string subtitle;
    }

    private struct QueuedVoice
    {
        public string id;
        public float delay;

        public QueuedVoice(string id, float delay)
        {
            this.id = id;
            this.delay = delay;
        }
    }

    [Header("语音库配置")]
    public List<VoiceClip> voiceLibrary = new List<VoiceClip>();

    [Header("随机语音设置")]
    [Tooltip("请在这里填入所有4个发现敌人的语音ID，例如: Enemy_Spotted, Enemy_1, Enemy_2, Enemy_3")]
    public string[] spottedVoiceIDs;

    [Header("音量设置")]
    [Range(0f, 2f)]
    public float masterVolume = 1.0f;

    // --- 内部变量 ---
    private AudioSource audioSource;
    private Queue<QueuedVoice> voiceQueue = new Queue<QueuedVoice>();
    private bool isProcessingQueue = false;

    // 状态记录
    private float lastEnemySpottedTime = -999f;
    private bool hasPlayedFirstDrop = false;
    private bool hasPlayedFirstEnemySpotted = false; // ⭐ 新增：记录是否遇到过第一个敌人

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        audioSource = GetComponent<AudioSource>();

        audioSource.spatialBlend = 1.0f; // 3D 音效
        audioSource.minDistance = 5.0f;
        audioSource.maxDistance = 100.0f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    // --- 核心播放接口 ---
    public void PlayVoice(string id, float delay = 0f)
    {
        voiceQueue.Enqueue(new QueuedVoice(id, delay));
        if (!isProcessingQueue) StartCoroutine(ProcessVoiceQueue());
    }

    // ⭐ 修改后的发现敌人逻辑
    public void TryPlayEnemySpottedVoice()
    {
        // 15秒冷却时间，防止太吵
        if (Time.time - lastEnemySpottedTime > 15f)
        {
            // 情况 A: 第一次发现敌人 -> 强制播放经典语音 "Enemy_Spotted"
            if (!hasPlayedFirstEnemySpotted)
            {
                PlayVoice("Enemy_Spotted");
                hasPlayedFirstEnemySpotted = true; // 标记为已播放
            }
            // 情况 B: 之后 -> 在随机列表中抽取
            else
            {
                if (spottedVoiceIDs != null && spottedVoiceIDs.Length > 0)
                {
                    int randomIndex = Random.Range(0, spottedVoiceIDs.Length);
                    string randomID = spottedVoiceIDs[randomIndex];
                    PlayVoice(randomID);
                }
                else
                {
                    // 如果列表忘填了，兜底播放原来的
                    PlayVoice("Enemy_Spotted");
                }
            }

            lastEnemySpottedTime = Time.time;
        }
    }

    public void TryPlayFirstDropVoice()
    {
        if (!hasPlayedFirstDrop)
        {
            PlayVoice("First_Drop");
            hasPlayedFirstDrop = true;
        }
    }

    // --- 队列处理逻辑 ---
    private IEnumerator ProcessVoiceQueue()
    {
        isProcessingQueue = true;
        while (voiceQueue.Count > 0)
        {
            QueuedVoice currentRequest = voiceQueue.Dequeue();
            if (currentRequest.delay > 0) yield return new WaitForSeconds(currentRequest.delay);

            VoiceClip target = voiceLibrary.Find(x => x.id == currentRequest.id);

            if (target.clip != null)
            {
                audioSource.PlayOneShot(target.clip, masterVolume);
                // Debug.Log($"[Elf]: {target.subtitle}");
                yield return new WaitForSeconds(target.clip.length + 0.2f); // 语音长度+0.2秒缓冲
            }
            else
            {
                Debug.LogWarning($"[PetVoice] 找不到语音ID: {currentRequest.id}");
                yield return null;
            }
        }
        isProcessingQueue = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        StopAllCoroutines();
    }
}