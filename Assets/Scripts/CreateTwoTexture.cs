using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Converters;

public delegate void ConvDataCallback(byte[] imgLeft, byte[] imgRight);

public class CreateTwoTexture : MonoBehaviour
{
    private AVProLiveCamera _liveCamera;

    public StereoFormat Format;

    private byte[] _lastImgLeft;
    private byte[] _lastImgRight;
    private bool _imgDataUpdate;

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

        CreateNewTexture();

        _imgDataUpdate = false;

        // fps caluclations
        _deltaTime = 0.0f;
        _threadFrameCount = 0;
        _threadFPS = 0.0f;  
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

    private void CreateNewTexture()
    {	
		var imgWidth = 1920;//liveCamTexture.width;
		var imgHeight = 1080;//liveCamTexture.height;

        byte[] sampleData = null;

        if (Format == StereoFormat.SideBySide)
        {
            Left = new Texture2D(imgWidth/2, imgHeight, TextureFormat.RGB24, false);
            Right = new Texture2D(imgWidth/2, imgHeight, TextureFormat.RGB24, false);
        }
        else
        {
            Left = new Texture2D(imgWidth, imgHeight, TextureFormat.RGB24, false);
            Right = new Texture2D(imgWidth, imgHeight, TextureFormat.RGB24, false);
        }

        ScreenInfo.SetFormatInfo(Format);

        // format checking and material initialization
        switch (Format)
        {
            case StereoFormat.DemoMode:
                GetComponent<MaterialCreator>().Init(true, true);
                sampleData = ReadSampleFromFile("16520");

                break;

            case StereoFormat.VideoSample:
                GetComponent<MaterialCreator>().Init(true, false);
                break;

            case StereoFormat.TwoCameras:
                GetComponent<MaterialCreator>().Init(false, false);

                // add snd device index

                if (imgWidth != 1920 && imgHeight != 1080)
                {
                    Debug.Log("No valid camera attached or wrong mode selected. Image 1 has to be of size 1920x1080.");
                    return;
                }

                break;

            case StereoFormat.FramePacking:
                GetComponent<MaterialCreator>().Init(false, false);

                if (imgWidth != 1920 && imgHeight != 2160)
                {
                    Debug.Log("No valid camera attached or wrong mode selected. Image has to be of size 1920x2160.");
                    return;
                }

                break;

            case StereoFormat.SideBySide:
                GetComponent<MaterialCreator>().Init(false, false);

                if (imgWidth < 1920)
                {
                    Debug.Log("No valid camera attached or wrong mode selected. Image has to be of size 1920x1080.");
                    return;
                }

                break;
        }

        _workerObject = new CVThread(1920, 1080, Format, UpdateImgData, sampleData);
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
