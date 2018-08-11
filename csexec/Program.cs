using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;

namespace csexec
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <1)
            {
                PrintUsage();
                return;
            }
            var hostname = args[0];
#if DEBUG
            Console.WriteLine("[*] hostname: {0}", hostname);
#endif

            var version = GetDotNetVersion(hostname);

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
            } catch (UnauthorizedAccessException uae)
            {
                Console.WriteLine(uae.Message);
                return;
            }

            InstallService(hostname, version);
            try
            {
                CSExecClient.Connect(hostname);
            }
            catch (TimeoutException te)
            {
                Console.WriteLine(te.Message);
            }
            UninstallService(hostname);
        }

        static void PrintUsage()
        {
            Console.WriteLine("This is similar to psexec -s \\\\hostname cmd.exe");
            Console.WriteLine("Syntax: ");
            Console.WriteLine("csexec.exe \\\\{hostname}");
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
                    Console.WriteLine("[*] Service Deleted from {0}", hostname);
#endif
                }                
            }
        }
        public static DotNetVersion GetDotNetVersion(string hostname)
        {
            var version = DotNetVersion.net20;
            var path = string.Format("{0}\\admin$\\Microsoft.NET\\Framework64", hostname);
            var directory = new DirectoryInfo(path);
            foreach (var dir in directory.GetDirectories())
            {
                var name = dir.Name.Substring(0, 4);
#if DEBUG
                Console.WriteLine("[*] Found .NET version: {0}", name);
#endif
                switch (name)
                {
                    case "v3.5":
                        version = DotNetVersion.net35;
                        break;
                    case "v4.0":
                        version = DotNetVersion.net40;
                        break;
                    case "v4.5":
                        version = DotNetVersion.net45;
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
