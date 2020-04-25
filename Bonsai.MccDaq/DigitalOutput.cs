using MccDaq;
using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Bonsai.MccDaq
{
    [Description("Writes a byte to a digital I/O port in a Measurement Computing device.")]
    public class DigitalOutput : Sink<short>
    {
        [Description("The board number as defined in the Instacal system config file.")]
        public int BoardNumber { get; set; }

        [Description("The number of the I/O port to write.")]
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
