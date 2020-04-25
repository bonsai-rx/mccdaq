using MccDaq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.MccDaq
{
    public class SampleDigital : Combinator<int>
    {
        public int BoardNumber { get; set; }

        public DigitalPortType PortType { get; set; } = DigitalPortType.AuxPort;

        public override IObservable<int> Process<TSource>(IObservable<TSource> source)
        {
            return Observable.Defer(() =>
            {
                var portType = PortType;
                var board = new MccBoard(BoardNumber);
                var configError = board.DConfigPort(portType, DigitalPortDirection.DigitalIn);
                if (configError.Value != ErrorInfo.ErrorCode.NoErrors)
                {
                    throw new InvalidOperationException(configError.Message);
                }

                return source.Select(input =>
                {
                    var error = board.DIn(portType, out short dataValue);
                    if (error.Value != ErrorInfo.ErrorCode.NoErrors)
                    {
                        throw new InvalidOperationException(error.Message);
                    }
                    return (int)dataValue;
                });
            });
        }
    }
}
