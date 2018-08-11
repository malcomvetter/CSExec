# CSExec (a c sharp psexec implementation)

Build in Visual Studio as .NET 4.0 project (or downgrade to .NET 3.5 so the client executable runs on Win 7). 

This is an example for how to implement `psexec` (from SysInternals Suite) functionality, but in open source C#. This does not implement all of the psexec functionality, but it does implement the equivalent functionality to running: `psexec -s \\target-host cmd.exe`

![screenshot](screen.png)

`psexec` works by doing the following steps:

* copy a windows service executable (`psexecsvc.exe`) that is embedded within the `psexec.exe` binary to `\\target-host\admin$\system32`
* remotely connect to the service control manager on `\\target-host` to install and start the `psexecsvc.exe` service
* connect to the named pipe on the target host: `\\target-host\pipe\psexecsvc`
* send commands to the `psexecsvc` via the named pipe
* receive output via the `psexecsvc` named pipe

This project `csexec` mimicks those steps in native C# with only a minimal amount of `pinvoke` for the remote service installation. It's actually surprisingly simple and takes a very minimal amount of code to implement.

The primary difference between this and `psexec` is that it must determine the .NET runtime on the remote host in order to install the correctly compiled service executable.
