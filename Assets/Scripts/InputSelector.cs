using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class InputSelector : MonoBehaviour {

	private Texture2D _backgroundTex;
	private GUIStyle _fontStyle;
	private float _counter;
	private float _waitingTime;

	private bool _enableGUI;
	private bool _done;
	private bool _inputSelected;

	private List<Vector2> _scrollVideoInputPos = new List<Vector2>();
	private Vector2 _horizScrollPos = Vector2.zero;


	// Use this for initialization
	void Start () {
		_enableGUI = true;
		_done = false;
		_inputSelected = false;

		_backgroundTex = (Texture2D) Resources.Load ("Textures/background", typeof(Texture2D));
		_fontStyle = new GUIStyle();
		_fontStyle.font = (Font) Resources.Load("Fonts/BNKGOTHM");
		_fontStyle.normal.textColor = Color.white;
		_fontStyle.fontSize = 30;

		_waitingTime = Time.time + 15;


		int numDevices = AVProLiveCameraManager.Instance.NumDevices;

		for (int i = 0; i < numDevices; i++) {
			AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice (i);
			Debug.Log(device);
		}

	}
	
	// Update is called once per frame
	void Update () {
		if (_inputSelected) {
			if (_done) {
				_enableGUI = false;
				Application.LoadLevel("default");
			}
		}

		if (!_inputSelected) {

			float timeLeft = _waitingTime - Time.time; 
			if (timeLeft < 0) {
				timeLeft = 0;
			}
		
			/*After a certain amount of time change to Application with default settings*/
			if (timeLeft <= 0) {
				_enableGUI = false;
				loadDefaultSettings ();
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
			
			//_scrollPos.Add(new Vector2(0, 0));
			//_scrollVideoInputPos.Add(new Vector2(0, 0));
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

	void OnGUI () {
		/*Draw Background tex*/
		GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), _backgroundTex);

		//_horizScrollPos = GUILayout.BeginScrollView(_horizScrollPos, false, false);

		if (_enableGUI) {
			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input Selector", _fontStyle);

			if (GUI.Button (new Rect (10, Screen.height / 1.5f, 60, 60), "Done")) {
				_done = true;
			}
			/*Selection of the Video Input*/
			GUI.Label(new Rect (10, Screen.height / 1.9f, Screen.width, 50),"Select a video input:");

			/*Selection of the Framerate*/

			/*Selection of the Mode*/

			/*Selection of the Resolution*/

			/*Demo Mode*/

			//GUILayout.EndScrollView();

		} else {
			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input selected. Please put on your OVR-Device now.", _fontStyle);
		}
	}//end OnGUI

	/*Loading Default Settings if nothing was selected*/
	void loadDefaultSettings () {
	}
} //end class

