using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    public static class TextUtils
    {
        private static Dictionary<Color, string> colorParentDic = new Dictionary<Color, string>();

        public static string GetColoredString(Color color, string str)
        {
            if (!colorParentDic.ContainsKey(color))
            {
                colorParentDic.Add(color, "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">");
            }
            return colorParentDic[color] + str + "</color>";
        }
    }
}
