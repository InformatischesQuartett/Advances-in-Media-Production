using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ScreenInfo
{
    public static float ScreenDistance { get; private set; }
    public static float ScreenSize { get; private set; }
    public static string Teleoptic { get; private set; }
    public static StereoFormat Format { get; private set; }
    public static float HIT { get; private set; }
    public static Vector2 AspectRatio = new Vector2(Config.AspectRatio.x, Config.AspectRatio.y);

    public static void UpdateScreenVaues(float distance, float size, float hit)
    {
        ScreenDistance = distance;
        ScreenSize = size;
        HIT = hit;
    }

    public static void SetTeleoptic()
    {
        if (Config.Monoscopic == true)
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
    private ScreenControlls _screens;
    private Text _text;
    private Toggle[] toggles;

    private Config.PresetSet _big, _small, _tv;
    // Use this for initialization
    private void Start()
    {
        _big = new Config.PresetSet {_screenDistance = 20.0f, _screenSize = 20.0f};
        _small = new Config.PresetSet { _screenDistance = 15.0f, _screenSize = 5 };
        _tv = new Config.PresetSet { _screenDistance = 2.5f, _screenSize = 0.7f };

        _screens = FindObjectOfType<ScreenControlls>();
        _text = gameObject.GetComponentInChildren<Text>();
        _text.text = "\nStereoFormat: " + ScreenInfo.Format +
                     "\nAspect Ratio: " + (int)ScreenInfo.AspectRatio.x + " : " + (int)ScreenInfo.AspectRatio.y +
                     "\nScreen distance: " + ScreenInfo.ScreenDistance +
                     "\nScreen size: " + ScreenInfo.ScreenSize + " x " + (ScreenInfo.AspectRatio.y * (ScreenInfo.ScreenSize / ScreenInfo.AspectRatio.x)) +
                     "\nHIT: " + ScreenInfo.HIT;

        toggles = gameObject.GetComponentsInChildren<Toggle>();


    }

    private void Update()
    {
        _text.text = "\nStereoFormat: " + ScreenInfo.Format +
                     "\nAspect Ratio: " + (int)ScreenInfo.AspectRatio.x + " : " +(int)ScreenInfo.AspectRatio.y+
                     "\nScreen distance: " + ScreenInfo.ScreenDistance +
                     "\nScreen size: " + ScreenInfo.ScreenSize + " x " + (ScreenInfo.AspectRatio.y * (ScreenInfo.ScreenSize / ScreenInfo.AspectRatio.x)) +
                     "\nHIT: " + ScreenInfo.HIT;

        SelectPreset();

    }

    private void SelectPreset()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            //set Rpeset to Ciname big
            toggles[0].isOn = true;
            toggles[1].isOn = false;
            toggles[2].isOn = false;
            SetScreen(_big._screenDistance, _big._screenSize);
        }
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            toggles[0].isOn = false;
            toggles[1].isOn = true;
            toggles[2].isOn = false;
            SetScreen(_small._screenDistance, _small._screenSize);
        }
        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            toggles[0].isOn = false;
            toggles[1].isOn = false;
            toggles[2].isOn = true;
            SetScreen(_tv._screenDistance, _tv._screenSize);
        }
        if (Input.GetAxis("Screen Distance") != 0.0f || Input.GetAxis("Screen Size") != 0.0f)
        {
            foreach (var toggle in toggles)
            {
                toggle.isOn = false;
            }
        }
    }

    private void SetScreen(float distance, float size)
    {
        _screens.ScreenDistance = distance;
        _screens.ScreenSize = size;
    }

    public void Click()
    {
        Debug.Log("Clicked");
    }

}