using System.Security.Cryptography;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.UI;

public static class ScreenInfo
{
    public static Vector2 AspectRatio = new Vector2(Config.AspectRatio.x, Config.AspectRatio.y);
    public static float ScreenDistance { get; private set; }
    public static float ScreenSize { get; private set; }
    public static string Teleoptic { get; private set; }
    public static StereoFormat Format { get; private set; }
    public static float HIT { get; private set; }

    
    public static void UpdateScreenVaues(float distance, float size, float hit)
    {
        ScreenDistance = distance;
        ScreenSize = size;
        HIT = hit;
    }

    public static void SetTeleoptic()
    {
        if (Config.Monoscopic)
        {
            Teleoptic = "Monoscopic";
        }
        else
        {
            Teleoptic = "Stereoscopic";
        }
    }

    public static void SetFormatInfo(StereoFormat format)
    {
        Format = format;
    }
}

public class GuiPanel : MonoBehaviour
{
    private class Info
    {
        public string Name { get; private set; }
        public float Distance { get; private set; }
        public Vector2 Size { get; private set; }

        public void UpdateInfo(string name, float distance, Vector2 size)
        {
            this.Name = name;
            this.Distance = distance;
            this.Size = size;
        }
    }

    private ScreenControlls _screens;
    private Text _text;
    private Info _info;
    // Use this for initialization

    private void Awake()
    {
        _screens = FindObjectOfType<ScreenControlls>();
        _screens.GuiTextUpdate = UpdateGuiText;
        _info = new Info();
        _text = gameObject.GetComponentInChildren<Text>();
    }

    
    private void UpdateGuiText()
    {
        string newText;
        if (Config.CleanPreset == true)
        {
            var s = new Vector2(Config.CurrentPreset.ScreenSize*Config.AspectRatioNorm.x, Config.CurrentPreset.ScreenSize*Config.AspectRatioNorm.z);
            this._info.UpdateInfo(Config.CurrentPreset.Name, Config.CurrentPreset.ScreenDistance, s);
        }
        else
        {
            var s = new Vector2(_screens.ScreenSize * Config.AspectRatioNorm.x, _screens.ScreenSize * Config.AspectRatioNorm.z);
            this._info.UpdateInfo("No Preset", _screens.ScreenDistance, s);
            
        }
        newText = "Preset: " + _info.Name + "\nDistance: " + _info.Distance + "\nSize: " + _info.Size.ToString();
        _text.text = newText;
    }
}