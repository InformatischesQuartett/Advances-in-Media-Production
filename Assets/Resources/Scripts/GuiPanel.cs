using UnityEngine;
using UnityEngine.UI;

public static class ScreenInfo
{
    public static float ScreenDistance { get; private set; }
    public static Vector2 ScreenSize { get; private set; }
    public static StereoFormat Format { get; private set; }
    public static float HIT { get; private set; }

    public static void UpdateScreenVaues(float distance, Vector2 size, float hit)
    {
        ScreenDistance = distance;
        ScreenSize = size;
        HIT = hit;
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
    // Use this for initialization
    private void Start()
    {
        _screens = FindObjectOfType<ScreenControlls>();
        _text = gameObject.GetComponentInChildren<Text>();
        _text.text = "StereoFormat: " + ScreenInfo.Format + "\nScreen distance: " +
                     ScreenInfo.ScreenDistance + "\nScreen size (width x height): " + ScreenInfo.ScreenSize.x +
                     " x " + ScreenInfo.ScreenSize.y + 
                     "\nHIT: " + ScreenInfo.HIT;
    }

    private void Update()
    {
        _text.text = "StereoFormat: " + ScreenInfo.Format + "\nScreen distance: " +
                     ScreenInfo.ScreenDistance + "\nScreen size (width x height): " + ScreenInfo.ScreenSize.x +
                     " x " + ScreenInfo.ScreenSize.y +
                     "\nHIT: " + ScreenInfo.HIT;
    }
}