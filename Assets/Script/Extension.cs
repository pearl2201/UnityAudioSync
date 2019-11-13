using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
public class Extension
{
    public static List<string> AutoBreakText(string sentence)
    {
        string newsentence = sentence;
        if (newsentence.StartsWith("- "))
        {
            newsentence = newsentence.Substring(2);
        }

        string[] words = newsentence.Split(' ');
        Debug.Log(words.Length);
        List<string> data = new List<string>();
        if (words.Length <= 1)
        {
            data.Add(sentence);
        }
        else
        {
            if (sentence.StartsWith("- "))
            {
                words[0] = "- " + words[0];
            }
            data.AddRange(words);
        }
        return data;
    }

    public static List<string> ManualBreakText(string sentence)
    {
        return sentence.Split('|').ToList();
    }

    public static List<Color> CreateColor(List<string> words)
    {
        List<Color> colortags = new List<Color>();
        Color colorStart = Color.HSVToRGB(0, 0.5f, 0.5f);
        Color colorEnd = Color.HSVToRGB(1, 0.5f, 0.5f);
        Debug.Log("colorStart: " + colorStart);
        Debug.Log("colorEnd: " + colorEnd);
        if (words.Count == 1)
        {
            colortags.Add(colorStart);
        }else if (words.Count ==2 )
        {
            colortags.Add(Color.red);
            colortags.Add(Color.cyan);
        }
        else
        {
            for (int i = 0; i < words.Count; i++)
            {
                colortags.Add(Color.HSVToRGB(Mathf.Lerp(0f, 1f, i / (words.Count - 1f)), 0.5f, 0.5f));
            }
        }

        return colortags;
    }

    public static string RebuildColorText(List<string> words, List<Color> colortags)
    {
        StringBuilder sb = new StringBuilder();
        //string highlighterOpenTag = "<color=#FFA10D>";
        //string normalizeOpenTag = "<color=#000000>";


        for (int i = 0; i < words.Count; i++)
        {
            sb.AppendFormat("<mark=#{0}>{1} ", ColorUtility.ToHtmlStringRGBA(colortags[i]), words[i]);
        }


        return sb.ToString().Trim();
    }
}
