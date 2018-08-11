using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace csexec
{
    [RunInstaller(true)]
    public class CSExecSvcInstaller : Installer
    {
        public CSExecSvcInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();
            serviceInstaller.DisplayName = GlobalVars.ServiceDisplayName;
            serviceInstaller.Description = GlobalVars.ServiceDescription;
            serviceInstaller.ServiceName = GlobalVars.ServiceName;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            processInstaller.Account = ServiceAccount.LocalSystem;
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
            serviceInstaller.AfterInstall += ServiceInstaller_AfterInstall;
        }

        void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            foreach (var svc in ServiceController.GetServices())
            {
                if (svc.ServiceName == GlobalVars.ServiceName)
                {
                    Console.WriteLine("[*] Found service installed: {0}", svc.DisplayName);
                    Console.WriteLine("[*] Starting service ...");
                    svc.Start();
                }
            }
        }
    }
}
