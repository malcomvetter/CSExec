using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace csexec
{
    public static class CSExecClient
    {
        public static void Connect(string hostname, string initialCommand, bool stopAfterInitialCommand)
        {
            using (var pipe = new NamedPipeClientStream(hostname, GlobalVars.ServiceName, PipeDirection.InOut))
            {
                pipe.Connect(5000);
                pipe.ReadMode = PipeTransmissionMode.Message;

                ExecuteCommand(pipe, initialCommand);

                if (stopAfterInitialCommand)
                    return;

                do
                {
                    Console.Write("{0}> ", GlobalVars.ClientName);

                    var input = Console.ReadLine();

                    if (input.ToLower() == "exit")
                        return;
                    else
                        ExecuteCommand(pipe, input);
                } while (true);
            }
        }

        private static void ExecuteCommand(NamedPipeClientStream pipe, string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            byte[] bytes = Encoding.Default.GetBytes(command);
            pipe.Write(bytes, 0, bytes.Length);
            pipe.Flush();

            var result = ReadMessage(pipe);

            Console.WriteLine(Encoding.UTF8.GetString(result));
            Console.WriteLine();
        }

        private static byte[] ReadMessage(PipeStream pipe)
        {
            byte[] buffer = new byte[1024];
            using (var ms = new MemoryStream())
            {
                do
                {
                    var readBytes = pipe.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, readBytes);
                }
                while (!pipe.IsMessageComplete);

                return ms.ToArray();
            }
        }
    }
}
