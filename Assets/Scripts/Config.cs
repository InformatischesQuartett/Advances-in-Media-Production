using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


public static class Config {
    private static readonly string _configPath = Application.streamingAssetsPath;
    private static readonly string _presetPath = Application.streamingAssetsPath + @"/Presets";

    public static int Test { get; set; }

    public static List<Color> Colors
    {
        get { return _colors; }
    }

    private static List<Color> _colors = new List<Color>(); 
    private static List<PresetSet> Presets = new List<PresetSet>();


    static Config()
    {
        string configContent = File.ReadAllText(_configPath + @"/config.json");
        var conf = JsonConvert.DeserializeObject<ConfigSet>(configContent);

        Test = conf.Test;

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
        public int Test;
        public float[][] Colors;
    }

    private struct PresetSet
    {
        
    }


}
