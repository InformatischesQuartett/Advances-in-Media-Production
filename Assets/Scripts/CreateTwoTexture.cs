using System;
using System.CodeDom;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

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
    public Texture2D Complete { get; private set; }

    private Rect _rectL, _rectR;

    private bool _first = true;

    public const bool ForceFullHd = true;
    public const bool DemoMode = true;
    private byte[] _sampleData;

    // fps calculations
    private const float FPSUpdateRate = 2.0f;

    private float _deltaTime;
    private float _threadFPS;
    private int _threadFrameCount;

    public enum StereoFormat
    {
        FramePacking,
        SideBySide
    }

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
    }

    private void OnGUI()
    {
        if (_liveCamera.OutputTexture != null && Left != null && Right != null)
        {
            //GUI.DrawTexture(new Rect(250, 0, 200, 200), _liveCamera.OutputTexture, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(150, 100, 400, 224), Left, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(150, 350, 400, 224), Right, ScaleMode.ScaleToFit, false);
        }

        GUI.Label(new Rect(5, 0, 250, 25), "Performance: " + _threadFPS.ToString("F1") + " fps");
    }

    private void Update()
    {
        if (_liveCamera.OutputTexture == null)
            return;

        if (Complete == null)
            return;

        if (_first)
        {
            GetComponent<MaterialCreator>().Init();
            _first = false;
        }

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

    private void Convert()
    {
        if (Format == StereoFormat.SideBySide)
            ConvertSBS(_liveCamera.OutputTexture);

        if (Format == StereoFormat.FramePacking)
            ConvertFP(_liveCamera.OutputTexture); //rien ne vas plus
    }

    private void ConvertSBS(Texture liveCamTexture)
    {
        if (Left != null && Right != null)
        {
            RenderTexture.active = (RenderTexture) liveCamTexture;

            Left.ReadPixels(_rectL, 0, 0, false);
            Left.Apply();

            Right.ReadPixels(_rectR, 0, 0, false);
            Right.Apply();

            RenderTexture.active = null;
        }
        else
        {
            CreateNewTexture(liveCamTexture, Format);
        }
    }



    public void SaveSampleToFile(byte[] data)
    {
        if (data == null)
            return;

        var randObj = new System.Random();
        int name = randObj.Next(10000, 99999);
        string path = Application.streamingAssetsPath + "/Samples/Sample" + name;
        FileStream file = File.Open(path, FileMode.Create);

        using (var bw = new BinaryWriter(file))
            foreach (byte value in data)
                bw.Write(value);

        UnityEngine.Debug.Log("Image sample saved to: " + path);
    }

    private byte[] ReadSampleFromFile(string id)
    {
        string path = Application.streamingAssetsPath + "/Samples/Sample" + id;
        FileStream file = File.Open(path, FileMode.Open);

        using (var br = new BinaryReader(file))
        {
            long valueCt = br.BaseStream.Length/sizeof (byte);
            var readArr = new byte[valueCt];

            for (int x = 0; x < valueCt; x++)
                readArr[x] = br.ReadByte();

            return readArr;
        }
    }

    private void UpdateImgData(byte[] imgLeft, byte[] imgRight)
    {
        lock (imgLeft)
            _lastImgLeft = imgLeft;

        lock (imgRight)
            _lastImgRight = imgRight;

        _imgDataUpdate = true;
    }

    private unsafe void ConvertFP(Texture liveCamTexture)
    {
        if (Complete != null)
        {
            if (_imgDataUpdate)
            {
                Left.LoadRawTextureData(_lastImgLeft);
                Left.Apply();

                Right.LoadRawTextureData(_lastImgRight);
                Right.Apply();

                _imgDataUpdate = false;
            }

            if (!_workerObject.GetUpdatedData())
            {
                if (!DemoMode)
                {
                    var dvcIndex = _liveCamera.Device.DeviceIndex;
                    var bytePtr = (byte*) AVProLiveCameraPlugin.GetLastFrameBuffered(dvcIndex).ToPointer();
                    _workerObject.SetUpdatedData(bytePtr);
                }
                else
                {
                    fixed (byte* bytePtr = _sampleData)
                        _workerObject.SetUpdatedData(bytePtr);
                }
            }
        }
    }

    private void CreateNewTexture(Texture liveCamTexture, StereoFormat format)
    {
        if (format == StereoFormat.SideBySide)
        {
            Left = new Texture2D(liveCamTexture.width/2, liveCamTexture.height, TextureFormat.RGB24, false);
            Right = new Texture2D(liveCamTexture.width/2, liveCamTexture.height, TextureFormat.RGB24, false);
            _rectL = new Rect(0, 0, Left.width, Left.height);
            _rectR = new Rect(Right.width, 0, Right.width*2, Right.height);
        }
        else if (format == StereoFormat.FramePacking)
        {
            var width = liveCamTexture.width;
            var height = liveCamTexture.height;

            if (ForceFullHd)
            {
                width = 1920;
                height = 1080;
            }

            Left = new Texture2D(width, height, TextureFormat.RGB24, false);
            Right = new Texture2D(width, height, TextureFormat.RGB24, false);
            Complete = new Texture2D(width, height, TextureFormat.RGB24, false);

            if (DemoMode)
            {
                _sampleData = ReadSampleFromFile("16520");
            }

            _workerObject = new CVThread(2 * width, height, UpdateImgData);
            _workerThread = new Thread(_workerObject.ProcessImage);
            _workerThread.Start();
        }
    }
}
