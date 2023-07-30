using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Net;
using BuildSoft.VRChat.Osc;

namespace HeartRateOnStream_OSC {
    internal class Program {
        public static WebSocketServer server;

        static int port = 4456;

        public static bool Connected = false;
        static int stage = 0;
        static string requestType = "";
        static string requestId = "";

        static int lastHeartrate = 0;
        static int heartrate = 0;

        static void Main(string[] args) {
            server = new WebSocketServer(port);
            server.Start();
            server.AddWebSocketService<HeartrateSocket>("/");

            Console.WriteLine("HeartRateOnStream OSC\n");
            Console.WriteLine("Please use the following connection details in the HeartRateOnStream app:");
            Console.WriteLine("Name of OBS text source: heartrate");
            Console.WriteLine("IP: " + GetLocalIPAddress());
            Console.WriteLine("Port: " + port);
            Console.WriteLine("Password: (leave the box blank)");

            while (true) {
                if (Connected) {
                    OscParameter.SendAvatarParameter("isHRConnected", true);

                    // 'Hello' stage.
                    if (stage == 0) {
                        stage++;
                        server.WebSocketServices["/"].Sessions.Broadcast("{\"op\": 0, \"d\": {\"obsWebSocketVersion\": \"5.1.0\", \"rpcVersion\": 1}}");
                        Console.WriteLine("Hello sent.");
                    }
                    // 'Identify' stage.
                    else if (stage == 2) {
                        stage++;
                        server.WebSocketServices["/"].Sessions.Broadcast("{\"op\": 2,\"d\": { \"negotiatedRpcVersion\": 1}}");
                        Console.WriteLine("Identified, setup complete! Waiting for heart rate data...");
                    }

                    // Respond with a success message to client requests.
                    if (requestId != "") {
                        server.WebSocketServices["/"].Sessions.Broadcast("{\"op\": 7, \"d\": { \"requestType\": " + requestType + ", \"requestId\": " + requestId + ", \"requestStatus\": { \"result\": true, \"code\": 100 }}}");
                        requestId = "";
                    }

                    // When heartrate value has changed, send to OSC.
                    if (lastHeartrate != heartrate) {
                        lastHeartrate = heartrate;

                        OscParameter.SendAvatarParameter("Heartrate", Remap(heartrate, 0, 255, -1, 1));
                        OscParameter.SendAvatarParameter("Heartrate2", Remap(heartrate, 0, 255, 0, 1));
                        OscParameter.SendAvatarParameter("Heartrate3", heartrate);

                        OscParameter.SendAvatarParameter("HR", heartrate);
                        OscParameter.SendAvatarParameter("isHRActive", true);
                    }
                }
                else {
                    OscParameter.SendAvatarParameter("isHRConnected", false);
                    OscParameter.SendAvatarParameter("isHRActive", false);
                }
            }
        }

        /// <summary>
        /// Remap a value to a different range.
        /// </summary>
        /// <param name="value">Value to map.</param>
        /// <param name="leftMin">Original range min.</param>
        /// <param name="leftMax">Original range max.</param>
        /// <param name="rightMin">New range min.</param>
        /// <param name="rightMax">New range max.</param>
        /// <returns></returns>
        public static float Remap(float value, float leftMin, float leftMax, float rightMin, float rightMax)
        {
            return rightMin + (value - leftMin) * (rightMax - rightMin) / (leftMax - leftMin);
        }

        /// <summary>
        /// Web socket behaviour for all incoming messages from HeartRateOnStream.
        /// </summary>
        public class HeartrateSocket : WebSocketBehavior {
            public void SendMessage(string contents) {
                SendMessage(contents);
            }

            protected override void OnOpen() {
                Console.Clear();
                Console.WriteLine("Connection opened.");
                Connected = true;
                stage = 0;
            }

            protected override void OnMessage(MessageEventArgs e) {
                // Identify message, send after we send our 'Hello' message on connect.
                if (e.Data.Contains("eventSubscriptions") && stage == 1) {
                    stage++;
                }
                // Any other message, usually a request.
                else {
                    JObject obj = JObject.Parse(e.Data);

                    // OP Code 6 is a request.
                    if ((int)obj["op"] == 6) {
                        requestId = (string)obj["d"]["requestId"];
                        requestType = (string)obj["d"]["requestType"];

                        // SetInputSettings request, should always be the heartrate.
                        if (requestType == "SetInputSettings") {
                            try {
                                heartrate = Convert.ToInt32((string)obj["d"]["requestData"]["inputSettings"]["text"]);
                                Console.Clear();
                                Console.WriteLine("Latest heart rate: " + heartrate);
                            }
                            catch {
                                // Or maybe it's not always the heart rate.
                                // That's why this is here.
                            }
                        }
                    }
                }
            }

            protected override void OnClose(CloseEventArgs e) {
                Console.WriteLine("Socket closed: " + e.Reason + ", Clean close: " + e.WasClean);
                Connected = false;
                base.OnClose(e);
            }

            protected override void OnError(ErrorEventArgs e) {
                Console.WriteLine("Socket error: " + e.Message);
                Connected = false;
                base.OnError(e);
            }

        }

        /// <summary>
        /// Get the local IPv4 to display in the config information.
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Disconnected - Check your internet connection!";
        }
    }
}
