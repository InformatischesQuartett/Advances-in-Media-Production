using System;
using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

public class CreateTwoTexture : MonoBehaviour
{
    private AVProLiveCamera _liveCamera;

    public StereoFormat Format;

    public Texture2D Left { get; private set; }
    public Texture2D Right { get; private set; }
    public Texture2D Complete { get; private set; }

    private Rect _rectL, _rectR;

    private bool _first = true;

    private Image<Rgb, byte> _filterLeft;
    private Image<Rgb, byte> _filterRight;
    private Image<Rgb, float>[] _rgbParts;

	private const bool ForceFullHd = true;
    private const bool DemoMode = true;
    private byte[] _sampleData;

    private bool _fstThread = false;
    private bool _sndThread = false;

    public enum StereoFormat
    {
        FramePacking,
        SideBySide
    }

    public struct ThreadData
    {
        public Image<Rgba, float> YUVImg;
        public int Part;
        public int Width;
        public int Height;
    }

    // Use this for initialization
    private IEnumerator Start()
    {
        Left = Right = null;
        _liveCamera = GetComponent<AVProLiveCamera>();

        yield return new WaitForSeconds(1);

        CreateNewTexture(_liveCamera.OutputTexture, Format);
    }

    private void OnGUI()
    {
        if (_liveCamera.OutputTexture != null && Left != null && Right != null)
        {
            GUI.DrawTexture(new Rect(250, 0, 200, 200), _liveCamera.OutputTexture, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(150, 200, 200, 200), Left, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(350, 200, 200, 200), Right, ScaleMode.ScaleToFit, false);
        }
    }

    private void Update()
    {
        if (_liveCamera.OutputTexture == null)
            return;

        if (_first)
        {
            GetComponent<MaterialCreator>().Init();
            _first = false;
        }

        Convert();
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

    private byte[] GetImageData(Image<Rgba, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
    }

    private byte[] GetImageData(Image<Rgb, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
    }

    private Image<Rgb, byte> ConvertYUV2RGB(Image<Rgba, float> yuvImg, int width, int height)
    {
        var watch = new Stopwatch();
        watch.Start();

        _fstThread = false;
        _sndThread = false;

        var fstThread = new Thread(MatrixCalculation);
        fstThread.Start(new ThreadData{ YUVImg = yuvImg, Part = 1, Width = width, Height = height });

        var sndThread = new Thread(MatrixCalculation);
        sndThread.Start(new ThreadData { YUVImg = yuvImg, Part = 2, Width = width, Height = height });

        fstThread.Join();
        sndThread.Join();

        UnityEngine.Debug.Log("End: " + watch.ElapsedMilliseconds + " / " + watch.ElapsedTicks);

        // filter
        _rgbParts[0] = _rgbParts[0].Resize(width, height, INTER.CV_INTER_NN);
        _rgbParts[1] = _rgbParts[1].Resize(width, height, INTER.CV_INTER_NN);

        return _rgbParts[0].Convert<Rgb, byte>().And(_filterLeft) +
               _rgbParts[1].Convert<Rgb, byte>().And(_filterRight);
    }

    private void MatrixCalculation(object threadDataVar)
    {
        var threadData = (ThreadData) threadDataVar;

        var d = threadData.YUVImg[2] - 128;
        var e = threadData.YUVImg[0] - 128;

        if (threadData.Part == 1)
        {
            var c1 = threadData.YUVImg[1] - 16;

            _rgbParts[0] = new Image<Rgb, float>(threadData.Width / 2, threadData.Height);
            _rgbParts[0][2] = (((298 * c1 + 409 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
            _rgbParts[0][1] = (((298 * c1 - 100 * d - 208 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
            _rgbParts[0][0] = (((298 * c1 + 516 * d + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
        }
        else
        {
            var c2 = threadData.YUVImg[3] - 16;

            _rgbParts[1] = new Image<Rgb, float>(threadData.Width / 2, threadData.Height);
            _rgbParts[1][2] = (((298 * c2 + 409 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
            _rgbParts[1][1] = (((298 * c2 - 100 * d - 208 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
            _rgbParts[1][0] = (((298 * c2 + 516 * d + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
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
            long valueCt = br.BaseStream.Length / sizeof(byte);
            var readArr = new byte[valueCt];

            for (int x = 0; x < valueCt; x++)
                readArr[x] = br.ReadByte();

            return readArr;
        }
    }

	private unsafe void ConvertFP(Texture liveCamTexture)
	{
		if (Complete != null)
		{
			var width = liveCamTexture.width;
			var height = liveCamTexture.height;

            //FixDepthFromFile();

			if (ForceFullHd) {
		    	width = 1920;
		    	height = 1080;
			}

		   

		    Image<Rgba, float> camImgYUV;

		    if (!DemoMode)
		    {
		        camImgYUV = new Image<Rgba, byte>(width, height, 4*width,
                    AVProLiveCameraPlugin.GetLastFrameBuffered(_liveCamera.Device.DeviceIndex)).Convert<Rgba, float>();
		        camImgYUV = camImgYUV.Flip(FLIP.VERTICAL);
		    }
		    else
		    {
		        fixed (byte* ptr = _sampleData)
		        {
		            camImgYUV = new Image<Rgba, byte>(width, height, 4*width, new IntPtr(ptr)).Convert<Rgba, float>();
		        }
		    }

		    // left image
		    var imgLeftYUV = camImgYUV.Copy(new Rectangle(0, 0, width/2, height));
		    var imgLeftRGB = GetImageData(ConvertYUV2RGB(imgLeftYUV, width, height));

            Left.LoadRawTextureData(imgLeftRGB);
            Left.Apply();

            // right image
            var imgRightYUV = camImgYUV.Copy(new Rectangle(width / 2, 0, width / 2, height));
		    var imgRightRGB = GetImageData(ConvertYUV2RGB(imgRightYUV, width, height));

            Right.LoadRawTextureData(imgRightRGB);
            Right.Apply();
        }
        else
            CreateNewTexture(liveCamTexture, Format);
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
			Left = new Texture2D(liveCamTexture.width, liveCamTexture.height/2, TextureFormat.RGB24, false);
			Right = new Texture2D(liveCamTexture.width, liveCamTexture.height/2, TextureFormat.RGB24, false);
			Complete = new Texture2D(liveCamTexture.width, liveCamTexture.height, TextureFormat.RGB24, false);

            _rgbParts = new Image<Rgb, float>[2];

            // create filter
            var blackWhite = new Image<Gray, byte>(2, 1, new Gray(0));

			if (!ForceFullHd) {
	            _filterLeft = new Image<Rgb, byte>(liveCamTexture.width, liveCamTexture.height);
	            _filterRight = new Image<Rgb, byte>(liveCamTexture.width, liveCamTexture.height);
			} else {
				_filterLeft = new Image<Rgb, byte>(1920, 1080);
				_filterRight = new Image<Rgb, byte>(1920, 1080);
			}
				
			blackWhite.Data[0, 0, 0] = 255;
            CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), _filterLeft);

            blackWhite.Data[0, 0, 0] = 0;
            blackWhite.Data[0, 1, 0] = 255;
            CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), _filterRight);

            if (DemoMode)
            {
                _sampleData = ReadSampleFromFile("16520");
            }
        }
    }
}
