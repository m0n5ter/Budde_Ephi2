// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Service.Program
// Assembly: EphiConveyorService, Version=2.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 63301449-2851-4891-B30B-9E7B422DAB27
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiConveyorService.exe

using System.ServiceProcess;

namespace PharmaProject.BusinessLogic.Service
{
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase.Run(new ServiceBase[]
            {
                new BootLoader()
            });
        }
    }
}