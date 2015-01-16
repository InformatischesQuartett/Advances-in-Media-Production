using System;
using UnityEngine;
using System.Collections;
using System.Drawing;
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

    /*private void OnGUI()
    {
        if (_liveCamera.OutputTexture != null && Left != null && Right != null)
        {
            GUI.DrawTexture(new Rect(250, 0, 200, 200), _liveCamera.OutputTexture, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(150, 200, 200, 200), Left, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(350, 200, 200, 200), Right, ScaleMode.ScaleToFit, false);
        }
    }*/

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

    private byte[] PrepareRenderImage(Image<Rgba, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
    }

	private static unsafe void YUV2RGBManaged(ref byte[] YUVData, ref byte[] RGBData, int width, int height)
	{
		fixed(byte* pRGBs = RGBData, pYUVs = YUVData)
		{
			for (int r = 0; r < height; r++)
			{
				byte* pRGB = pRGBs + r * width * 3;
				byte* pYUV = pYUVs + r * width * 2;
				
				//process two pixels at a time
				for (int c = 0; c < width; c += 2)
				{
					int C1 = pYUV[1] - 16;
					int C2 = pYUV[3] - 16;
					int D = pYUV[2] - 128;
					int E = pYUV[0] - 128;
					
					int R1 = (298 * C1 + 409 * E + 128) >> 8;
					int G1 = (298 * C1 - 100 * D - 208 * E + 128) >> 8;
					int B1 = (298 * C1 + 516 * D + 128) >> 8;
					
					int R2 = (298 * C2 + 409 * E + 128) >> 8;
					int G2 = (298 * C2 - 100 * D - 208 * E + 128) >> 8;
					int B2 = (298 * C2 + 516 * D + 128) >> 8;

					pRGB[0] = (byte)(R1 < 0 ? 0 : R1 > 255 ? 255 : R1);
					pRGB[1] = (byte)(G1 < 0 ? 0 : G1 > 255 ? 255 : G1);
					pRGB[2] = (byte)(B1 < 0 ? 0 : B1 > 255 ? 255 : B1);
					
					pRGB[3] = (byte)(R2 < 0 ? 0 : R2 > 255 ? 255 : R2);
					pRGB[4] = (byte)(G2 < 0 ? 0 : G2 > 255 ? 255 : G2);
					pRGB[5] = (byte)(B2 < 0 ? 0 : B2 > 255 ? 255 : B2);
					
					pRGB += 6;
					pYUV += 4;
				}
			}
		}
	}
	
	private void ConvertFP(Texture liveCamTexture)
	{
		if (Complete != null)
		{
			var depthImgYUV = new Image<Rgba, byte>(1920, 1080, 4*1920,
			                     AVProLiveCameraPlugin.GetLastFrameBuffered(_liveCamera.Device.DeviceIndex));
			depthImgYUV = depthImgYUV.Flip(FLIP.VERTICAL);

			// left image
			var depthImgLeft = depthImgYUV.Copy(new Rectangle(0, 0, 960, 1080));

			var imgLeftDataYUV = PrepareRenderImage(depthImgLeft);
			var imgLeftDataRGB = new byte[(int) (imgLeftDataYUV.Length * 1.5f)];
			YUV2RGBManaged(ref imgLeftDataYUV, ref imgLeftDataRGB, 1920, 1080);

			Left.LoadRawTextureData(imgLeftDataRGB);
			Left.Apply();

			// right image
			var depthImgRight = depthImgYUV.Copy(new Rectangle(960, 0, 960, 1080));

			var imgRightDataYUV = PrepareRenderImage(depthImgRight);
			var imgRightDataRGB = new byte[(int) (imgRightDataYUV.Length * 1.5f)];
			YUV2RGBManaged(ref imgRightDataYUV, ref imgRightDataRGB, 1920, 1080);
			
			Right.LoadRawTextureData(imgRightDataRGB);
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
        }
    }
}
