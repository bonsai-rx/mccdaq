using MccDaq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.MccDaq
{
    public class AnalogInput : Combinator<short>
    {
        public int BoardNumber { get; set; }

        public int Channel { get; set; }

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
