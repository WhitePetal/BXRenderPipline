using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MRTCount : MonoBehaviour
{
    private string mrtCount;
    private Text t;

    private int count;
    private float deltaTime;

    void Start()
    {
        t = GetComponent<Text>();
        mrtCount = "MRTCount: " + SystemInfo.supportedRenderTargetCount;
    }

    // Update is called once per frame
    void Update()
    {
        if(count < 60)
		{
            count++;
            deltaTime += Time.deltaTime;
		}
        else
		{
            float fps = count / deltaTime;
            deltaTime = 0f;
            count = 0;
            t.text = mrtCount + "\n" + "FPS: " + fps;
		}
    }
}
