using MccDaq;
using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Bonsai.MccDaq
{
    [Description("Sets the state of a digital I/O port in a Measurement Computing device.")]
    public class DigitalOutput : Sink<short>
    {
        [Description("The board number as defined in the Instacal system config file.")]
        public int BoardNumber { get; set; }

        [Description("The optional specific digital bit to write on the I/O port. If no bit is specified, the entire port state is set.")]
        public int? BitNumber { get; set; }

        [Description("The number of the I/O port to write.")]
        public DigitalPortType PortType { get; set; } = DigitalPortType.AuxPort;

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
                ErrorInfo configError;
                var portType = PortType;
                var bitNumber = BitNumber;
                var board = new MccBoard(BoardNumber);
                if (bitNumber.HasValue) configError = board.DConfigBit(portType, bitNumber.Value, DigitalPortDirection.DigitalOut);
                else configError = board.DConfigPort(portType, DigitalPortDirection.DigitalOut);
                ThrowExceptionForErrorInfo(configError);

                if (bitNumber.HasValue)
                {
                    return source.Do(input =>
                    {
                        var bitValue = input != 0 ? DigitalLogicState.High : DigitalLogicState.Low;
                        var error = board.DBitOut(portType, bitNumber.Value, bitValue);
                        ThrowExceptionForErrorInfo(error);
                    });
                }
                else return source.Do(dataValue =>
                {
                    var error = board.DOut(portType, dataValue);
                    ThrowExceptionForErrorInfo(error);
                });
            });
        }
    }
}
