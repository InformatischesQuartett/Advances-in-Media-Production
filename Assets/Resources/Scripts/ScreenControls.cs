using UnityEngine;
using System.Collections.Generic;

public class ScreenControls : MonoBehaviour
{
    private GameObject _screenL;
    private GameObject _screenR;

    public float Hit { get; private set; }
    public float ScreenDistance { get; set; }
    public float ScreenSize { get; set; }

    private Vector3 _positionVectorScreenL;
    private Vector3 _positionVectorScreenR;

    private Vector3 _positionVector;

    private List<GameObject> _cameras;

    private bool _colorAxisInUse = false;
    private bool _presetAxisInUse = false;
    private bool _saveAxisInUse = false;

    public List<Config.PresetSet> Presets { get; private set; }

    public delegate void UpdateGuiText();
    public UpdateGuiText GuiTextUpdate;

    // Use this for initialization
    private void Start()
    {
        Config.Init();

        //Initial Screen settings
        ScreenDistance = Config.ScreenDistanceDefault;
        ScreenSize = Config.ScreenSizeDefault;
        Hit = Config.HitDefault;

        _screenL = GameObject.Find("screenL");
        _screenR = GameObject.Find("screenR");

        SetupScreens();
        
        _positionVector = this.transform.position;

        _cameras = new List<GameObject>();

        //Add Cameras here!
        _cameras.Add(GameObject.Find("/Main Camera"));
        _cameras.Add(GameObject.Find("LeftEyeAnchor"));
        _cameras.Add(GameObject.Find("RightEyeAnchor"));

        SetCamerasBackground(Config.Colors[Config.CurrentColorIndex]);

        LoadPreset();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetAxisRaw("Screen Size") != 0 || Input.GetAxisRaw("Screen Distance") != 0 ||
            Input.GetAxisRaw("Color Select") != 0 || Input.GetAxis("HIT") != 0)
        {
            Config.CleanPreset = false;
            GuiTextUpdate();
        }

        //HIT
        Hit += Input.GetAxis("HIT")*Time.deltaTime*Config.HitSensitivity;
        Hit = Mathf.Clamp(Hit, -10, 10);

        //Distance
        ScreenDistance += Input.GetAxis("Screen Distance")*Time.deltaTime*Config.ScreenDistanceSensitivity;
        ScreenDistance = Mathf.Clamp(ScreenDistance, 0.2f, 100);

        //SIZE
        ScreenSize += Input.GetAxis("Screen Size")*Time.deltaTime*Config.ScreenSizeSensitivity;
        ScreenSize = Mathf.Clamp(ScreenSize, 0, 20);

        //COLOR
        if (Input.GetAxisRaw("Color Select") != 0)
        {
            if (_colorAxisInUse == false)
            {
                Config.CurrentColorIndex += (int)Input.GetAxisRaw("Color Select");
                SetCamerasBackground(Config.Colors[Config.CurrentColorIndex]);
            }
            _colorAxisInUse = true;
        }
        else
        {
            _colorAxisInUse = false;
        }

        //Presets
        if (Input.GetAxisRaw("Preset Select") != 0)
        {
            if (_presetAxisInUse == false)
            {
                Config.CurrentPresetIndex += (int)Input.GetAxisRaw("Preset Select");
                LoadPreset();
            }
            _presetAxisInUse = true;
        }
        else
        {
            _presetAxisInUse = false;
        }

        //Saving presets
        if (Input.GetAxisRaw("Preset Save") != 0)
        {
            if (_saveAxisInUse == false)
            {
                Config.SavePreset(ScreenDistance, ScreenSize, Config.Colors[Config.CurrentColorIndex], Config.AspectRatioNormString);
            }
            _saveAxisInUse = true;
        }
        else
        {
            _saveAxisInUse = false;
        }

        if (Input.GetAxis("Tracker Reset") > 0)
            OVRManager.display.RecenterPose();

        if (Input.GetAxis("Preset Reset") > 0)
            LoadPreset();

		if (Input.GetKeyDown (KeyCode.Escape))
			Application.Quit();

		if (Input.GetKeyDown(KeyCode.Backspace))
		    Application.LoadLevel(0);


        _positionVectorScreenL.x = Hit/2f;
        _positionVectorScreenL.y = 0;
        _positionVectorScreenL.z = 0;
        _screenL.transform.localPosition = _positionVectorScreenL;

        _positionVectorScreenR.x = -Hit/2f;
        _positionVectorScreenR.y = 0;
        _positionVectorScreenR.z = 0;
        _screenR.transform.localPosition = _positionVectorScreenR;

        _positionVector.x = 0;
        _positionVector.y = 0;
        _positionVector.z = ScreenDistance;
        this.transform.position = _positionVector;

        this.transform.localScale = ScreenSize * Vector3.one;

    }

    private void SetCamerasBackground(Color color)
    {
        foreach (GameObject cam in _cameras)
        {
            cam.camera.backgroundColor = color;
            Config.CurrentColor = color;
        }
    }

    private void SetupScreens()
    {
        _positionVectorScreenL = _screenL.transform.position;
        _positionVectorScreenR = _screenR.transform.position;

        _screenL.transform.localScale = Config.AspectRatioNorm;
        _screenR.transform.localScale = Config.AspectRatioNorm;
    }

    private void LoadPreset()
    {
        SetupScreens();

        var color = new Color(Config.Presets[Config.CurrentPresetIndex].BackgroundColor[0], Config.Presets[Config.CurrentPresetIndex].BackgroundColor[1], Config.Presets[Config.CurrentPresetIndex].BackgroundColor[2], 1);

        ScreenDistance = Config.CurrentPreset.ScreenDistance;
        ScreenSize = Config.CurrentPreset.ScreenSize;
        Hit = Config.HitDefault;

        SetCamerasBackground(color);

        Config.CleanPreset = true;
        
        GuiTextUpdate();
    }


}
