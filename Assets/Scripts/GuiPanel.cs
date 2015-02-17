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
    private PresetSetDummy _big;
    private ScreenControlls _screens;
    private PresetSetDummy _small;
    private Text _text;
    private PresetSetDummy _tv;
    private Toggle[] toggles;
    // Use this for initialization
    private void Start()
    {
        _screens = FindObjectOfType<ScreenControlls>();
        _big = new PresetSetDummy { _screenDistance = 20.0f, _screenSize = 20.0f, _backgroundColorIndex = Config.Colors.Count - 1 };
        _small = new PresetSetDummy { _screenDistance = 15.0f, _screenSize = 5.0f, _backgroundColorIndex = Config.Colors.Count - 1 };
        _tv = new PresetSetDummy { _screenDistance = 2.5f, _screenSize = 0.7f, _backgroundColorIndex = Config.Colors.Count - 2 };

        _text = gameObject.GetComponentInChildren<Text>();
        _text.text = "\nStereoFormat: " + ScreenInfo.Format +
                     "\nAspect Ratio: " + (int) ScreenInfo.AspectRatio.x + " : " + (int) ScreenInfo.AspectRatio.y +
                     "\nScreen distance: " + ScreenInfo.ScreenDistance +
                     "\nScreen size: " + ScreenInfo.ScreenSize + " x " +
                     (ScreenInfo.AspectRatio.y*(ScreenInfo.ScreenSize/ScreenInfo.AspectRatio.x)) +
                     "\nHIT: " + ScreenInfo.HIT;

        toggles = gameObject.GetComponentsInChildren<Toggle>();
    }

    private void Update()
    {
        _text.text = "\nStereoFormat: " + ScreenInfo.Format +
                     "\nAspect Ratio: " + (int) ScreenInfo.AspectRatio.x + " : " + (int) ScreenInfo.AspectRatio.y +
                     "\nScreen distance: " + ScreenInfo.ScreenDistance +
                     "\nScreen size: " + ScreenInfo.ScreenSize + " x " +
                     (ScreenInfo.AspectRatio.y*(ScreenInfo.ScreenSize/ScreenInfo.AspectRatio.x)) +
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
            SetPreset(_big._screenDistance, _big._screenSize, _big._backgroundColorIndex);
        }
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            toggles[0].isOn = false;
            toggles[1].isOn = true;
            toggles[2].isOn = false;
            SetPreset(_small._screenDistance, _small._screenSize, _small._backgroundColorIndex);
        }
        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            toggles[0].isOn = false;
            toggles[1].isOn = false;
            toggles[2].isOn = true;
            SetPreset(_tv._screenDistance, _tv._screenSize, _tv._backgroundColorIndex);
        }
        if (Input.GetAxis("Screen Distance") != 0.0f || Input.GetAxis("Screen Size") != 0.0f)
        {
            foreach (Toggle toggle in toggles)
            {
                toggle.isOn = false;
            }
        }
    }

    private void SetPreset(float distance, float size, int colorIndex)
    {
        _screens.ScreenDistance = distance;
        _screens.ScreenSize = size;
        Config.CurrentColorIndex = colorIndex;
    }

    public struct PresetSetDummy
    {
        //just some temp dummy presets
        public float _screenDistance;
        //using width as reference, hight ist calculated relative to the aspect ratio
        public float _screenSize;
        public int _backgroundColorIndex;
    }

}