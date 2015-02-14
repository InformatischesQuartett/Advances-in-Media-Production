using UnityEngine;
using System.Collections.Generic;
using UnityEngineInternal;

public class ScreenControlls : MonoBehaviour
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

    private List<Color> _availableColors;
    public int CurrentColorIndex { get; set; }

    private bool _colorAxisInUse = false;

    // Use this for initialization
    private void Start()
    {
        _screenL = GameObject.Find("screenL");
        _screenR = GameObject.Find("screenR");

        _positionVectorScreenL = _screenL.transform.position;
        _positionVectorScreenR = _screenR.transform.position;

        _screenL.transform.localScale = Config.AspectRatioNorm;
        _screenR.transform.localScale = Config.AspectRatioNorm;
        
        _positionVector = this.transform.position;

        _cameras = new List<GameObject>();
        _availableColors = Config.Colors;

        //Add Cameras here!
        _cameras.Add(GameObject.Find("/Main Camera"));
        _cameras.Add(GameObject.Find("LeftEyeAnchor"));
        _cameras.Add(GameObject.Find("RightEyeAnchor"));

        SetCamerasBackground(_cameras, _availableColors[CurrentColorIndex]);

        //Initial Screen settings
        ScreenDistance = Config.ScreenDistanceDefault;
        ScreenSize = Config.ScreenSizeDefault;
        Hit = Config.HitDefault;

    }

    // Update is called once per frame
    private void Update()
    {

        //HIT
        Hit += Input.GetAxis("HIT")*Time.deltaTime*Config.HitSensitivity;
        Hit = Mathf.Clamp(Hit, -10, 10);

        //Distance
        ScreenDistance += Input.GetAxis("Screen Distance")*Time.deltaTime*Config.ScreenDistanceSensitivity;
        ScreenDistance = Mathf.Clamp(ScreenDistance, 0, 1000);

        //SIZE
        ScreenSize += Input.GetAxis("Screen Size")*Time.deltaTime*Config.ScreenSizeSensitivity;
        ScreenSize = Mathf.Clamp(ScreenSize, 0, 20);

        //COLOR
        if (Input.GetAxisRaw("Color Select") != 0)
        {
            if (_colorAxisInUse == false)
            {
                CurrentColorIndex += (int) Input.GetAxisRaw("Color Select");
                CurrentColorIndex = Mathf.Clamp(CurrentColorIndex, 0, _availableColors.Count - 1);
            }
            _colorAxisInUse = true;
        }
        else
        {
            _colorAxisInUse = false;
        }



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

        SetCamerasBackground(_cameras, _availableColors[CurrentColorIndex]);

        ScreenInfo.UpdateScreenVaues(ScreenDistance, ScreenSize, Hit);

        if (Input.GetAxis("TrackerReset") > 0)
            OVRManager.display.RecenterPose();

    }

    private void SetCamerasBackground(List<GameObject> cameras, Color color)
    {
        foreach (GameObject cam in cameras)
        {
            cam.camera.backgroundColor = color;
        }
    }

    public int GetColorCount()
    {
        return _availableColors.Count;
    }


}
