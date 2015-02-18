using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Converters;

public delegate void ConvDataCallback(byte[] imgLeft, byte[] imgRight);

public class CreateTwoTexture : MonoBehaviour
{
    private byte[] _lastImgLeft;
    private byte[] _lastImgRight;
    private bool _imgDataUpdate;

    private CVThread _workerObject;
    private Thread _workerThread;

    public Texture2D Left { get; private set; }
    public Texture2D Right { get; private set; }

	private AVProLiveCameraDevice _device1;
	private AVProLiveCameraDevice _device2;

	private int _lastFrameCount = -1;

    // fps calculations
    private const float FPSUpdateRate = 2.0f;

    private float _deltaTime;
    private float _threadFPS;
    private int _threadFrameCount;

    // Use this for initialization
    private IEnumerator Start()
    {
        Left = Right = null;
        yield return new WaitForSeconds(1);

		StartCameras ();
        CreateNewTexture();
	
        _imgDataUpdate = false;

        // fps caluclations
        _deltaTime = 0.0f;
        _threadFrameCount = 0;
        _threadFPS = 0.0f;  
    }

	void OnRenderObject()
	{
		if (_lastFrameCount != Time.frameCount)
		{
			_lastFrameCount = Time.frameCount;

			if (_device1 != null) _device1.Update(false);
			if (_device2 != null) _device2.Update(false);
		}
	}

    private void Update()
    {
        if (Left == null || Right == null) return;
		if (_workerObject == null) return;
		
		Convert();

        // fps calculations
        _deltaTime += Time.deltaTime;

        if (_deltaTime > 1.0f / FPSUpdateRate)
        {
            int runCount = _workerObject.GetRunCount();

            int threadDiff = runCount - _threadFrameCount;
            _threadFPS = threadDiff / _deltaTime;
            _threadFrameCount = runCount;

            _deltaTime -= 1.0f / FPSUpdateRate;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(5, 0, 250, 25), "Performance: " + _threadFPS.ToString("F1") + " fps");
    }

	private void StartCameras()
	{
		if (Config.AVDevice1 != -1) {
			_device1 = AVProLiveCameraManager.Instance.GetDevice (Config.AVDevice1);
			_device1.Start (Config.AVCamMode, -1);
		}

		if (Config.AVDevice2 != -1) {
			_device2 = AVProLiveCameraManager.Instance.GetDevice (Config.AVDevice2);
			_device2.Start (Config.AVCamMode, -1);
		}
	}

    private void CreateNewTexture()
    {	
        byte[] sampleData = null;

        if (Config.CurrentFormat == StereoFormat.SideBySide)
        {
            Left = new Texture2D(860, 1080, TextureFormat.RGB24, false);
            Right = new Texture2D(860, 1080, TextureFormat.RGB24, false);
        }
        else
        {
            Left = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            Right = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        }

        // format checking and material initialization
		switch (Config.CurrentFormat)
        {
            case StereoFormat.DemoMode:
                GetComponent<MaterialCreator>().Init(true, true);
                sampleData = ReadSampleFromFile("16520");

                break;

            case StereoFormat.VideoSample:
                GetComponent<MaterialCreator>().Init(true, false);
                break;

            case StereoFormat.TwoCameras:
                GetComponent<MaterialCreator>().Init(true, false);
                break;

			case StereoFormat.SideBySide:
            case StereoFormat.FramePacking:
                GetComponent<MaterialCreator>().Init(false, false);
                break;
        }

		_workerObject = new CVThread(Config.CurrentFormat, UpdateImgData, Config.AVDevice1, Config.AVDevice2, sampleData);
        _workerThread = new Thread(_workerObject.ProcessImage);
        _workerThread.Start();
    }

    private void UpdateImgData(byte[] imgLeft, byte[] imgRight)
    {
        if (_imgDataUpdate)
            return;

        lock (imgLeft)
            _lastImgLeft = imgLeft;

        lock (imgRight)
            _lastImgRight = imgRight;

        _imgDataUpdate = true;
    }

    private void Convert()
    {
        if (_imgDataUpdate)
        {
			Left.LoadRawTextureData(_lastImgLeft);
            Left.Apply();

            Right.LoadRawTextureData(_lastImgRight);
            Right.Apply();

            _imgDataUpdate = false;
        }
    }

    public void SaveSampleToFile(byte[] data)
    {
        if (data == null) return;

        var randObj = new System.Random();
        int sampleName = randObj.Next(10000, 99999);
        string path = Application.streamingAssetsPath + "/Samples/Sample" + sampleName;
        FileStream file = File.Open(path, FileMode.Create);

        using (var bw = new BinaryWriter(file))
            foreach (byte value in data)
                bw.Write(value);

        Debug.Log("Image sample saved to: " + path);
    }

    private byte[] ReadSampleFromFile(string id)
    {
        string path = Application.streamingAssetsPath + "/Samples/Sample" + id;
        FileStream file = File.Open(path, FileMode.Open);

        using (var br = new BinaryReader(file))
        {
            long valueCt = br.BaseStream.Length / sizeof(byte);
            var readArr = new byte[valueCt];

            for (int x = 0; x < valueCt; x++)
                readArr[x] = br.ReadByte();

            return readArr;
        }
    }

    private void OnApplicationQuit()
    {
        if (_workerObject != null)
        {
            _workerObject.RequestStop();
            _workerThread.Join();
        }
    }
}
