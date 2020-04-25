using MccDaq;
using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Bonsai.MccDaq
{
    [Description("Sets the value of an analog output channel in a Measurement Computing device.")]
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
                    if (error.Value != ErrorInfo.ErrorCode.NoErrors)
                    {
                        throw new InvalidOperationException(error.Message);
                    }
                });
            });
        }
    }
}
