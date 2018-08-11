using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace csexec
{
    public static class CSExecClient
    {
        public static void Connect(string hostname)
        {
            using (var pipe = new NamedPipeClientStream(hostname, GlobalVars.ServiceName, PipeDirection.InOut))
            {
                pipe.Connect(5000);
                pipe.ReadMode = PipeTransmissionMode.Message;
                do
                {
                    Console.Write("{0}> ", GlobalVars.ClientName);
                    var input = Console.ReadLine();
                    if (String.IsNullOrEmpty(input)) continue;
                    byte[] bytes = Encoding.Default.GetBytes(input);
                    pipe.Write(bytes, 0, bytes.Length);
                    if (input.ToLower() == "exit") return;
                    var result = ReadMessage(pipe);
                    Console.WriteLine(Encoding.UTF8.GetString(result));
                    Console.WriteLine();
                } while (true);
            }
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
