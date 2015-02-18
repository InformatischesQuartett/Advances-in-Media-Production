using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


public static class Config {
    private static readonly string _configPath = Application.streamingAssetsPath;
    private static readonly string _presetPath = Application.streamingAssetsPath + @"/Presets";

	public static AVProLiveCameraDevice AVDevice1 { get; set; }
	public static AVProLiveCameraDevice AVDevice2 { get; set; }
	public static StereoFormat CurrentFormat { get; set; }

    public static float ScreenDistanceDefault { get; private set; }
    public static float ScreenDistanceSensitivity { get; private set; }
    public static float ScreenSizeDefault { get; private set; }
    public static float ScreenSizeSensitivity { get; private set; }
    public static float HitDefault { get; private set; }
    public static float HitSensitivity { get; private set; }

    public static bool CleanPreset { get; set; }

    public static Vector2 AspectRatio { get; private set; }
    public static Vector3 AspectRatioNorm { get; private set; }
    public static string AspectRatioNormString { get; private set; }

    public static bool Monoscopic { get; private set; }

    public static List<Color> Colors
    {
        get { return _colors; }
    }
    public static PresetSet CurrentColor
    {
        get { return _presets[_currentColorIndex]; }
    }

    public static List<PresetSet> Presets
    {
        get { return _presets; }
    }

    public static PresetSet CurrentPreset
    {
        get { return _presets[_currentPresetIndex]; }
    }

    private static List<Color> _colors = new List<Color>(); 
    private static List<PresetSet> _presets = new List<PresetSet>();

    private static int _currentPresetIndex; 
    private static int _currentColorIndex;

    public static int CurrentPresetIndex
    {
        get { return _currentPresetIndex; }
        set
        {
            _currentPresetIndex = Mathf.Clamp(value, 0, Presets.Count - 1);
            SetAspectRatioNorm(Presets[_currentPresetIndex].AspectRatioNorm);
        }
    }

    public static int CurrentColorIndex
    {
        get { return _currentColorIndex; }
        set
        {
            _currentColorIndex = Mathf.Clamp(value, 0, Colors.Count - 1);
        }
    }




    static Config()
    {
        string configContent = File.ReadAllText(_configPath + @"/config.json");
        var conf = JsonConvert.DeserializeObject<ConfigSet>(configContent);

        ScreenDistanceDefault = conf.ScreenDistanceDefault;
        ScreenDistanceSensitivity = conf.ScreenDistanceSensitivity;
        ScreenSizeDefault = conf.ScreenSizeDefault * 0.1f;
        ScreenSizeSensitivity = conf.ScreenSizeSensitivity;
        HitDefault = conf.HitDefault;
        HitSensitivity = conf.HitSensitivity;

        AspectRatio = new Vector2(conf.DefaultAspectRatioX, conf.DefaultAspectRatioY);
        AspectRatioNorm = new Vector3();

        SetAspectRatioNorm(conf.AspectRatioNorm);

        Monoscopic = conf.Monoscopic;
        OVRManager.instance.monoscopic = Monoscopic;

        for (int i = 0; i < conf.Colors.Length; i++)
        {
            _colors.Add(new Color(conf.Colors[i][0],conf.Colors[i][1],conf.Colors[i][2],1));
        }

        var DefaultPreset = new PresetSet();
        DefaultPreset.Name = "Default";
        DefaultPreset.ScreenSize = ScreenSizeDefault;
        DefaultPreset.ScreenDistance = ScreenDistanceDefault;
        DefaultPreset.AspectRatioNorm = conf.AspectRatioNorm;
        DefaultPreset.BackgroundColor = new float[3];
        DefaultPreset.BackgroundColor[0] = Colors[0].r;
        DefaultPreset.BackgroundColor[1] = Colors[0].g;
        DefaultPreset.BackgroundColor[2] = Colors[0].b;
        Presets.Add(DefaultPreset);

        foreach (string file in Directory.GetFiles(_presetPath))
        {
            if (file.EndsWith(".json"))
            {
                string filecontent = File.ReadAllText(file);
                var preset = JsonConvert.DeserializeObject<PresetSet>(filecontent);
                if (string.IsNullOrEmpty(preset.Name))
                    preset.Name = Path.GetFileNameWithoutExtension(file);

                preset.ScreenSize *= 0.1f;
                Presets.Add(preset);
            }
        }

        _currentColorIndex = 0;
        _currentPresetIndex = 0;
    }

    private static void SetAspectRatioNorm(string arn)
    {
        AspectRatioNormString = arn;

        if (arn == "horizontal")
        {
            AspectRatioNorm = new Vector3(1, 0, AspectRatio.y / AspectRatio.x);
        }
        else if (arn == "vertical")
        {
            AspectRatioNorm = new Vector3(AspectRatio.x / AspectRatio.y, 0, 1);
        }
        else
        {
            Debug.LogError("Config for AspectRatioNorm not valid! Valid options are 'horizontal' or 'vertical'.");
            Application.Quit();
        }
    }

    public static void SavePreset(float dist, float size, Color color, string ar)
    {
        var preset = new PresetSet();
        preset.ScreenDistance = dist;
        preset.ScreenSize = size;
        preset.BackgroundColor = new float[3];
        preset.BackgroundColor[0] = color.r;
        preset.BackgroundColor[1] = color.g;
        preset.BackgroundColor[2] = color.b;
        preset.AspectRatioNorm = ar;

        DateTime dt = DateTime.Now;

        preset.Name = "CustomPreset-" + String.Format("{0:yyMMdd-HHmmss}", dt);
        
        string json = JsonConvert.SerializeObject(preset, Formatting.Indented);
        File.WriteAllText(_presetPath + "/" + preset.Name + ".json", json);

        Presets.Add(preset);
    }

    private struct ConfigSet
    {
        public float[][] Colors;
        public float ScreenDistanceDefault;
        public float ScreenDistanceSensitivity;
        public float ScreenSizeDefault;
        public float ScreenSizeSensitivity;
        public float HitDefault;
        public float HitSensitivity;

        public float DefaultAspectRatioX;
        public float DefaultAspectRatioY;

        public string AspectRatioNorm;

        public bool Monoscopic;
    }

    public struct PresetSet
    {
        public string Name;
        //just some temp dummy presets
        public float ScreenDistance;
        //using width as reference, hight ist calculated relative to the aspect ratio
        public float ScreenSize;
        public float[] BackgroundColor;

        public string AspectRatioNorm;
    }
}

public enum StereoFormat
{
    FramePacking,
    SideBySide,
    TwoCameras,
    DemoMode,
    VideoSample
}