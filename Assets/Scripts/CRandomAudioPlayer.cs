using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class CRandomAudioPlayer : MonoBehaviour
{
    [System.Serializable] public class MaterialAudioOverride
    {
        public Material[] mateiral;
        public SoundBank[] banks;
    }
    [System.Serializable] public class SoundBank
    {
        public string name;
        public AudioClip[] clips;
    }

    public bool randomizePitch = true;                  // 랜덤 pitch 조율을 허용할 것인가? (= 오디오가 풍부해진다)
    public float pitchRandomRange = 0.2f;               // 랜덤 pitch 오차 값.
    public float playDelay = 0f;                        // 재생시 대기할 시간
    public SoundBank defaultPack = new SoundBank();     // 기본 랜덤 오디오 팩.
    public MaterialAudioOverride[] overrides;            // 텍스처에 따른 override 오디오 팩.

    private AudioSource audioSource;                    // 오디오 컴포넌트.
    private Dictionary<Material, SoundBank[]> lookup;     // lookup-table (미리 분류해서 작성해 둔 자료구조)

    [HideInInspector]
    public bool isPlaying;
    [HideInInspector]
    public bool canPlay;

    private void Awake()
    {
        // 오디오 컴포넌트를 검색한 뒤 오디오클립 배열을 lookup 테이블로 작성
        // (=> 검색 시간이 매우 단축되기 때문이다)
        audioSource = GetComponent<AudioSource>();
        lookup = new Dictionary<Material, SoundBank[]>();
        for(int i = 0; i<overrides.Length; i++)
        {
            foreach (var material in overrides[i].mateiral)
                lookup[material] = overrides[i].banks;
        }
    }

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayRandomClip()
    {
        PlayClip(null, 0);
    }
    public void PlayRandomClip(Material material, int bankID = 0)
    {
        PlayClip(material, bankID);
    }

    private void PlayClip(Material overrideMaterial, int bankID)
    {
        AudioClip[] clips = null;

        // 전달된 material이 있을 경우 lookup에서 pack을 찾는다.
        if(overrideMaterial != null)
        {
            if (lookup.TryGetValue(overrideMaterial, out SoundBank[] banks))
                clips = banks[bankID].clips;
        }

        // 만약 lookup에서 찾지 못했다면 기본 pack을 사용한다.
        if (clips == null)
            clips = defaultPack.clips;

        // random pitch인지를 확인해 pitch를 조정한다.
        audioSource.pitch = randomizePitch ? Random.Range(1 - pitchRandomRange, 1 + pitchRandomRange) : 1f;
        audioSource.clip = clips.GetRandom();
        audioSource.Play();
    }
}
