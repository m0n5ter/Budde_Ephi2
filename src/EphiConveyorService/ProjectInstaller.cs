// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Service.ProjectInstaller
// Assembly: EphiConveyorService, Version=2.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 63301449-2851-4891-B30B-9E7B422DAB27
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiConveyorService.exe

using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace PharmaProject.BusinessLogic.Service
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private IContainer components;
        private ServiceInstaller PharmaAServicenstaller;
        private ServiceProcessInstaller serviceProcessInstaller;

        public ProjectInstaller()
        {
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            serviceProcessInstaller = new ServiceProcessInstaller();
            PharmaAServicenstaller = new ServiceInstaller();
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Password = null;
            serviceProcessInstaller.Username = null;
            PharmaAServicenstaller.Description = "Driver for the Ephi conveyor system";
            PharmaAServicenstaller.DisplayName = "Ephi SK Pharma conveyor driver";
            PharmaAServicenstaller.ServiceName = "PharmaConveyorDriver";
            PharmaAServicenstaller.StartType = ServiceStartMode.Automatic;
            Installers.AddRange(new Installer[2]
            {
                serviceProcessInstaller,
                PharmaAServicenstaller
            });
        }
    }
}