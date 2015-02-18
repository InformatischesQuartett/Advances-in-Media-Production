using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class InputSelector : MonoBehaviour {

	private Texture2D _backgroundTex;
	private GUIStyle _fontStyle;

	private float _waitingTime;

	private bool _enableGUI;

	/*Camera Modes for Toggle*/

	/**
	 * 0: Sony Side by Side
	 * 1: Sony Framepacking
	 * 2: 2x C300
	 * 3: Demo1: Trailer
	 * 4: Demo2: freeze image
	 **/ 
	private bool[] _selectedMode = new bool[5];
	private int _currSelection = -1;

	/*positions for the GUI scroll elements*/
	private List<Vector2> _scrollPos = new List<Vector2>();
	private List<Vector2> _scrollVideoInputPos = new List<Vector2>();
	private Vector2 _horizScrollPos = Vector2.zero;
	private Vector2 _horizScrollPos2 = Vector2.zero;

	private List<AVProLiveCameraDevice> chosenDevices = new List<AVProLiveCameraDevice>();
	private AVProLiveCameraDevice _chosenDevice1;
	private AVProLiveCameraDevice _chosenDevice2;

	
	void Start () {
		/* Makes the application run even when in background*/
		Application.runInBackground = true;

		EnumerateDevices ();

		/*Boolean values for the gui*/
		_enableGUI = true;

		/*Design*/
		_backgroundTex = (Texture2D) Resources.Load ("Textures/background", typeof(Texture2D));
		_fontStyle = new GUIStyle();
		_fontStyle.font = (Font) Resources.Load("Fonts/BNKGOTHM");
		_fontStyle.normal.textColor = Color.white;
		_fontStyle.fontSize = 30;

		/*Waiting time till the application is automatically started*/
		_waitingTime = Time.time + 15;

		/*Gets number of all camera devices found*/
		int numDevices = AVProLiveCameraManager.Instance.NumDevices;

		/*Creates new virtual AVProLiveCamera devices for each camera device found*/
		for (int i = 0; i < numDevices; i++) {
			AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice (i);
			Debug.Log(device);
		}

	}
	
	// Update is called once per frame
	void Update () {

		/*Change to main application after certain amount of time without any selection*/
		if (_currSelection == -1) {

			float timeLeft = _waitingTime - Time.time; 
			if (timeLeft < 0) {
				timeLeft = 0;
			}
		
			/*After a certain amount of time change to Main Application with a video sample*/
			if (timeLeft <= 0) {
				Config.CurrentFormat = StereoFormat.VideoSample;
				Application.LoadLevel ("default");
			}
		}
	}

	private void EnumerateDevices()
	{
		// Enumerate all cameras
		int numDevices = AVProLiveCameraManager.Instance.NumDevices;

		for (int i = 0; i < numDevices; i++)
		{
			AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice(i);
			_scrollPos.Add(new Vector2(0, 0));
			_scrollVideoInputPos.Add(new Vector2(0, 0));
		}		
	}

	private void UpdateCameras()
	{
		// Update all cameras
		int numDevices = AVProLiveCameraManager.Instance.NumDevices;
		for (int i = 0; i < numDevices; i++)
		{
			AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice(i);
			
			// Update the actual image
			device.Update(false);
		}
	}

	private int _lastFrameCount;
	void OnRenderObject()
	{
		if (_lastFrameCount != Time.frameCount)
		{
			_lastFrameCount = Time.frameCount;
			
			UpdateCameras();
		}
	}

	public void NewDeviceAdded()
	{
		EnumerateDevices();
	}

	void NewSelection(int mode)
	{
		_currSelection = mode;

		/*Set all other values but the one currently selected to false*/
		for (int i = 0; i < _selectedMode.Length; i++) {
			_selectedMode[i] = (_currSelection == i);	
		}
	}

	void OnGUI () {
		/*Draw Background tex*/
		GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), _backgroundTex);
		int lastSelection = _currSelection;

		if (_enableGUI) {
			/*----> Start horizontal scrollview area <----*/
			_horizScrollPos = GUILayout.BeginScrollView(_horizScrollPos, false, false);
			GUILayout.Label("Please select a mode from the list below:");

			/*Sony Side by Side*/

			if (GUILayout.Toggle(_selectedMode[0], "Sony, Side by Side")) {
				NewSelection(0);
				/*Mode 5 is side by side mode*/
				if (_currSelection != lastSelection) {
					AVProLiveCameraManager.Instance.GetDevice(0).Close();
					if (AVProLiveCameraManager.Instance.NumDevices > 1) {
						AVProLiveCameraManager.Instance.GetDevice(1).Close();
					}
					AVProLiveCameraManager.Instance.GetDevice(0).Start(5,-1);
				}
			}
			/*Sony Framepacking*/
			if (GUILayout.Toggle(_selectedMode[1], "Sony, Framepacking")) {
				NewSelection(1);
				if (_currSelection != lastSelection) {
					if (AVProLiveCameraManager.Instance.NumDevices > 0) {
						/*Mode 19 is framepacking mode*/
						AVProLiveCameraManager.Instance.GetDevice(0).Close();
						if (AVProLiveCameraManager.Instance.NumDevices > 1) {
							AVProLiveCameraManager.Instance.GetDevice(1).Close();
						}
						AVProLiveCameraManager.Instance.GetDevice(0).Start (19, -1);
					}
				}
			}
			/*2x C300 Framepacking*/
			if (GUILayout.Toggle(_selectedMode[2], "2x Canon EOS C300")) {
				NewSelection(2);
				if (_currSelection != lastSelection) {
					if (AVProLiveCameraManager.Instance.NumDevices > 1) {
						AVProLiveCameraManager.Instance.GetDevice(0).Close();
						AVProLiveCameraManager.Instance.GetDevice(1).Close();
						AVProLiveCameraManager.Instance.GetDevice(0).Start (5,-1);
						AVProLiveCameraManager.Instance.GetDevice(1).Start (5,-1);
					}
				}
			}

			/*Demo mode Video S3D*/
			if (GUILayout.Toggle(_selectedMode[3], "Demo: Video S3D")) {
				NewSelection(3);
			}

			/*Demo mode Freeze Image*/
			if (GUILayout.Toggle(_selectedMode[4], "Demo: Freeze Image")) {
				NewSelection(4);
			}

			if (_currSelection == 0 || _currSelection == 1) {
				if (AVProLiveCameraManager.Instance.NumDevices > 0) {
					/*----> Start vertical control group. <----*/
					GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
					AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice(0);
					/*Create a camera rectangle*/
					Rect cameraRect = GUILayoutUtility.GetRect(300, 168);
					GUI.Button(cameraRect, device.OutputTexture);
					GUILayout.Box("Camera 1: "  + device.Name);
					GUILayout.EndVertical();
				} else {
					GUILayout.Label("Caution: There is no camera connected! The Sony camera needs to be in the first HDMI-In slot.");
				}
			}

			if (_currSelection == 2) {
				/*----> Start vertical control group. <----*/
				if (AVProLiveCameraManager.Instance.NumDevices > 1) {
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
					AVProLiveCameraDevice device1 = AVProLiveCameraManager.Instance.GetDevice(0);
					/*Create a camera rectangle*/
					Rect cameraRect1 = GUILayoutUtility.GetRect(300, 168);
					GUI.Button(cameraRect1, device1.OutputTexture);
					GUILayout.Box("Camera 1: "  + device1.Name);
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					AVProLiveCameraDevice device2 = AVProLiveCameraManager.Instance.GetDevice(1);
					/*Create a camera rectangle*/
					Rect cameraRect2 = GUILayoutUtility.GetRect(300, 168);
					GUI.Button(cameraRect2, device2.OutputTexture);
					GUILayout.Box("Camera 2: "  + device2.Name);
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				} else {
					GUILayout.Label("Caution: There are no two cameras connected! The Input has to be SDI or maybe you have chosen the wrong mode.");
				}
			}

			if (GUILayout.Button ("Done")) {
				loadApplication ();
			}

			GUILayout.EndScrollView();
			/*----> End horizontal scrollview area <----*/

			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input Selector", _fontStyle);


		} else {
			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input selected. Please put on your OVR-Device now.", _fontStyle);
		}
	}//end OnGUI
	

	private void loadApplication () {
		/*Check for selections*/
		modeSelection();

		for (int i=0; i < _selectedMode.Length; i++) {
			if (_selectedMode[i] == true) {
				_enableGUI = false;
				Application.LoadLevel ("default");
				return;
			}
		}

		Debug.Log ("Nothing selected");
	}

	/**
	 * Change the mode selection according to the toggles
	 **/ 
	private void modeSelection() {
	
		if (_selectedMode[0]) {
			_chosenDevice1 = AVProLiveCameraManager.Instance.GetDevice(0);
			Config.CurrentFormat = StereoFormat.SideBySide;
			Config.AVDevice1 = _chosenDevice1;
			Config.AVDevice2 = null;
		} 
		if (_selectedMode[1]) {
			_chosenDevice1 = AVProLiveCameraManager.Instance.GetDevice(0);
			Config.CurrentFormat = StereoFormat.FramePacking;
			Config.AVDevice1 = _chosenDevice1;
			Config.AVDevice2 = null;
		}
		if (_selectedMode[2]) {
			_chosenDevice1 = AVProLiveCameraManager.Instance.GetDevice(0);
			_chosenDevice2 = AVProLiveCameraManager.Instance.GetDevice(1);
			Config.CurrentFormat = StereoFormat.TwoCameras;
			Config.AVDevice1 = _chosenDevice1;
			Config.AVDevice2 = _chosenDevice2;
		}
		if (_selectedMode[3]) {
			Config.CurrentFormat = StereoFormat.VideoSample;
			Config.AVDevice1 = null;
			Config.AVDevice2 = null;
		}
		if (_selectedMode[4]) {
			Config.CurrentFormat = StereoFormat.DemoMode;
			Config.AVDevice1 = null;
			Config.AVDevice2 = null;
		}

	}
} //end class

