using MccDaq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.MccDaq
{
    public class DigitalOutput : Sink<short>
    {
        public int BoardNumber { get; set; }

        public DigitalPortType PortType { get; set; } = DigitalPortType.AuxPort;

        public override IObservable<short> Process(IObservable<short> source)
        {
            return Observable.Defer(() =>
            {
                var portType = PortType;
                var board = new MccBoard(BoardNumber);
                var configError = board.DConfigPort(portType, DigitalPortDirection.DigitalOut);
                if (configError.Value != ErrorInfo.ErrorCode.NoErrors)
                {
                    throw new InvalidOperationException(configError.Message);
                }

                return source.Do(dataValue =>
                {
                    var error = board.DOut(portType, dataValue);
                    if (error.Value != ErrorInfo.ErrorCode.NoErrors)
                    {
                        throw new InvalidOperationException(error.Message);
                    }
                });
            });
        }
    }
}
