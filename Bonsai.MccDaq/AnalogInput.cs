using MccDaq;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bonsai.MccDaq
{
    public class AnalogInput : Source<Mat>
    {
        public AnalogInput()
        {
            SampleCount = 32;
            SampleRate = 1000;
            Range = global::MccDaq.Range.NotUsed;
        }

        public int BoardNumber { get; set; }

        public int SampleCount { get; set; }

        public int SampleRate { get; set; }

        public int LowChannel { get; set; }

        public int HighChannel { get; set; }

        public global::MccDaq.Range Range { get; set; }

        static void ThrowExceptionForErrorInfo(ErrorInfo error)
        {
            if (error.Value != ErrorInfo.ErrorCode.NoErrors)
            {
                throw new InvalidOperationException(error.Message);
            }
        }

        public override IObservable<Mat> Generate()
        {
            const int PacketSize = 256;
            return Observable.Create<Mat>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var range = Range;
                    var sampleRate = SampleRate;
                    var sampleCount = SampleCount;
                    var lowChannel = LowChannel;
                    var highChannel = HighChannel;
                    var board = new MccBoard(BoardNumber);
                    var channels = highChannel - lowChannel + 1;
                    var dataPoints = sampleCount * channels;
                    var bufferSize = PacketSize * (sampleCount * 2 / PacketSize + 1);
                    var bufferPoints = bufferSize * channels;
                    var buffer = new Mat(bufferSize, channels, Depth.S16, 1);
                    var options = ScanOptions.Continuous | ScanOptions.Background;
                    using (var waitEvent = new AutoResetEvent(false))
                    using (var cancellation = cancellationToken.Register(() => waitEvent.Set()))
                    {
                        try
                        {
                            EventCallback dataAvailable = delegate { waitEvent.Set(); };
                            var error = board.EnableEvent(EventType.OnDataAvailable, dataPoints, dataAvailable, IntPtr.Zero);
                            ThrowExceptionForErrorInfo(error);

                            error = board.AInScan(lowChannel, highChannel, bufferPoints, ref sampleRate, range, buffer.Data, options);
                            ThrowExceptionForErrorInfo(error);

                            var readIndex = 0;
                            while (!cancellationToken.IsCancellationRequested)
                            {
                                waitEvent.WaitOne();
                                error = board.GetStatus(out short status, out int count, out int index, FunctionType.AiFunction);
                                ThrowExceptionForErrorInfo(error);
                                if (index < 0) continue;

                                index /= channels;
                                var overflowCount = 0;
                                var readCount = index - readIndex;
                                if (readCount < 0) // overflow
                                {
                                    overflowCount = bufferSize - readIndex;
                                    readCount = index + overflowCount;
                                }

                                while (readCount >= sampleCount)
                                {
                                    readCount -= sampleCount;
                                    var output = new Mat(channels, sampleCount, buffer.Depth, buffer.Channels);
                                    if (overflowCount > 0)
                                    {
                                        overflowCount -= sampleCount;
                                        if (overflowCount < 0) // wrap-around buffer, copy remainder
                                        {
                                            var overflowSamples = bufferSize - readIndex;
                                            using (var outputOverflow = output.GetSubRect(new Rect(0, 0, overflowSamples, channels)))
                                            using (var dataOverflow = buffer.GetSubRect(new Rect(0, readIndex, channels, overflowSamples)))
                                            {
                                                CV.Transpose(dataOverflow, outputOverflow);
                                            }

                                            readIndex = 0;
                                            overflowSamples = sampleCount - overflowSamples;
                                            using (var outputOverflow = output.GetSubRect(new Rect(sampleCount - overflowSamples, 0, overflowSamples, channels)))
                                            using (var dataOverflow = buffer.GetSubRect(new Rect(0, readIndex, channels, overflowSamples)))
                                            {
                                                CV.Transpose(dataOverflow, outputOverflow);
                                            }

                                            observer.OnNext(output);
                                            continue;
                                        }
                                    }

                                    using (var data = buffer.GetSubRect(new Rect(0, readIndex, channels, sampleCount)))
                                    {
                                        CV.Transpose(data, output);
                                    }
                                    readIndex = (readIndex + sampleCount) % bufferSize;
                                    observer.OnNext(output);
                                }
                            }
                        }
                        finally
                        {
                            board.StopBackground(FunctionType.AiFunction);
                            board.DisableEvent(EventType.OnDataAvailable);
                        }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            });
        }
    }
}
