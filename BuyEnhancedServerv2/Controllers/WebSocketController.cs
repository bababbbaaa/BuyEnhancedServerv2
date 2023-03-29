using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;


namespace BuyEnhancedServerv2.WebSocketController
{
    public class WebSocketController : ControllerBase
    {
        static private bool addState;
        static private bool removeState;
        static private bool traderState;

        static private WebSocket? webSocket;

        public WebSocketController()
        {
            addState = false;
            removeState = false;
            traderState = false;
        }

        [Route("/add")]
        public async Task Add()
        {
            if(webSocket != null)
            {
                if (webSocket.State == WebSocketState.Closed)
                {
                    addState = false;
                    removeState = false;
                    traderState = false;
                }
            }

            if(!isWebSocketAvailable())
            {
                HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            }
            else if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                addState = true;

                webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                Thread thread = new(this.listenForDisconnectFromSocket!);
                thread.Start();

                while (addState) ;
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [Route("/remove")]
        public async Task Remove()
        {
            if (webSocket != null)
            {
                if (webSocket.State == WebSocketState.Closed)
                {
                    addState = false;
                    removeState = false;
                    traderState = false;
                }
            }

            if (!isWebSocketAvailable())
            {
                HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            }
            else if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                removeState = true;

                webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                Thread thread = new(this.listenForDisconnectFromSocket!);
                thread.Start();

                while (removeState) ;
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [Route("/trader")]
        public async Task Trader()
        {
            if (webSocket != null)
            {
                if (webSocket.State == WebSocketState.Closed)
                {
                    addState = false;
                    removeState = false;
                    traderState = false;
                }
            }

            if (!isWebSocketAvailable())
            {
                HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            }
            else if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                traderState = true;

                webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                Thread thread = new(this.listenForDisconnectFromSocket!);
                thread.Start();

                while (traderState) ;
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private void listenForDisconnectFromSocket()
        {

            if (webSocket != null && !isWebSocketAvailable())
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
                                removeState = false;
                                traderState = false;
                                break;
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Erreur lors de la récupération du message ou conflit");
                            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "fin de la connexion", CancellationToken.None).WaitAsync(CancellationToken.None);
                            addState = false;
                            removeState = false;
                            break;
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

        static public async Task SendToRemove(string message)
        {
            if (removeState)
            {
                ArraySegment<byte> text = Encoding.ASCII.GetBytes(message);

                await webSocket!.SendAsync(text, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        static public async Task SendToTrader(string message)
        {
            if (traderState)
            {
                ArraySegment<byte> text = Encoding.ASCII.GetBytes(message);

                await webSocket!.SendAsync(text, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        static public bool isWebSocketAvailable()
        {
            return !(addState || removeState || traderState);
        }
    };
}
