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
using System.Threading;

namespace HeartRateOnStream_OSC {
    internal class Program {
        public static WebSocketServer server;

        static int port = 4456;

        public static bool Connected = false;
        static string requestType = "";
        static string requestId = "";

        static int lastHeartrate = 0;
        static int heartrate = 0;

        static Queue<string> messagesToSend = new Queue<string>();

        static void Main(string[] args) {
            server = new WebSocketServer(port);
            server.Start();
            server.AddWebSocketService<HeartrateSocket>("/");

            Console.WriteLine("HeartRateOnStream OSC\n");
            Console.WriteLine("Please use the following connection details in the HeartRateOnStream app:");
            Console.WriteLine("IP: " + GetLocalIPAddress());
            Console.WriteLine("Port: " + port + " (You can set this in the Autodiscover box!)");
            Console.WriteLine("Password: (leave the box blank)");

            while (true) {
                while (messagesToSend.Count > 0) {
                    string response = messagesToSend.Dequeue();
                    server.WebSocketServices["/"].Sessions.Broadcast(response);
                }

                if (Connected) {
                    OscParameter.SendAvatarParameter("isHRConnected", true);

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

                Thread.Sleep(100);
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
                Connected = true;

                // Send hello. (op 0)
                string response = "{\"op\": 0, \"d\": {\"obsWebSocketVersion\": \"5.4.2\", \"rpcVersion\": 1}}";
                messagesToSend.Enqueue(response);

                Console.Clear();
                Console.WriteLine("HeartRateOnStream-OSC");
                Console.WriteLine("");
                Console.WriteLine("App connected! Continue to the next page in the app.");
            }

            protected override void OnMessage(MessageEventArgs e) {
                JObject obj = JObject.Parse(e.Data);
                int opCode = (int)obj["op"];

                // Identify
                if (opCode == 1) {
                    string response = "{\"d\": {\"negotiatedRpcVersion\": 1 }, \"op\": 2}";
                    messagesToSend.Enqueue(response);
                }
                // Request.
                else if (opCode == 6) {
                    // OP Code 6 is a request.
                    if ((int)obj["op"] == 6) {
                        requestId = (string)obj["d"]["requestId"];
                        requestType = (string)obj["d"]["requestType"];

                        // SetInputSettings request, should always be the heartrate.
                        if (requestType == "SetInputSettings") {
                            try {
                                string inputName = ((string)obj["d"]["requestData"]["inputName"]);
                                if (inputName == "heartrate") {
                                    heartrate = Convert.ToInt32((string)obj["d"]["requestData"]["inputSettings"]["text"]);
                                    Console.Clear();
                                    Console.WriteLine("HeartRateOnStream-OSC");
                                    Console.WriteLine("");
                                    Console.WriteLine("Latest heart rate: " + heartrate);
                                }
                                else {
                                    string response = "{\"d\": { \"requestId\": \"0dc1d282-84a1-4683-9bdd-1cebc25e4d1b\", \"requestStatus\": { \"code\": 600, \"comment\": \"No source was found by the name of `" + inputName + "`.\", \"result\": false }, \"requestType\": \"SetInputSettings\" }, \"op\": 7}";
                                    messagesToSend.Enqueue(response);
                                }
                            }
                            catch {
                                // Or maybe it's not always the heart rate.
                                // That's why this is here.
                            }
                        }
                        else if (requestType == "GetSceneList") {
                            JObject sceneListResponse = new JObject {
                                ["d"] = new JObject {
                                    ["requestId"] = (string)obj["d"]["requestId"],
                                    ["requestStatus"] = new JObject {
                                        ["code"] = 100,
                                        ["result"] = true,
                                    },
                                    ["requestType"] = "GetSceneList",
                                    ["responseData"] = new JObject {
                                        ["currentPreviewSceneName"] = null,
                                        ["currentPreviewSceneUuid"] = null,
                                        ["currentProgramSceneName"] = "HeartRateOnStream-OSC",
                                        ["currentProgramSceneUuid"] = "755bb54b-c681-44a2-bc0d-da59d534f4af",
                                        ["scenes"] = new JArray {
                                            new JObject {
                                                ["sceneIndex"] = 0,
                                                ["sceneName"] = "HeartRateOnStream-OSC",
                                                ["sceneUuid"] = "88b2711a-f268-4405-a150-a82331db2b48"
                                            }
                                        }
                                    }
                                },
                                ["op"] = 7,
                            };
                            messagesToSend.Enqueue(JsonConvert.SerializeObject(sceneListResponse));
                        }
                        else if (requestType == "GetCurrentProgramScene") {
                            JObject getCurrentProgramResponse = new JObject {
                                ["d"] = new JObject {
                                    ["requestId"] = (string)obj["d"]["requestId"],
                                    ["requestStatus"] = new JObject {
                                        ["code"] = 100,
                                        ["result"] = true,
                                    },
                                    ["requestType"] = "GetCurrentProgramScene",
                                    ["responseData"] = new JObject {
                                        ["currentProgramSceneName"] = "HeartRateOnStream-OSC",
                                        ["currentProgramSceneUuid"] = "755bb54b-c681-44a2-bc0d-da59d534f4af",
                                        ["sceneName"] = "HeartRateOnStream-OSC",
                                        ["sceneUuid"] = "755bb54b-c681-44a2-bc0d-da59d534f4af"
                                    }
                                },
                                ["op"] = 7,
                            };
                            messagesToSend.Enqueue(JsonConvert.SerializeObject(getCurrentProgramResponse));
                        }
                        else if (requestType == "GetSceneItemList") {
                            string response = "{ \"d\": { \"requestId\": \"" + (string)obj["d"]["requestId"] + "\", \"requestStatus\": { \"code\": 100, \"result\": true }, \"requestType\": \"GetSceneItemList\", \"responseData\": { \"sceneItems\": [ { \"inputKind\": \"text_gdiplus_v2\", \"isGroup\": null, \"sceneItemBlendMode\": \"OBS_BLEND_NORMAL\", \"sceneItemEnabled\": true, \"sceneItemId\": 1, \"sceneItemIndex\": 0, \"sceneItemLocked\": false, \"sceneItemTransform\": { \"alignment\": 5, \"boundsAlignment\": 0, \"boundsHeight\": 0.0, \"boundsType\": \"OBS_BOUNDS_NONE\", \"boundsWidth\": 0.0, \"cropBottom\": 0, \"cropLeft\": 0, \"cropRight\": 0, \"cropTop\": 0, \"height\": 256.0, \"positionX\": 0.0, \"positionY\": 0.0, \"rotation\": 0.0, \"scaleX\": 1.0, \"scaleY\": 1.0, \"sourceHeight\": 256.0, \"sourceWidth\": 2.0, \"width\": 2.0 }, \"sourceName\": \"heartrate\", \"sourceType\": \"OBS_SOURCE_TYPE_INPUT\", \"sourceUuid\": \"ab5bb7b9-7ef9-4bc0-b4c5-2d01a47a7ee0\" } ] } }, \"op\": 7}";
                            messagesToSend.Enqueue(response);

                            Console.Clear();
                            Console.WriteLine("HeartRateOnStream-OSC");
                            Console.WriteLine("");
                            Console.WriteLine("Select the 'heartrate' text in the Source selection and select 'As heartrate'!");
                            Console.WriteLine("Then, click 'Finish selection'!");
                            Console.WriteLine("");
                            Console.WriteLine("(It can take 30s~ to update after clicking finish, be patient!)");
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
