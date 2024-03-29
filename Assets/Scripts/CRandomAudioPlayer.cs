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

    public bool randomizePitch = true;                  // ���� pitch ������ ����� ���ΰ�? (= ������� ǳ��������)
    public float pitchRandomRange = 0.2f;               // ���� pitch ���� ��.
    public float playDelay = 0f;                        // ����� ����� �ð�
    public SoundBank defaultPack = new SoundBank();     // �⺻ ���� ����� ��.
    public MaterialAudioOverride[] overrides;            // �ؽ�ó�� ���� override ����� ��.

    private AudioSource audioSource;                    // ����� ������Ʈ.
    private Dictionary<Material, SoundBank[]> lookup;     // lookup-table (�̸� �з��ؼ� �ۼ��� �� �ڷᱸ��)

    [HideInInspector]
    public bool isPlaying;
    [HideInInspector]
    public bool canPlay;

    private void Awake()
    {
        // ����� ������Ʈ�� �˻��� �� �����Ŭ�� �迭�� lookup ���̺�� �ۼ�
        // (=> �˻� �ð��� �ſ� ����Ǳ� �����̴�)
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

        // ���޵� material�� ���� ��� lookup���� pack�� ã�´�.
        if(overrideMaterial != null)
        {
            if (lookup.TryGetValue(overrideMaterial, out SoundBank[] banks))
                clips = banks[bankID].clips;
        }

        // ���� lookup���� ã�� ���ߴٸ� �⺻ pack�� ����Ѵ�.
        if (clips == null)
            clips = defaultPack.clips;

        // random pitch������ Ȯ���� pitch�� �����Ѵ�.
        audioSource.pitch = randomizePitch ? Random.Range(1 - pitchRandomRange, 1 + pitchRandomRange) : 1f;
        audioSource.clip = clips.GetRandom();
        audioSource.Play();
    }
}
