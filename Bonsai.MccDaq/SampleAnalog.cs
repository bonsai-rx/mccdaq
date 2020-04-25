using MccDaq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.MccDaq
{
    [Description("Reads a single sample from an analog input channel in a Measurement Computing device.")]
    public class SampleAnalog : Combinator<short>
    {
        [Description("The board number as defined in the Instacal system config file.")]
        public int BoardNumber { get; set; }

        [Description("The number of the A/D channel to read.")]
        public int Channel { get; set; }

        [Description("Specifies the range used in the A/D device. If the board has a fixed gain, this parameter is ignored.")]
        public global::MccDaq.Range Range { get; set; }

        public override IObservable<short> Process<TSource>(IObservable<TSource> source)
        {
            return Observable.Defer(() =>
            {
                var range = Range;
                var channel = Channel;
                var board = new MccBoard(BoardNumber);
                return source.Select(input =>
                {
                    var error = board.AIn(channel, range, out short dataValue);
                    if (error.Value != ErrorInfo.ErrorCode.NoErrors)
                    {
                        throw new InvalidOperationException(error.Message);
                    }
                    return dataValue;
                });
            });
        }
    }
}
