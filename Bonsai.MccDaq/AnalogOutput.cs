using MccDaq;
using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;

namespace Bonsai.MccDaq
{
    [Description("Sets the value of one or more analog output channels in a Measurement Computing device.")]
    public class AnalogOutput : Sink<short>
    {
        public AnalogOutput()
        {
            Range = global::MccDaq.Range.NotUsed;
        }

        [Description("The board number as defined in the Instacal system config file.")]
        public int BoardNumber { get; set; }

        [Description("The number of the D/A channel to write.")]
        public int Channel { get; set; }

        [Description("Specifies the range used in the D/A device. If the board has a fixed gain, this parameter is ignored.")]
        public global::MccDaq.Range Range { get; set; }

        static void ThrowExceptionForErrorInfo(ErrorInfo error)
        {
            if (error.Value != ErrorInfo.ErrorCode.NoErrors)
            {
                throw new InvalidOperationException(error.Message);
            }
        }

        public override IObservable<short> Process(IObservable<short> source)
        {
            return Observable.Defer(() =>
            {
                var range = Range;
                var channel = Channel;
                var board = new MccBoard(BoardNumber);
                return source.Do(dataValue =>
                {
                    var error = board.AOut(channel, range, dataValue);
                    ThrowExceptionForErrorInfo(error);
                });
            });
        }

        public IObservable<Mat> Process(IObservable<Mat> source)
        {
            return Observable.Create<Mat>(observer =>
            {
                var range = Range;
                var lowChannel = Channel;
                var board = new MccBoard(BoardNumber);
                var options = ScanOptions.Default;
                var scanObserver = Observer.Create<Mat>(
                    buffer =>
                    {
                        var actualRate = 0;
                        var highChannel = lowChannel + buffer.Rows - 1;
                        var error = board.AOutScan(lowChannel, highChannel, buffer.Rows, ref actualRate, range, buffer.Data, options);
                        try
                        {
                            ThrowExceptionForErrorInfo(error);
                            if (actualRate != 0)
                            {
                                throw new InvalidOperationException($"The specified sampling rate is not available. Suggested rate: {actualRate}");
                            }

                            observer.OnNext(buffer);
                        }
                        catch (Exception ex) { observer.OnError(ex); }
                    },
                    observer.OnError,
                    observer.OnCompleted);
                return source.SubscribeSafe(scanObserver);
            });
        }
    }
}
