using MccDaq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.MccDaq
{
    public class AnalogOutput : Sink<short>
    {
        public AnalogOutput()
        {
            Range = global::MccDaq.Range.NotUsed;
        }

        public int BoardNumber { get; set; }

        public int Channel { get; set; }

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
