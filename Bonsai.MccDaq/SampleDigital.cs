using MccDaq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.MccDaq
{
    [Description("Samples a digital I/O port in a Measurement Computing device.")]
    public class SampleDigital : Combinator<short>
    {
        [Description("The board number as defined in the Instacal system config file.")]
        public int BoardNumber { get; set; }

        [Description("The optional specific digital bit to read from the I/O port. If no bit is specified, the entire port state is sampled.")]
        public int? BitNumber { get; set; }

        [Description("The number of the I/O port to read.")]
        public DigitalPortType PortType { get; set; } = DigitalPortType.AuxPort;

        static void ThrowExceptionForErrorInfo(ErrorInfo error)
        {
            if (error.Value != ErrorInfo.ErrorCode.NoErrors)
            {
                throw new InvalidOperationException(error.Message);
            }
        }

        public override IObservable<short> Process<TSource>(IObservable<TSource> source)
        {
            return Observable.Defer(() =>
            {
                ErrorInfo configError;
                var portType = PortType;
                var bitNumber = BitNumber;
                var board = new MccBoard(BoardNumber);
                if (bitNumber.HasValue) configError = board.DConfigBit(portType, bitNumber.Value, DigitalPortDirection.DigitalIn);
                else configError = board.DConfigPort(portType, DigitalPortDirection.DigitalIn);
                ThrowExceptionForErrorInfo(configError);

                if (bitNumber.HasValue)
                {
                    return source.Select(input =>
                    {
                        var error = board.DBitIn(portType, bitNumber.Value, out DigitalLogicState bitValue);
                        ThrowExceptionForErrorInfo(error);
                        return (short)bitValue;
                    });
                }
                else return source.Select(input =>
                {
                    var error = board.DIn(portType, out short dataValue);
                    ThrowExceptionForErrorInfo(error);
                    return dataValue;
                });
            });
        }
    }
}
