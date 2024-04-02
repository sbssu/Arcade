using System.Collections;
using UnityEngine;

// Ȱ��ȭ �� �ִϸ��̼��� ����ϰ� ������ �ڵ� ��Ȱ��ȭ ��Ű�� Ŭ����.
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
