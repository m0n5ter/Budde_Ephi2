// Decompiled with JetBrains decompiler
// Type: PharmaProject.Program
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System.Threading;
using PharmaProject.Locations;
using PharmaProject.Segments;
using PharmaProject.UTC;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var störvektor = new byte[100];
            WmsCommunicator.Start();
            var pbl = new PBL();
            WmsCommunicator.OnReceived += message =>
            {
                if (message == null)
                    return;
                if (message.functionCode == 101U)
                    WmsCommunicator.Send(BaseMessage.MessageToByteArray(new Störvektor(störvektor)));
                else
                    BaseLocation.FindLocation(message.location)?.ProcessRxMessage(message);
            };
            NdwConnectCommunicator.MqttClientSingleton();
            CSD_UTC.UtcServerConnect(503, "172.16.0.1");
            while (true)
            {
                Thread.Sleep(1000);
                CSD.RunWatchdog();
            }
        }
    }
}