// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Service.BootLoader
// Assembly: EphiConveyorService, Version=2.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 63301449-2851-4891-B30B-9E7B422DAB27
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiConveyorService.exe

using System;
using System.ComponentModel;
using System.ServiceProcess;
using log4net;
using log4net.Config;

namespace PharmaProject.BusinessLogic.Service
{
    public class BootLoader : ServiceBase
    {
        private readonly ILog log;
        private IContainer components;
        private readonly Entry pbl;

        public BootLoader()
        {
            InitializeComponent();
            XmlConfigurator.Configure();
            log = LogManager.GetLogger("Service");
            log.Info("Service started");
            try
            {
                pbl = new Entry();
            }
            catch (Exception ex)
            {
                log.Error("Error during instantiation", ex);
                throw;
            }
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Starting driver...");
            pbl.Start();
            log.Info("Driver started");
        }

        protected override void OnStop()
        {
            log.Info("Stopping driver...");
            pbl.Stop();
            log.Info("Driver stopped");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            ServiceName = "Service1";
        }
    }
}