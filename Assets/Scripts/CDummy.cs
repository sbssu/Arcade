using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CDummy : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer bodyRenderer;
    [SerializeField] float dissolveTime;

    private void Start()
    {
        bodyRenderer.material.SetFloat("_Dissolve", 0f);
    }

    public void OnDeath()
    {
        StartCoroutine(IEDissolve());
    }

    private IEnumerator IEDissolve()
    {
        float time = 0f;
        while(time < dissolveTime)
        {
            time += Time.deltaTime;
            bodyRenderer.material.SetFloat("_Dissolve", time / dissolveTime);
            yield return null;
        }
        Destroy(gameObject);
    }
}
