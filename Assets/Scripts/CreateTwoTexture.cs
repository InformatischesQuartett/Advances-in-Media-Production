using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;

public delegate void ConvDataCallback(byte[] imgLeft, byte[] imgRight);

public enum StereoFormat
{
    FramePacking,
    SideBySide,
    DemoMode,
    VideoSample
}

public class CreateTwoTexture : MonoBehaviour
{
    private AVProLiveCamera _liveCamera;

    public StereoFormat Format;

    private byte[] _lastImgLeft;
    private byte[] _lastImgRight;
    private bool _imgDataUpdate;
    private byte[] _sampleData;

    private CVThread _workerObject;
    private Thread _workerThread;

    public Texture2D Left { get; private set; }
    public Texture2D Right { get; private set; }

    // fps calculations
    private const float FPSUpdateRate = 2.0f;

    private float _deltaTime;
    private float _threadFPS;
    private int _threadFrameCount;

    // Use this for initialization
    private IEnumerator Start()
    {
        Left = Right = null;
        _liveCamera = GetComponent<AVProLiveCamera>();

        yield return new WaitForSeconds(1);

        CreateNewTexture(_liveCamera.OutputTexture, Format);

        _imgDataUpdate = false;

        // fps caluclations
        _deltaTime = 0.0f;
        _threadFrameCount = 0;
        _threadFPS = 0.0f;

        ScreenInfo.SetFormatInfo(Format);
    }

    private void Update()
    {
        if (Left == null || Right == null) return;
		
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
		/*
        if (_liveCamera.OutputTexture != null && Left != null && Right != null)
        {
            //GUI.DrawTexture(new Rect(250, 0, 200, 200), _liveCamera.OutputTexture, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(150, 100, 400, 224), Left, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(150, 350, 400, 224), Right, ScaleMode.ScaleToFit, false);
        }*/

        GUI.Label(new Rect(5, 0, 250, 25), "Performance: " + _threadFPS.ToString("F1") + " fps");
		
    }

    private void CreateNewTexture(Texture liveCamTexture, StereoFormat format)
    {
        GetComponent<MaterialCreator>().Init(format == StereoFormat.VideoSample);
		
		var imgWidth = liveCamTexture.width;
        var imgHeight = liveCamTexture.height;

        switch (format)
        {
            case StereoFormat.SideBySide:
                Left = new Texture2D(imgWidth/2, imgHeight, TextureFormat.RGB24, false);
                Right = new Texture2D(imgWidth/2, imgHeight, TextureFormat.RGB24, false);
                break;

            case StereoFormat.FramePacking:
            case StereoFormat.VideoSample:
            case StereoFormat.DemoMode:
                imgWidth = 1920;
                imgHeight = 1080;

                Left = new Texture2D(imgWidth, imgHeight, TextureFormat.RGB24, false);
                Right = new Texture2D(imgWidth, imgHeight, TextureFormat.RGB24, false);
                break;
        }

        if (format == StereoFormat.DemoMode)
            _sampleData = ReadSampleFromFile("16520");

        _workerObject = new CVThread(imgWidth, imgHeight, Format, UpdateImgData);
        _workerThread = new Thread(_workerObject.ProcessImage);
        _workerThread.Start();
    }

    private void UpdateImgData(byte[] imgLeft, byte[] imgRight)
    {
        lock (imgLeft)
            _lastImgLeft = imgLeft;

        lock (imgRight)
            _lastImgRight = imgRight;

        _imgDataUpdate = true;
    }

    private unsafe void Convert()
    {
        if (_imgDataUpdate)
        {
			Left.LoadRawTextureData(_lastImgLeft);
            Left.Apply();

            Right.LoadRawTextureData(_lastImgRight);
            Right.Apply();

            _imgDataUpdate = false;
        }

        if (_workerObject.GetUpdatedData())
            return;

        switch (Format)
        {
            case StereoFormat.DemoMode:
                fixed (byte* bytePtr = _sampleData)
                    _workerObject.SetUpdatedData(bytePtr);
                break;

            case StereoFormat.VideoSample:
                _workerObject.SetUpdatedData(null);
                break;

            default:
                var dvcIndex = _liveCamera.Device.DeviceIndex;
                var byteLivePtr = (byte*) AVProLiveCameraPlugin.GetLastFrameBuffered(dvcIndex).ToPointer();
                _workerObject.SetUpdatedData(byteLivePtr);
                break;
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
}
