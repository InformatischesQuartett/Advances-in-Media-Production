using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

public unsafe class YUV2RGBThread
{
    private byte* _pYUV;
    private byte* _pRGBLeft;
    private byte* _pRGBRight;

    private readonly int _imgWidth;

    public YUV2RGBThread(int imgWidth, byte* pYUV, byte* pRGBLeft, byte* pRGBRight)
    {
        _imgWidth = imgWidth;

        _pYUV = pYUV;
        _pRGBLeft = pRGBLeft;
        _pRGBRight = pRGBRight;
    }

    public void LineCalculation()
    {
        // process two pixels at a time
        for (var x = 0; x < _imgWidth; x += 2)
        {
            var c1 = _pYUV[1] - 16;
            var c2 = _pYUV[3] - 16;

            var d = _pYUV[2] - 128;
            var e = _pYUV[0] - 128;

            var r1 = (298*c1 + 409*e + 128) >> 8;
            var g1 = (298*c1 - 100*d - 208*e + 128) >> 8;
            var b1 = (298*c1 + 516*d + 128) >> 8;

            var r2 = (298*c2 + 409*e + 128) >> 8;
            var g2 = (298*c2 - 100*d - 208*e + 128) >> 8;
            var b2 = (298*c2 + 516*d + 128) >> 8;

            if (x < _imgWidth/2)
            {
                _pRGBLeft[2] = (byte) (r1 < 0 ? 0 : r1 > 255 ? 255 : r1);
                _pRGBLeft[1] = (byte) (g1 < 0 ? 0 : g1 > 255 ? 255 : g1);
                _pRGBLeft[0] = (byte) (b1 < 0 ? 0 : b1 > 255 ? 255 : b1);

                _pRGBLeft[5] = (byte) (r2 < 0 ? 0 : r2 > 255 ? 255 : r2);
                _pRGBLeft[4] = (byte) (g2 < 0 ? 0 : g2 > 255 ? 255 : g2);
                _pRGBLeft[3] = (byte) (b2 < 0 ? 0 : b2 > 255 ? 255 : b2);

                _pRGBLeft += 6;
            }
            else
            {
                _pRGBRight[2] = (byte) (r1 < 0 ? 0 : r1 > 255 ? 255 : r1);
                _pRGBRight[1] = (byte) (g1 < 0 ? 0 : g1 > 255 ? 255 : g1);
                _pRGBRight[0] = (byte) (b1 < 0 ? 0 : b1 > 255 ? 255 : b1);

                _pRGBRight[5] = (byte) (r2 < 0 ? 0 : r2 > 255 ? 255 : r2);
                _pRGBRight[4] = (byte) (g2 < 0 ? 0 : g2 > 255 ? 255 : g2);
                _pRGBRight[3] = (byte) (b2 < 0 ? 0 : b2 > 255 ? 255 : b2);

                _pRGBRight += 6;
            }

			_pYUV += 4;
        }
    }
}

public unsafe class CVThread
{
    private readonly ConvDataCallback _convCallback;

    private volatile int _runCounter;
    private volatile bool _shouldStop;

    private readonly int _imgWidth;
    private readonly int _imgHeight;
    private readonly StereoFormat _imgMode;

    private int _deviceID;

    private Capture _vidCapture;
    private int _frameRate;

    private readonly YUV2RGBThread _imgConvLeft;
    private readonly YUV2RGBThread _imgConvRight;

    private readonly byte[] _imgData;

    public CVThread(int width, int height, StereoFormat mode, ConvDataCallback callback, int dID = -1, byte[] imgData = null)
    {
        _runCounter = 0;
        _shouldStop = false;

        _imgWidth = (mode == StereoFormat.FramePacking) ? 2 * width : width;
        _imgHeight = height;
        _imgMode = mode;
        _imgData = imgData;

        _convCallback = callback;
        _deviceID = dID;

        _frameRate = 30;

        if (mode == StereoFormat.VideoSample)
            LoadVideoSample();
    }

    public int GetRunCount()
    {
        return _runCounter;
    }

    public void RequestStop()
    {
        _shouldStop = true;
    }

    private void LoadVideoSample()
    {
        _vidCapture = new Capture(UnityEngine.Application.streamingAssetsPath + "/Samples/Dracula.ogv");
        _frameRate = (int) _vidCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_FPS);
    }

    private byte[] GetImageData(Image<Rgb, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
    }

    public void ProcessVideo(out byte[] rgbImgLeft, out byte[] rgbImgRight)
    {
        var frame = _vidCapture.QueryFrame();

        if (frame == null)
        {
            _vidCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_POS_AVI_RATIO, 0);
            frame = _vidCapture.QueryFrame();
        }

        var frameRgb = frame.Convert<Rgb, byte>();

        rgbImgLeft = GetImageData(frameRgb.Copy(new Rectangle(0, 0, 1920, 1080)));
        rgbImgRight = GetImageData(frameRgb.Copy(new Rectangle(0, 0, 1920, 1080)));
    }

    private void ProcessCamera(byte* imgDataPtr, ref byte[] rgbImgLeft, ref byte[] rgbImgRight)
    {
        var doneEvent = new ManualResetEvent(false);
        var lineConvArr = new YUV2RGBThread[_imgHeight];
        var taskCount = _imgHeight;

        fixed (byte* pRGBsLeft = rgbImgLeft, pRGBsRight = rgbImgRight)
        {
            for (var y = 0; y < _imgHeight; y++)
            {
                byte* pRGBLeft = pRGBsLeft + y * _imgWidth * 3 / 2;
                byte* pRGBRight = pRGBsRight + y * _imgWidth * 3 / 2;

                byte* pYUV = imgDataPtr + y * _imgWidth * 2;

                var lineConv = new YUV2RGBThread(_imgWidth, pYUV, pRGBLeft, pRGBRight);
                lineConvArr[y] = lineConv;

                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        lineConv.LineCalculation();
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref taskCount) == 0)
                            doneEvent.Set();
                    }
                });
            }

            doneEvent.WaitOne();
        }
    }

    public void ProcessImage()
    {
        while (!_shouldStop)
        {
            var frameWatch = new Stopwatch();
            frameWatch.Start();

            // convert data
            float dataLen = _imgWidth * _imgHeight;

            if (_imgMode == StereoFormat.VideoSample)
                dataLen *= 3;
            else
                dataLen *= 4 * 1.5f;

            var rgbImgLeft = new byte[(int)dataLen];
            var rgbImgRight = new byte[(int)dataLen];

            switch (_imgMode)
            {
                case StereoFormat.VideoSample:
                    ProcessVideo(out rgbImgLeft, out rgbImgRight);
                    break;

                case StereoFormat.DemoMode:
                    fixed (byte* imgDataPtr = _imgData)
                        ProcessCamera(imgDataPtr, ref rgbImgLeft, ref rgbImgRight);
                    break;

                case StereoFormat.SideBySide:
                case StereoFormat.FramePacking:
                    var imgCamPtr = (byte*) AVProLiveCameraPlugin.GetLastFrameBuffered(_deviceID).ToPointer();
                    ProcessCamera(imgCamPtr, ref rgbImgLeft, ref rgbImgRight);
                    break;
            }

            // return data
            if (_convCallback != null)
                _convCallback(rgbImgLeft, rgbImgRight);

            // limit to specific frame rate
            while (frameWatch.ElapsedMilliseconds < 1000 / _frameRate)
                Thread.Sleep(1);

            _runCounter++;
        }
    }
}