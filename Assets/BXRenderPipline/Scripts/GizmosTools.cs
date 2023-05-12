using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmosTools : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Camera cam = GetComponent<Camera>();
        int tileXCount = Mathf.CeilToInt(cam.pixelWidth / 16f);
        int tileYCount = Mathf.CeilToInt(cam.pixelHeight / 16f);
        int tileZCount = 256;
        for(int z = 0; z < tileZCount; ++z)
        {
            for(int y = 0; y < tileYCount; ++y)
            {
                for(int x = 0; x < tileXCount; ++x)
                {
                    float zValue = z * cam.farClipPlane / 256f;
                    float height_half = Mathf.Tan(cam.fieldOfView * 0.5f) * zValue;
                    float width_half = height_half * cam.aspect;
                    float yValue = -height_half + y * height_half / 8f;
                    float xValue = -width_half * x * width_half / 8f;
                    Vector3 offset = cam.transform.forward * zValue + cam.transform.right * xValue + cam.transform.up * yValue;
                    Gizmos.DrawWireCube(cam.transform.position + offset,
                        new Vector3(width_half / 8f, height_half / 8f, cam.farClipPlane / 256f)
                        );
                }
            }
        }
    }
}
