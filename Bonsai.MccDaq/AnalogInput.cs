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
    public class AnalogInput : Source<Mat>
    {
        public AnalogInput()
        {
            BufferSize = 10;
            SampleRate = 1000;
            Range = global::MccDaq.Range.Bip5Volts;
            Options = ScanOptions.Default;
        }

        public int BoardNumber { get; set; }

        public int BufferSize { get; set; }

        public int SampleRate { get; set; }

        public int LowChannel { get; set; }

        public int HighChannel { get; set; }

        public global::MccDaq.Range Range { get; set; }

        public ScanOptions Options { get; set; }

        public override IObservable<Mat> Generate()
        {
            return Observable.Create<Mat>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var boardNumber = BoardNumber;
                    var sampleRate = SampleRate / 2;
                    DaqDeviceManager.IgnoreInstaCal();
                    var devices = DaqDeviceManager.GetDaqDeviceInventory(DaqDeviceInterface.Usb);
                    var board = DaqDeviceManager.CreateDaqDevice(boardNumber, devices[boardNumber]);
                    try
                    {
                        var lowChannel = LowChannel;
                        var highChannel = HighChannel;
                        var channels = highChannel - lowChannel + 1;
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var output = new Mat(channels, BufferSize, Depth.S16, 1);
                            var error = board.AInScan(lowChannel, highChannel, output.Cols, ref sampleRate, Range, output.Data, Options);
                            if (error.Value != ErrorInfo.ErrorCode.NoErrors)
                            {
                                observer.OnError(new InvalidOperationException(error.Message));
                            }

                            observer.OnNext(output);
                        }
                    }
                    finally
                    {
                        DaqDeviceManager.ReleaseDaqDevice(board);
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            });
        }
    }
}
