using MccDaq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.MccDaq
{
    [Description("Samples a digital I/O port in a Measurement Computing device.")]
    public class SampleDigital : Combinator<int>
    {
        [Description("The board number as defined in the Instacal system config file.")]
        public int BoardNumber { get; set; }

        [Description("The number of the I/O port to read.")]
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
