# CSExec (a c sharp psexec implementation)

Build in Visual Studio (.net 3.5 so it runs on Win 7+). 

This is an example for how to implement `psexec` (from SysInternals Suite) functionality, but in open source C#. This does not implement all of the psexec functionality, but it does implement the equivalent functionality to running: `psexec -s \\target-host cmd.exe`

![screenshot](screen.png)

`psexec` works by doing the following steps:

* copy a windows service executable (`psexecsvc.exe`) that is embedded within the `psexec.exe` binary to `\\target-host\admin$\system32`
* remotely connect to the service control manager on `\\target-host` to install and start the `psexecsvc.exe` service
* connect to the named pipe on the target host: `\\target-host\pipe\psexecsvc`
* send commands to the `psexecsvc` via the named pipe
* receive output via the `psexecsvc` named pipe

This project `csexec` mimicks those steps in native C# with only a minimal amount of `pinvoke` for the remote service installation. It's actually surprisingly simple and takes a very minimal amount of code to implement.
