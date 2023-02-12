using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MRTCount : MonoBehaviour
{
    private string mrtCount;
    private Text t;
    void Start()
    {
        t = GetComponent<Text>();
        mrtCount = "MRTCount: " + SystemInfo.supportedRenderTargetCount;
    }

    // Update is called once per frame
    void Update()
    {
        float fps = 1.0f / Time.deltaTime;
        t.text = mrtCount + "\n" + "FPS: " + fps;
    }
}
