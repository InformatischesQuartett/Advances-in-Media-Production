using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

public class MatrixCalcClass
{
    private readonly int _width;
    private readonly int _height;

    private readonly Image<Rgb, byte> _filter;
    private Image<Rgb, byte> _result;

    public MatrixCalcClass(int width, int height, Image<Rgb, byte> filter)
    {
        _width = width;
        _height = height;
        _filter = filter;
    }

    public Image<Rgb, byte> GetResult()
    {
        return _result;
    }

    public void MatrixCalculation(Image<Gray, float> c, Image<Gray, float> d, Image<Gray, float> e)
    {
        c -= 16;

        var rgbPart = new Image<Rgb, float>(_width / 2, _height);
        rgbPart[2] = (((298 * c + 409 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
        rgbPart[1] = (((298 * c - 100 * d - 208 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
        rgbPart[0] = (((298 * c + 516 * d + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));

        _result = rgbPart.Resize(_width, _height, INTER.CV_INTER_NN).Convert<Rgb, byte>().And(_filter);
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

    private Image<Rgba, float> _imgData;

    private readonly MatrixCalcClass _matrixCalcLeft;
    private readonly MatrixCalcClass _matrixCalcRight;

    private Image<Rgb, byte> _filterLeft;
    private Image<Rgb, byte> _filterRight;

    public CVThread(int width, int height, ConvDataCallback callback)
    {
        _runCounter = 0;

        _shouldStop = false;
        _updatedData = false;

        _imgWidth = width;
        _imgHeight = height;

        CreateFilter();

        _convCallback = callback;

        // threading
       _matrixCalcLeft = new MatrixCalcClass(width, height, _filterLeft);
       _matrixCalcRight = new MatrixCalcClass(width, height, _filterRight);
    }

    private void CreateFilter()
    {
        _filterLeft = new Image<Rgb, byte>(_imgWidth, _imgHeight);
        _filterRight = new Image<Rgb, byte>(_imgWidth, _imgHeight);

        var blackWhite = new Image<Gray, byte>(2, 1, new Gray(0));

        blackWhite.Data[0, 0, 0] = 255;
        CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), _filterLeft);

        blackWhite.Data[0, 0, 0] = 0;
        blackWhite.Data[0, 1, 0] = 255;
        CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), _filterRight);
    }

    public void SetUpdatedData(Image<Rgba, float> data)
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

    private byte[] GetImageData(Image<Rgb, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
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

            // left image
            var imgLeftYUV = _imgData.Copy(new Rectangle(0, 0, _imgWidth / 2, _imgHeight));
            var imgLeftRGB = GetImageData(ConvertYUV2RGB(imgLeftYUV));

            // right image
            var imgRightYUV = _imgData.Copy(new Rectangle(_imgWidth / 2, 0, _imgWidth / 2, _imgHeight));
            var imgRightRGB = GetImageData(ConvertYUV2RGB(imgRightYUV));

            // return data
            if (_convCallback != null)
                _convCallback(imgLeftRGB, imgRightRGB);

            UnityEngine.Debug.Log(" End1: " + watch.ElapsedMilliseconds + " / " + watch.ElapsedTicks);

            // wait for new data
            _updatedData = false;
            _runCounter++;
        }
    }

    private Image<Rgb, byte> ConvertYUV2RGB(Image<Rgba, float> imgPart)
    {
        var d = imgPart[2] - 128;
        var e = imgPart[0] - 128;

        var runningCt = 2;
        var joinEvent = new AutoResetEvent(false);

        ThreadPool.QueueUserWorkItem(delegate
        {
            _matrixCalcLeft.MatrixCalculation(imgPart[1], d, e);
            if (0 == Interlocked.Decrement(ref runningCt))
                joinEvent.Set();
        });

        ThreadPool.QueueUserWorkItem(delegate
        {
            _matrixCalcRight.MatrixCalculation(imgPart[3], d, e);
            if (0 == Interlocked.Decrement(ref runningCt))
                joinEvent.Set();
        });

        joinEvent.WaitOne();

        return _matrixCalcLeft.GetResult() + _matrixCalcRight.GetResult();
    }
}