using System;
using System.Diagnostics;
using System.Threading;
using Emgu.CV;
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
    private volatile bool _updatedData;

    private volatile int _imgWidth;
    private volatile int _imgHeight;

    private readonly YUV2RGBThread _imgConvLeft;
    private readonly YUV2RGBThread _imgConvRight;

    private byte* _imgData;

    public CVThread(int width, int height, ConvDataCallback callback)
    {
        _runCounter = 0;

        _shouldStop = false;
        _updatedData = false;

        _imgWidth = width;
        _imgHeight = height;

        _convCallback = callback;
    }

    public void SetUpdatedData(byte* data)
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

    private byte[] GetImageData(Image<Rgba, byte> img)
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

            // convert YUV2RGB line by line
            var dataLen = _imgWidth*_imgHeight*4;
            var rgbImgLeft = new byte[(int)(dataLen * 1.5f)];
            var rgbImgRight = new byte[(int)(dataLen * 1.5f)];

            var doneEvent = new ManualResetEvent(false);
            var lineConvArr = new YUV2RGBThread[_imgHeight];
            var taskCount = _imgHeight;

            fixed (byte* pRGBsLeft = rgbImgLeft, pRGBsRight = rgbImgRight)
            {
                for (var y = 0; y < _imgHeight; y++)
                {
                    byte* pRGBLeft = pRGBsLeft + y * _imgWidth * 3 / 2;
                    byte* pRGBRight = pRGBsRight + y * _imgWidth * 3 / 2;

                    byte* pYUV = _imgData + y * _imgWidth * 2;

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
            }

            doneEvent.WaitOne();

            // return data
            if (_convCallback != null)
                _convCallback(rgbImgLeft, rgbImgRight);

            UnityEngine.Debug.Log("End: " + watch.ElapsedMilliseconds);

            // wait for new data
            _updatedData = false;
            _runCounter++;
        }
    }
}