using System;
using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
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

	private const bool forceFullHD = true;

    public enum StereoFormat
    {
        FramePacking,
        SideBySide
    }

    // Use this for initialization
    private IEnumerator Start()
    {
        Left = Right = null;
        _liveCamera = this.GetComponent<AVProLiveCamera>();

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

    private Image<Rgb, byte> ConvertYUV2RGB(Image<Rgba, byte> input, int width, int height)
    {
        var watch = new Stopwatch();
        watch.Start();

        var yuvImg = input.Convert<Rgba, float>();

        var c1 = yuvImg[1] - 16;
        var c2 = yuvImg[3] - 16;
        var d = yuvImg[2] - 128;
        var e = yuvImg[0] - 128;

       // UnityEngine.Debug.Log("1. " + watch.ElapsedMilliseconds);

        // first part of image
        var rgbFst = new Image<Rgb, byte>(width / 2, height);

        var r1 = (((298 * c1 + 409 * e + 128) / 256) - 0.5f);
        r1 = r1.ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));

      //  UnityEngine.Debug.Log("2. " + watch.ElapsedMilliseconds);

        var g1 = (((298 * c1 - 100 * d - 208 * e + 128) / 256) - 0.5f);
        g1 = g1.ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));

        var b1 = (((298 * c1 + 516 * d + 128) / 256) - 0.5f);
        b1 = b1.ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));

      //  UnityEngine.Debug.Log("3. " + watch.ElapsedMilliseconds);

        rgbFst[2] = r1.Convert<Gray, byte>();
        rgbFst[1] = g1.Convert<Gray, byte>();
        rgbFst[0] = b1.Convert<Gray, byte>();

      //  UnityEngine.Debug.Log("4. " + watch.ElapsedMilliseconds);

        rgbFst = rgbFst.Resize(width, height, INTER.CV_INTER_NN);

      //  UnityEngine.Debug.Log("5. " + watch.ElapsedMilliseconds);

        // second part of image
        var rgbSnd = new Image<Rgb, byte>(width / 2, height);

        var r2 = (((298 * c2 + 409 * e + 128) / 256) - 0.5f);
        r2 = r2.ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));

        var g2 = (((298 * c2 - 100 * d - 208 * e + 128) / 256) - 0.5f);
        g2 = g2.ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));

        var b2 = (((298 * c2 + 516 * d + 128) / 256) - 0.5f);
        b2 = b2.ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));

        rgbSnd[2] = r2.Convert<Gray, byte>();
        rgbSnd[1] = g2.Convert<Gray, byte>();
        rgbSnd[0] = b2.Convert<Gray, byte>();

        rgbSnd = rgbSnd.Resize(width, height, INTER.CV_INTER_NN);

      //  UnityEngine.Debug.Log("6. " + watch.ElapsedMilliseconds);

        // filter
        rgbFst = rgbFst.And(_filterLeft);
        rgbSnd = rgbSnd.And(_filterRight);



      //  UnityEngine.Debug.Log("7. " + watch.ElapsedMilliseconds);

        return rgbFst + rgbSnd;
    }

    private void ConvertFP(Texture liveCamTexture)
	{
		if (Complete != null)
		{
			var width = liveCamTexture.width;
			var height = liveCamTexture.height;

			if (forceFullHD) {
		    	width = 1920;
		    	height = 1080;
			}

            var camImgYUV = new Image<Rgba, byte>(width, height, 4 * width,
				AVProLiveCameraPlugin.GetLastFrameBuffered(_liveCamera.Device.DeviceIndex));
			camImgYUV = camImgYUV.Flip(FLIP.VERTICAL);

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

            // create filter
            var blackWhite = new Image<Gray, byte>(2, 1, new Gray(0));

			if (!forceFullHD) {
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
        }
    }
}
