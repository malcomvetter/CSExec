using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace csexec
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }
            var hostname = args[0];

            var initialCommand = string.Empty;
            var stopAfterInitialCommand = false;

            if (args.Length > 1)
            {
                if (args[1] == "cmd")
                {
                    initialCommand = string.Join(" ", args.Skip(2).ToArray());

                    if (args.Length > 2)
                    {
                        if (args[2] == "/c")
                        {
                            stopAfterInitialCommand = true;
                        }
                    }
                }
            }

#if DEBUG
            Console.WriteLine("[*] hostname: {0}", hostname);
#endif
            var version = GetDotNetVersion(hostname);
            CopyServiceExe(version, hostname);
            InstallService(hostname, version);
            try
            {
                CSExecClient.Connect(hostname, initialCommand, stopAfterInitialCommand);
            }
            catch (TimeoutException te)
            {
                Console.WriteLine(te.Message);
            }
            UninstallService(hostname);
            DeleteServiceExe(hostname);
        }

        static void PrintUsage()
        {
            Console.WriteLine("This is similar to psexec -s \\\\hostname cmd.exe");
            Console.WriteLine("Syntax: ");
            Console.WriteLine("csexec.exe \\\\{hostname} [cmd [/c] [<string>]]");
        }

        private static void CopyServiceExe(DotNetVersion version, string hostname)
        {
            byte[] svcexe = new byte[0];
            if (version == DotNetVersion.net35)
            {
                svcexe = Properties.Resources.csexecsvc_net35;
            }
            if (version == DotNetVersion.net40)
            {
                svcexe = Properties.Resources.csexecsvc_net40;
            }
            if (version == DotNetVersion.net45)
            {
                svcexe = Properties.Resources.csexecsvc_net45;
            }
            var path = hostname + @"\admin$\system32\csexecsvc.exe";
            try
            {
                File.WriteAllBytes(path, svcexe);
#if DEBUG
                Console.WriteLine("[*] Copied {0} service executable to {1}", version, hostname);
#endif
            }
            catch (UnauthorizedAccessException uae)
            {
                Console.WriteLine(uae.Message);
                return;
            }
        }

        private static void DeleteServiceExe(string hostname)
        {
            var path = hostname + @"\admin$\system32\csexecsvc.exe";
            var max = 5;
            for (int i = 0; i < max; i++)
            {
                try
                {
                    Thread.Sleep(1000);
                    File.Delete(path);
                    i = max;
#if DEBUG
                    Console.WriteLine("[*] Deleted service executable from {0}", hostname);
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }
        }

        static void InstallService(string hostname, DotNetVersion version)
        {
            try
            {
                UninstallService(hostname);
            }
            catch (Exception) { }
            using (var scmHandle = NativeMethods.OpenSCManager(hostname, null, NativeMethods.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE))
            {
                if (scmHandle.IsInvalid)
                {
                    throw new Win32Exception();
                }

                using (
                    var serviceHandle = NativeMethods.CreateService(
                        scmHandle,
                        GlobalVars.ServiceName,
                        GlobalVars.ServiceDisplayName,
                        NativeMethods.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                        NativeMethods.SERVICE_TYPES.SERVICE_WIN32_OWN_PROCESS,
                        NativeMethods.SERVICE_START_TYPES.SERVICE_AUTO_START,
                        NativeMethods.SERVICE_ERROR_CONTROL.SERVICE_ERROR_NORMAL,
                        GlobalVars.ServiceEXE,
                        null,
                        IntPtr.Zero,
                        null,
                        null,
                        null))
                {
                    if (serviceHandle.IsInvalid)
                    {
                        throw new Win32Exception();
                    }
#if DEBUG
                    Console.WriteLine("[*] Installed {0} Service on {1}", version, hostname);
#endif
                    NativeMethods.StartService(serviceHandle, 0, null);
#if DEBUG
                    Console.WriteLine("[*] Service Started on {0}", hostname);
#endif
                }
            }
        }
        static void UninstallService(string hostname)
        {
            using (var scmHandle = NativeMethods.OpenSCManager(hostname, null, NativeMethods.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE))
            {
                if (scmHandle.IsInvalid)
                {
                    throw new Win32Exception();
                }

                using (var serviceHandle = NativeMethods.OpenService(scmHandle, GlobalVars.ServiceName, NativeMethods.SERVICE_ACCESS.SERVICE_ALL_ACCESS))
                {
                    if (serviceHandle.IsInvalid)
                    {
                        throw new Win32Exception();
                    }
                    
                    NativeMethods.DeleteService(serviceHandle);
#if DEBUG
                    Console.WriteLine("[*] Service Uninstalled from {0}", hostname);
#endif
                }                
            }
        }
        public static DotNetVersion GetDotNetVersion(string hostname)
        {
            var version = DotNetVersion.net20;
            var path1 = string.Format("{0}\\admin$\\Microsoft.NET\\Framework64", hostname);
            var path2 = string.Format("{0}\\admin$\\Microsoft.NET\\Framework", hostname);
            DirectoryInfo[] directories;
            try
            {
                var directory = new DirectoryInfo(path1);
                directories = directory.GetDirectories();
            }
            catch (IOException)
            {
                var directory = new DirectoryInfo(path2);
                directories = directory.GetDirectories();
            }
            foreach (var dir in directories)
            {
                var name = dir.Name.Substring(0, 4);
#if DEBUG
                Console.WriteLine("[*] Found .NET version: {0}", name);
#endif
                switch (name)
                {
                    case "v4.5":
                        version = DotNetVersion.net45;
                        break;
                    case "v4.0":
                        version = DotNetVersion.net40;
                        break;
                    case "v3.5":
                        version = DotNetVersion.net35;
                        break;
                    default:
                        continue;
                }
            }
#if DEBUG
            Console.WriteLine("[*] Choosing {0}", version);
#endif
            return version;
        }
        public enum DotNetVersion
        {
            net20,
            net35,
            net40,
            net45
        }
    }
}
