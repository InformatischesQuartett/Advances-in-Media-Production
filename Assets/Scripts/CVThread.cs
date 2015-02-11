using System;
using System.CodeDom;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

public unsafe class LineConvThread
{
    private byte* _pYUV;
    private byte* _pRGB;

    private readonly int _imgWidth;

    public LineConvThread(int imgWidth, byte* pYUV, byte* pRGB)
    {
        _imgWidth = imgWidth;

        _pYUV = pYUV;
        _pRGB = pRGB;
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

            // first pixel
            var r1 = (298*c1 + 409*e + 128) >> 8;
            var g1 = (298*c1 - 100*d - 208*e + 128) >> 8;
            var b1 = (298*c1 + 516*d + 128) >> 8;

            _pRGB[2] = (byte) (r1 < 0 ? 0 : r1 > 255 ? 255 : r1);
            _pRGB[1] = (byte) (g1 < 0 ? 0 : g1 > 255 ? 255 : g1);
            _pRGB[0] = (byte) (b1 < 0 ? 0 : b1 > 255 ? 255 : b1);

            // second pixel
            var r2 = (298*c2 + 409*e + 128) >> 8;
            var g2 = (298*c2 - 100*d - 208*e + 128) >> 8;
            var b2 = (298*c2 + 516*d + 128) >> 8;

            _pRGB[5] = (byte) (r2 < 0 ? 0 : r2 > 255 ? 255 : r2);
            _pRGB[4] = (byte) (g2 < 0 ? 0 : g2 > 255 ? 255 : g2);
            _pRGB[3] = (byte) (b2 < 0 ? 0 : b2 > 255 ? 255 : b2);

            // next
			_pRGB += 6;
			_pYUV += 4;
        }
    }
}

public class YUV2RGBConvThread
{
    private readonly int _imgWidth;
    private readonly int _imgHeight;

    private byte[] _result;

    public YUV2RGBConvThread(int width, int height)
    {
        _imgWidth = width;
        _imgHeight = height;
    }

    public byte[] GetResult()
    {
        return _result;
    }

    private byte[] GetImageData(Image<Rgba, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
    }

    public unsafe void ConvertYUV2RGB(Image<Rgba, byte> imgPart)
    {
        var imgDataYUV = GetImageData(imgPart);
        _result = new byte[(int) (imgDataYUV.Length*1.5f)];

        var doneEvent = new ManualResetEvent(false);
        var lineConvArr = new LineConvThread[_imgHeight];
        var taskCount = _imgHeight;

        fixed (byte* pRGBs = _result, pYUVs = imgDataYUV)
        {
            for (var y = 0; y < _imgHeight; y++)
            {
                byte* pRGB = pRGBs + y*_imgWidth*3;
                byte* pYUV = pYUVs + y*_imgWidth*2;

                var lineConv = new LineConvThread(_imgWidth, pYUV, pRGB);
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
        }

        doneEvent.WaitOne();

        UnityEngine.Debug.Log("Hier");

        fixed (byte* test = _result)
        {
            var testimg = new Image<Rgb, byte>(3840, 1080, 6*1920, new IntPtr(test));
            UnityEngine.Debug.Log("Da"); 
            testimg.Save("Test.jpg");
            UnityEngine.Debug.Log("Dort");

        }
    }
}

public class CVThread
{
    private readonly ConvDataCallback _convCallback;

    private volatile int _runCounter;

    private volatile bool _shouldStop;
    private volatile bool _updatedData;

    private volatile int _imgWidth;
    private volatile int _imgHeight;

    private readonly YUV2RGBConvThread _imgConvLeft;
    private readonly YUV2RGBConvThread _imgConvRight;

    private Image<Rgba, byte> _imgData;

    public CVThread(int width, int height, ConvDataCallback callback)
    {
        _runCounter = 0;

        _shouldStop = false;
        _updatedData = false;

        _imgWidth = width;
        _imgHeight = height;

        _convCallback = callback;

        // threading
        _imgConvLeft = new YUV2RGBConvThread(2*width, height);
        _imgConvRight = new YUV2RGBConvThread(2*width, height);
    }

    public void SetUpdatedData(Image<Rgba, byte> data)
    {
        _imgData = data;
        _updatedData = true;
    }

    public bool GetUpdatedData()
    {
        return _updatedData;
    }

    public int GetRunCount()
    {
        return _runCounter;
    }

    public void RequestStop()
    {
        _shouldStop = true;
    }

    public void ProcessImage()
    {
        while (!_shouldStop)
        {
            while (!_updatedData && !_shouldStop)
                Thread.Sleep(0);

            if (_shouldStop) return;

            var watch = new Stopwatch();
            watch.Start();

            var imgLeftYUV = _imgData.Copy(new Rectangle(0, 0, _imgWidth/2, _imgHeight));
            var imgRightYUV = _imgData.Copy(new Rectangle(_imgWidth/2, 0, _imgWidth/2, _imgHeight));

            var runningCt = 1;
            var joinEvent = new AutoResetEvent(false);

            ThreadPool.QueueUserWorkItem(delegate
            {
                _imgData = _imgData.Copy();
                _imgConvLeft.ConvertYUV2RGB(_imgData);
                if (0 == Interlocked.Decrement(ref runningCt))
                    joinEvent.Set();
            });

          /*  ThreadPool.QueueUserWorkItem(delegate
            {
                _imgConvRight.ConvertYUV2RGB(imgRightYUV);
                if (0 == Interlocked.Decrement(ref runningCt))
                    joinEvent.Set();
            });*/

            joinEvent.WaitOne();

            // return data
            if (_convCallback != null)
                _convCallback(_imgConvLeft.GetResult(), _imgConvLeft.GetResult());

            UnityEngine.Debug.Log("End: " + watch.ElapsedMilliseconds);

            // wait for new data
            _updatedData = false;
            _runCounter++;
        }
    }
}