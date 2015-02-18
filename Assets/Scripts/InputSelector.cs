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
				_enableGUI = false;
				Config.CurrentFormat = StereoFormat.VideoSample;
				Application.LoadLevel ("default");
			}
		}
	}

	private void EnumerateDevices()
	{
		// Enumerate all cameras
		int numDevices = AVProLiveCameraManager.Instance.NumDevices;
		print("num devices: " + numDevices);
		for (int i = 0; i < numDevices; i++)
		{
			AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice(i);
			
			// Enumerate video inputs (only for devices with multiple analog input sources, eg TV cards)
			print("device " + i + ": " + device.Name + " has " + device.NumVideoInputs + " videoInputs");
			for (int j = 0; j < device.NumVideoInputs; j++)
			{
				print("  videoInput " + j + ": " + device.GetVideoInputName(j));
			}
			
			// Enumerate camera modes
			print("device " + i + ": " + device.Name + " has " + device.NumModes + " modes");
			for (int j = 0; j < device.NumModes; j++)
			{
				AVProLiveCameraDeviceMode mode = device.GetMode(j);
				print("  mode " + j + ": " + mode.Width + "x" + mode.Height + " @" + mode.FPS.ToString("F2") + "fps [" + mode.Format + "] idx:" + mode.Index);
			}
			
			// Enumerate camera settings
			print("device " + i + ": " + device.Name + " has " + device.NumSettings + " video settings");
			for (int j = 0; j < device.NumSettings; j++)
			{
				AVProLiveCameraSettingBase settingBase = device.GetVideoSettingByIndex(j);
				switch (settingBase.DataTypeValue)
				{
				case AVProLiveCameraSettingBase.DataType.Boolean:
				{
					AVProLiveCameraSettingBoolean settingBool = (AVProLiveCameraSettingBoolean)settingBase;
					print(string.Format("  setting {0}: {1}({2}) value:{3} default:{4} canAuto:{5} isAuto:{6}", j, settingBase.Name, settingBase.PropertyIndex, settingBool.CurrentValue, settingBool.DefaultValue, settingBase.CanAutomatic, settingBase.IsAutomatic));
				}
					break;
				case AVProLiveCameraSettingBase.DataType.Float:
				{
					AVProLiveCameraSettingFloat settingFloat = (AVProLiveCameraSettingFloat)settingBase;
					print(string.Format("  setting {0}: {1}({2}) value:{3} default:{4} range:{5}-{6} canAuto:{7} isAuto:{8}", j, settingBase.Name, settingBase.PropertyIndex, settingFloat.CurrentValue, settingFloat.DefaultValue, settingFloat.MinValue, settingFloat.MaxValue, settingBase.CanAutomatic, settingBase.IsAutomatic));
				}
					break;
				}
			}
			
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

		if (_enableGUI) {
			/*----> Start horizontal scrollview area <----*/
			_horizScrollPos = GUILayout.BeginScrollView(_horizScrollPos, false, false);
			GUILayout.Label("Please select a mode from the list below:");

			/*Sony Side by Side*/

			if (GUILayout.Toggle(_selectedMode[0], "Sony, Side by Side")) {
				NewSelection(0);
			}
			/*Sony Framepacking*/
			if (GUILayout.Toggle(_selectedMode[1], "Sony, Framepacking")) {
				NewSelection(1);
			}
			/*2x C300 Framepacking*/
			if (GUILayout.Toggle(_selectedMode[2], "2x Canon EOS C300")) {
				NewSelection(2);
			}



			if (_currSelection == 0 || _currSelection == 1) {
				/*----> Start vertical control group. <----*/
				GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
			}

			if (_currSelection == 2) {
			}


			for (int i = 0; i < AVProLiveCameraManager.Instance.NumDevices; i++){

				GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
				AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice(i);

				/*GUI is only enabled if a device is connected*/
				GUI.enabled = device.IsConnected;



				/*Create a camera rectangle*/
				Rect cameraRect = GUILayoutUtility.GetRect(300, 168);
				if (GUI.Button(cameraRect, device.OutputTexture)) {
					/*choose camera by clicking on this button*/
					chosenDevices.Add(AVProLiveCameraManager.Instance.GetDevice(i));
				}
				          
				GUILayout.Box("Camera " + i + ": " + device.Name);

				/*----> End vertical control group. <----*/
				GUILayout.EndVertical();
			}


			GUILayout.EndScrollView();
			/*----> End horizontal scrollview area <----*/

			/*Display selected Modes*/
			GUI.Label (new Rect (10, Screen.height / 1.1f, Screen.width, 50), "Your selected Video Device(s):");

			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input Selector", _fontStyle);

			if (GUI.Button (new Rect (80, Screen.height / 1.5f, 60, 60), "Done")) {
				loadApplication ();
			}

			/*Selection of the Video Input*/
			GUI.Label(new Rect (10, Screen.height / 1.9f, Screen.width, 50),"Select a Video Input:");

			/*Selection of the Framerate*/
			GUI.Label(new Rect (10, Screen.height / 1.8f, Screen.width, 50),"Select a Framerate:");

			/*Selection of the Mode*/
			GUI.Label(new Rect (10, Screen.height / 1.7f, Screen.width, 50),"Select a Camera Mode:");

			/*Selection of the Resolution*/
			//GUI.Label(new Rect (10, Screen.height / 1.6f, Screen.width, 50),"Select a Resolution:");


			/*Demo Modes*/
			GUI.Label(new Rect (10, Screen.height / 1.6f, Screen.width, 50),"Or choose one of the Demos:");

			if (GUI.Button (new Rect (10, Screen.height / 1.5f, 60, 60), "Demo1: S3D Trailer")) {
				//set demo mode
			}

			if (GUI.Button (new Rect (10, Screen.height / 1.5f, 60, 60), "Demo1: S3D Trailer")) {
				//set demo mode
			}



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

		Debug.Log ("Nothing sected");

	}

	/**
	 * Change the mode selection according to the toggles
	 **/ 
	private void modeSelection() {
	
		if (_selectedMode[0]) {
			_chosenDevice1 = AVProLiveCameraManager.Instance.GetDevice(0);
			/*Mode 5 is side by side*/
			_chosenDevice1.Start();
		} 
		if (_selectedMode[1]) {
			_chosenDevice1 = AVProLiveCameraManager.Instance.GetDevice(0);
			/*Mode 19 is framepacking mode*/
			_chosenDevice1.Start(19, -1);
		}
		if (_selectedMode[2]) {
			_chosenDevice1 = AVProLiveCameraManager.Instance.GetDevice(0);
			_chosenDevice2 = AVProLiveCameraManager.Instance.GetDevice(1);
			_chosenDevice1.Start(5, -1);
			_chosenDevice2.Start(5, -1);
		}
		if (_selectedMode[3]) {

		}
		if (_selectedMode[4]) {

		}

	}
} //end class

