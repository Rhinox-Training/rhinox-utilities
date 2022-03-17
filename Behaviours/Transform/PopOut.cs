using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopOut : MonoBehaviour
{
    public float Duration = 0.4f;
    public float StartRatio = 0.5f;
    Vector3 _startScale;

    void Start()
    {
        _startScale = transform.localScale;
        transform.localScale = Vector3.zero;
        //Pop();
    }

    public void Pop()
    {
        StartCoroutine(PopRoutine());
    }

    public void Reset()
    {
        transform.localScale = Vector3.zero;
    }

    IEnumerator PopRoutine()
    {
        var time = Duration;
        transform.localScale = _startScale * StartRatio;
        while (time > 0)
        {
            var ratio = 1 - (time / Duration) * (1 - StartRatio);
            transform.localScale = _startScale * ratio;
            time -= Time.deltaTime;
            yield return null;
        }

        transform.localScale = _startScale;
    }
}
