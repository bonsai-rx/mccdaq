using MccDaq;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.MccDaq
{
    public class AnalogScan : Source<Mat>
    {
        public AnalogScan()
        {
            BufferSize = 10;
            SampleRate = 1000;
            Range = global::MccDaq.Range.NotUsed;
        }

        public int BoardNumber { get; set; }

        public int BufferSize { get; set; }

        public int SampleRate { get; set; }

        public int LowChannel { get; set; }

        public int HighChannel { get; set; }

        public global::MccDaq.Range Range { get; set; }

        public override IObservable<Mat> Generate()
        {
            return Observable.Create<Mat>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var range = Range;
                    var sampleRate = SampleRate;
                    var bufferSize = BufferSize;
                    var lowChannel = LowChannel;
                    var highChannel = HighChannel;
                    var board = new MccBoard(BoardNumber);
                    var channels = highChannel - lowChannel + 1;
                    var buffer = new Mat(bufferSize, channels, Depth.S16, 1);
                    var numSamples = buffer.Rows * buffer.Cols;
                    var options = ScanOptions.Default;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var error = board.AInScan(lowChannel, highChannel, numSamples, ref sampleRate, range, buffer.Data, options);
                        if (error.Value != ErrorInfo.ErrorCode.NoErrors)
                        {
                            observer.OnError(new InvalidOperationException(error.Message));
                        }

                        var output = new Mat(buffer.Cols, buffer.Rows, buffer.Depth, buffer.Channels);
                        CV.Transpose(buffer, output);
                        observer.OnNext(output);
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            });
        }
    }
}
