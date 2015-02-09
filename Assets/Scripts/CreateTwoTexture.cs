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

				    int R1 = (298*C1 + 409*E + 128) >> 8;
				    int G1 = (298*C1 - 100*D - 208*E + 128) >> 8;
				    int B1 = (298*C1 + 516*D + 128) >> 8;
					
					int R2 = (298 * C2 + 409 * E + 128) >> 8;
					int G2 = (298 * C2 - 100 * D - 208 * E + 128) >> 8;
					int B2 = (298 * C2 + 516 * D + 128) >> 8;

					pRGB[2] = (byte)(R1 < 0 ? 0 : R1 > 255 ? 255 : R1);
					pRGB[1] = (byte)(G1 < 0 ? 0 : G1 > 255 ? 255 : G1);
					pRGB[0] = (byte)(B1 < 0 ? 0 : B1 > 255 ? 255 : B1);
					
					pRGB[5] = (byte)(R2 < 0 ? 0 : R2 > 255 ? 255 : R2);
					pRGB[4] = (byte)(G2 < 0 ? 0 : G2 > 255 ? 255 : G2);
					pRGB[3] = (byte)(B2 < 0 ? 0 : B2 > 255 ? 255 : B2);
					
					pRGB += 6;
					pYUV += 4;
				}
			}
		}
	}
	
	private unsafe void ConvertFP(Texture liveCamTexture)
	{
		if (Complete != null)
		{
		    var width = liveCamTexture.width;
		    var height = liveCamTexture.height;

            var depthImgYUV = new Image<Rgba, byte>(width, height, 4 * width,
				AVProLiveCameraPlugin.GetLastFrameBuffered(_liveCamera.Device.DeviceIndex));
			depthImgYUV = depthImgYUV.Flip(FLIP.VERTICAL);

			// left image
            var depthImgLeft = depthImgYUV.Copy(new Rectangle(0, 0, width / 2, height));
		    var depthImgLeft2 = depthImgLeft.Convert<Rgba, short>();

		    var test = new Image<Rgb, short>(width/2, height);
		    var test2 = new Image<Rgb, short>(width/2, height);

            var c1 = depthImgLeft2[1] - 16;
            var c2 = depthImgLeft2[3] - 16;
            var d = depthImgLeft2[2] - 128;
            var e = depthImgLeft2[0] - 128;

            test[0] = (298 * c1 + 409 * e + 128) / 256;
            test[1] = (298 * c1 - 100 * d - 208 * e + 128) / 256;
            test[2] = (298 * c1 + 516 * d + 128) / 256;

            test2[0] = (298 * c2 + 409 * e + 128) / 256;
            test2[1] = (298 * c2 - 100 * d - 208 * e + 128) / 256;
            test2[2] = (298 * c2 + 516 * d + 128) / 256;

		    test = test.Resize(width, height, INTER.CV_INTER_LINEAR);

            test.Convert<Rgb, byte>().Save("TestRGB1.jpg");
            test2.Convert<Rgb, byte>().Save("TestRGB2.jpg");

			var imgLeftDataYUV = PrepareRenderImage(depthImgLeft);
			var imgLeftDataRGB = new byte[(int) (imgLeftDataYUV.Length * 1.5f)];
			YUV2RGBManaged(ref imgLeftDataYUV, ref imgLeftDataRGB, width, height);

		    fixed (byte* dataptr = imgLeftDataRGB)
		    {
		        var test123 = new Image<Rgb, byte>(width, height, 3 * width, new IntPtr(dataptr));
                test123.Save("TestOrg.jpg");
		    }

			Left.LoadRawTextureData(imgLeftDataRGB);
			Left.Apply();

			// right image
		    var depthImgRight = depthImgYUV.Copy(new Rectangle(width/2, 0, width/2, height));

			var imgRightDataYUV = PrepareRenderImage(depthImgRight);
			var imgRightDataRGB = new byte[(int) (imgRightDataYUV.Length * 1.5f)];
			YUV2RGBManaged(ref imgRightDataYUV, ref imgRightDataRGB, width, height);
			
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
