using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;


namespace BuyEnhancedServerv2.WebSocketController
{
    public class WebSocketController : ControllerBase
    {
        static private bool addState;

        static private WebSocket? webSocket;

        public WebSocketController()
        {
            WebSocketController.addState = false;
        }

        [Route("/add")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                addState = true;

                Thread thread = new(this.listenForDisconnectFromAdd!);
                thread.Start(webSocket);

                while (addState) ;
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private void listenForDisconnectFromAdd(Object aWebSocket)
        {
            WebSocket webSocket = (WebSocket)aWebSocket;

            if (webSocket != null && addState)
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (true)
                {
                    Thread.Sleep(400);
                    if (receiveResult.IsCompleted)
                    {
                        try
                        {
                            if (receiveResult.Result.MessageType == WebSocketMessageType.Close)
                            {
                                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "fin de la connexion", CancellationToken.None).WaitAsync(CancellationToken.None);
                                addState = false;
                                break;
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Impossible de récuperer le message recu");
                        }
                    }
                }
            }
        }

        static public async Task SendToAdd(string message)
        {
            if (addState)
            {
                ArraySegment<byte> text = Encoding.ASCII.GetBytes(message);

                await webSocket!.SendAsync(text, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        static public bool isAddConnected()
        {
            return addState;
        }
    };
}
