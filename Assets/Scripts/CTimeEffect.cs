using System.Collections;
using UnityEngine;

// 활성화 시 애니메이션을 재생하고 끝나면 자동 비활성화 시키는 클래스.
[RequireComponent(typeof(Animation))]
public class CTimeEffect : MonoBehaviour
{
    [SerializeField] Light staffLight;
    [SerializeField] Animation anim;
    Coroutine disableCoroutine;

    private void Awake()
    {
        anim = GetComponent<Animation>();
        gameObject.SetActive(false);
    }
    public void Activate()
    {
        staffLight.enabled = true;
        gameObject.SetActive(true);
        anim?.Play();
        
        if(disableCoroutine != null)
            StopCoroutine(disableCoroutine);
        disableCoroutine = StartCoroutine(DisableAtEndOfAnimation());
    }
    IEnumerator DisableAtEndOfAnimation()
    {
        yield return new WaitForSeconds(anim.clip.length);
        gameObject.SetActive(false);
        staffLight.enabled = false;
    }
}
