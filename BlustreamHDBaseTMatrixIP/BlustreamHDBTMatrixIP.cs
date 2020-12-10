namespace AVSwitcherBlustreamHDBTMatrixIP
{
    using Crestron.RAD.Common.BasicDriver;
    using Crestron.RAD.Common.Interfaces;
    using Crestron.RAD.Common.Transports;
    using Crestron.RAD.DeviceTypes.AudioVideoSwitcher;

    using Crestron.SimplSharp;

    using System;

    public class BlustreamHDBTMatrixIP : AAudioVideoSwitcher, ITcp
    {
        public BlustreamHDBTMatrixIP()
        {
        }

        public void Initialize(IPAddress ipAddress, int port)
        {
            TcpTransport tcpTransport = new TcpTransport()
            {
                EnableAutoReconnect = base.EnableAutoReconnect,
                EnableLogging = this.InternalEnableLogging,
                CustomLogger = this.InternalCustomLogger,
                EnableRxDebug = this.InternalEnableRxDebug,
                EnableTxDebug = this.InternalEnableTxDebug
            };

            TcpTransport tcpTransport1 = tcpTransport;
            tcpTransport1.Initialize(ipAddress, port);
            this.ConnectionTransport = tcpTransport1;

            BlustreamHDBTMatrixProtocol blustreamHDBTMatrixProtocol = new BlustreamHDBTMatrixProtocol(this.ConnectionTransport, base.Id)
            {
                EnableLogging = this.InternalEnableLogging,
                CustomLogger = this.InternalCustomLogger
            };

            base.AudioVideoSwitcherProtocol = blustreamHDBTMatrixProtocol;
            BlustreamHDBTMatrixIP blustreamHDBTMatrix = this;
            base.AudioVideoSwitcherProtocol.RxOut += new RxOut(blustreamHDBTMatrix.SendRxOut);
            base.AudioVideoSwitcherProtocol.Initialize(base.AudioVideoSwitcherData);
        }
    }
}
