using System;
using System.IO;
using System.Net;
using System.Text;

namespace PlayMore35.Global {
    public class RollServer {
        private readonly HttpListener Http;

        public RollServer() {
            Http = new HttpListener();
            Http.Prefixes.Add("http://localhost:2035/");
            Http.Start();
        }

        public void Roll(string dice, string description) {
            Http.BeginGetContext(new AsyncCallback(SendRoll(dice, description)), Http);
        }

        public static Action<IAsyncResult> SendRoll(string dice, string description) {
            return (IAsyncResult result) => {
                HttpListener listener = (HttpListener)result.AsyncState;
                HttpListenerContext context = listener.EndGetContext(result);
                HttpListenerResponse response = context.Response;
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.ContentType = "application/json";
                string responseString = $"{{\"dice\": \"{dice}\", \"description\": \"{description}\"}}";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            };
        }
    }
}
