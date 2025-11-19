using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public class bootstrapaer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Bootstrap());
    }
    private IEnumerator Bootstrap()
    {
        while (YG2.isSDKEnabled == false)
        {
            yield return new WaitForSeconds(1f);
        }
        SceneManager.LoadScene(1);
    }
}
