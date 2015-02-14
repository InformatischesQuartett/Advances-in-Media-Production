using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


public static class Config {
    private static readonly string _configPath = Application.streamingAssetsPath;
    private static readonly string _presetPath = Application.streamingAssetsPath + @"/Presets";

    public static float ScreenDistanceDefault { get; private set; }
    public static float ScreenDistanceSensitivity { get; private set; }
    public static float ScreenSizeDefault { get; private set; }
    public static float ScreenSizeSensitivity { get; private set; }
    public static float HitDefault { get; private set; }
    public static float HitSensitivity { get; private set; }

    public static Vector2 AspectRatio { get; private set; }
    public static Vector3 AspectRatioNorm { get; private set; }

    public static bool Monoscopic { get; private set; }

    public static List<Color> Colors
    {
        get { return _colors; }
    }

    private static List<PresetSet> Presets
    {
        get { return _presets; }
    }

    private static List<Color> _colors = new List<Color>(); 
    private static List<PresetSet> _presets = new List<PresetSet>();


    static Config()
    {
        string configContent = File.ReadAllText(_configPath + @"/config.json");
        var conf = JsonConvert.DeserializeObject<ConfigSet>(configContent);

        ScreenDistanceDefault = conf.ScreenDistanceDefault;
        ScreenDistanceSensitivity = conf.ScreenDistanceSensitivity;
        ScreenSizeDefault = conf.ScreenSizeDefault;
        ScreenSizeSensitivity = conf.ScreenSizeSensitivity;
        HitDefault = conf.HitDefault;
        HitSensitivity = conf.HitSensitivity;

        AspectRatio = new Vector2(conf.AspectRatioX, conf.AspectRatioY);
        AspectRatioNorm = new Vector3();

        if (conf.AspectRatioNorm == "horizontal")
        {
            AspectRatioNorm = new Vector3(1, 0, conf.AspectRatioY/conf.AspectRatioX);
        }
        else if (conf.AspectRatioNorm == "vertical")
        {
            AspectRatioNorm = new Vector3(conf.AspectRatioX/conf.AspectRatioY, 0, 1);
        }
        else
        {
            Debug.LogError("Config for AspectRatioNorm not valid! Valid options are 'horizontal' or 'vertical'.");
            Application.Quit();
        }


        Monoscopic = conf.Monoscopic;
        OVRManager.instance.monoscopic = Monoscopic;

        for (int i = 0; i < conf.Colors.Length; i++)
        {
            _colors.Add(new Color(conf.Colors[i][0],conf.Colors[i][1],conf.Colors[i][2],1));
        }

        /*
        foreach (string file in Directory.GetFiles(_presetPath))
        {
            if (file.EndsWith(".json"))
            {
                string filecontent = File.ReadAllText(file);
                var preset = JsonConvert.DeserializeObject<PresetSet>(filecontent);
                Presets.Add(preset);
            }
        }
         * */
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

        public float AspectRatioX;
        public float AspectRatioY;

        public string AspectRatioNorm;

        public bool Monoscopic;
    }

    public struct PresetSet
    {
        //just some temp dummy presets
        public  float _screenDistance;
        public  float _screenSize;   
    }


}
