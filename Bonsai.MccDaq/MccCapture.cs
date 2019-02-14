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
    public class MccCapture : Source<Mat>
    {
        public MccCapture()
        {
            SampleRate = 1000;
            Range = global::MccDaq.Range.Bip5Volts;
            Options = ScanOptions.Default;
        }

        public int BoardNumber { get; set; }

        public int BufferSize { get; set; }

        public int SampleRate { get; set; }

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
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var output = new Mat(1, BufferSize, Depth.S16, 1);
                            var error = board.AInScan(0, 0, output.Cols, ref sampleRate, Range, output.Data, Options);
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
