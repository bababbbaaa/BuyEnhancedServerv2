// See https://aka.ms/new-console-template for more information
using AngleSharp.Io;
using AngleSharp.Text;
using BuyEnhancedServer;
using BuyEnhancedServer.Binance;
using BuyEnhancedServer.Bybit;
using BuyEnhancedServer.Proxies;
using BuyEnhancedServerv2.API;
using BuyEnhancedServerv2.Utils;
using BuyEnhancedServerv2.WebSocketController;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;


//________________________COMMENT FAIRE UNE GET REQUEST A L'API V3________________________

/*
HttpClient client = new HttpClient();

client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

string url = "https://api-testnet.bybit.com/";

string fonction = "public/linear/recent-trading-records";
string fonction = "private/linear/order/create";
string fonction = "v2/public/time";
string param = "";

//GET
//var responseString = await client.GetStringAsync(url+fonction+param);
//Console.WriteLine("Serveur : "+responseString);


//api_key = "Qo0t1u3J6qIZFTqBNJ",
//secret = "5HDA2RMnObRyfkSbhD3WJCwGqipP8muxwGwE"
*/


//________________________RECUPERER LES PROXIES________________________

/*ProxyList list = ProxyList.Instance();

AddNewValidProxiesThread addThread = new AddNewValidProxiesThread(ref list);

addThread.getNewProxiesFromGeonode();

addThread.getNewProxiesFromFreeProxy();

addThread.getNewProxiesFromProxyScrape();


Console.WriteLine("main");
WebProxy proxy = null;
addThread.isGoodProxy(proxy);

addThread.Run();*/


//________________________TEST D'ENREGISTREMENT DANS UN FICHIER________________________

/*
//cr�ation d'une liste
ProxyList proxyList = ProxyList.Instance();

proxyList.add(new WebProxy("172.16.1.1", 100)); 
proxyList.add(new WebProxy("172.16.1.2", 200));
proxyList.add(new WebProxy("172.16.1.3", 300));
proxyList.add(new WebProxy("172.16.1.4", 400));

proxyList.Save();

for(int i = 0; i < 4; i++)
{
    proxyList.remove(0);
}

Console.WriteLine("Liste : ");
proxyList.afficher();
Console.WriteLine("____________________");

Console.WriteLine("Load...");
Console.WriteLine("Liste : ");
proxyList.Load();
proxyList.afficher();*/


//________________________TEST ADDNEWVALIDPROXIESTHREAD________________________


/*ProxyList list = ProxyList.Instance();

AddNewValidProxiesThread addThread = new AddNewValidProxiesThread(ref list);

await addThread.Run();*/


//________________________TEST REMOVEINVALIDPROXIESTHREAD________________________


/*ProxyList list = ProxyList.Instance();

RemoveInvalidProxiesThread removeThread = new RemoveInvalidProxiesThread(ref list);

await removeThread.Run();*/

//________________________TEST UPDATE PROXY LIST________________________

/*
ProxyList list = ProxyList.Instance();

AddNewValidProxiesThread add = new AddNewValidProxiesThread(ref list);

RemoveInvalidProxiesThread remove = new RemoveInvalidProxiesThread(ref list);

var removeThread = new Thread(remove.Run);
removeThread.Start();

var addThread = new Thread(add.Run);
addThread.Start();
*/

//________________________TESTER LA CONNEXION INTERNET________________________
/*
var ping = new Ping(); 
var reply = ping.Send(new IPAddress(new byte[] { 8, 8, 8, 8 }));

bool isConnected = (reply.Status == IPStatus.Success);

Console.WriteLine(isConnected);*/



//________________________COMMENT FAIRE POUR PRENDRE UN TRADE________________________

/*
Console.WriteLine("Ouverture � la hausse et fermeture 2 secondes apr�s attendu");

BybitManager tradeManager = new BybitManager("SpF1bwKBaAWuEWbkxX", "d39sjQb4coXGnQ2yg43SGUWIIQ7NQ6eglrWj");

Position pos = new Position("BTCUSDT", (float)21879.50, "updateTimeStamp", "0,05");

tradeManager.takePosition(pos, 0.00004);

Thread.Sleep(2000);

tradeManager.ClosePosition(pos);





Console.WriteLine("Attente de 5 secondes");

Thread.Sleep(5000);



Console.WriteLine("Ouverture � la baisse et fermeture 2 secondes apr�s attendu");

Position pos1 = new Position("BTCUSDT", (float)21879.50, "updateTimeStamp", "-0,05");

tradeManager.takePosition(pos1, 0.05);

Thread.Sleep(2000);

tradeManager.ClosePosition(pos1);
*/


//________________________TEST DES FONCTIONS UTILITAIRES DE TRADERMANAGER________________________

/*
BybitManager tradeManager = new BybitManager("SpF1bwKBaAWuEWbkxX", "d39sjQb4coXGnQ2yg43SGUWIIQ7NQ6eglrWj");

Console.WriteLine(tradeManager.GetBalanceUSD());

//attention elle est priv�e
//tradeManager.switchToHedgeMode();

//attention elle est priv�e
double minimum;
double step;
string symbole = "PHBUSDT";
tradeManager.GetSymbolInfo(symbole, out minimum,out step);

Console.WriteLine("Symbole :" + symbole);
Console.WriteLine("Minimum :" + minimum.ToString(CultureInfo.InvariantCulture));
Console.WriteLine("Step :" + step.ToString(CultureInfo.InvariantCulture));

//attention elle est priv�e
//Console.WriteLine(tradeManager.GetPositionSize(new Position("BTCUSDT",2555.0f,"1000","0,05")));
*/

//________________________RECUPERER LES DONNEES DES TRADERS________________________


//Trader trader = new Trader("B76D80355A416EDD30F1C3F8E2F051C7");

/*
List<Position> liste = trader.GetPositions();

foreach(Position pos in liste)
{
    Console.WriteLine(pos.ToString());
}
*/

//Console.WriteLine(trader.PrepareSize(0.0599999999999999999,"BTCUSDT"));


//________________________SIZE LIST________________________

/*
SizeList sizeList = new SizeList("LesTaillesDuTraderDeTest");


//sizeList.add(new SizeFormat { size = 300, timestamp = 1 });
//sizeList.add(new SizeFormat { size = 500, timestamp = 2 });
//sizeList.add(new SizeFormat { size = 100, timestamp = 3});
//sizeList.add(new SizeFormat { size = 400, timestamp = 4 });
//sizeList.add(new SizeFormat { size = 200, timestamp = 5 });
//sizeList.add(new SizeFormat { size = 400, timestamp = 6 });

sizeList.Save();

Console.WriteLine(sizeList.calculateMedian());

*/

//________________________INVALID SYMBOL LIST________________________

/*
InvalidSymbolList invalidSymbolList = InvalidSymbolList.Instance();


invalidSymbolList.add("ABC");
invalidSymbolList.add("DEF");
invalidSymbolList.add("GHI");
invalidSymbolList.add("JKL");
invalidSymbolList.add("MNO");
invalidSymbolList.add("PQR");

invalidSymbolList.afficher();
*/


//________________________Mise en place de fakePositions________________________
/*
ProxyList proxyList = ProxyList.Instance();


Trader trader = new Trader("Test", ref proxyList);

//trader.SetPositions();

foreach (Position position in trader.GetFakePositions())
{
    position.display();
}

 */

//___________________________________LOG______________________________________

/*
Log.TraceError("fonction","message");
Log.TraceInformation("fonction", "message");
Log.TraceWarning("fonction", "message");
*/

//___________________________________TEST PROXY RESTSHARP______________________________________
/*
RestClient restClient = new RestSharp.RestClient("http://www.binance.com/");
var request = new RestRequest("/bapi/futures/v1/public/future/leaderboard/getOtherPosition");

restClient.Options.Proxy = null;

var encryptedUid = "EE4F67875522537CAF940E5BDA96AF2C";

request.AddJsonBody(new
{
    encryptedUid,
    tradeType = "PERPETUAL"
});

var response = restClient.Post(request);

Console.WriteLine(response.Content);
*/

//___________________________________TEST PROXY HTTP HANDLER    ______________________________________
/*
var proxylist = ProxyList.Instance();

RemoveInvalidProxiesThread remove = new RemoveInvalidProxiesThread(ref proxylist);

//isGoodProxy utilise un HTTP handler 
remove.isGoodProxy(null);*/

//___________________________________TEST verifyEncryptedUid de Trader______________________________________

//Console.WriteLine(Trader.verifyEncryptedUid("3AFFCB67ED4F1D1D8437BA17F4E8E5ED"));

//___________________________________TEST verifyAuthentificationInformations de BybitManager______________________________________

//Console.WriteLine(BybitManager.verifyAuthentificationInformations("SpF1bwKBaAWuEWbkxX", "d39sjQb4coXGnQ2yg43SGUWIIQ7NQ6eglrWj"));

//___________________________________TEST closeAllPositions de BybitManager______________________________________
/*
BybitManager manager = new("QVFHFBNTZMVTCKPDWX", "EJPBUSLHKXTHCJGYVYHWGBVCPNAWWCTDCONB");

manager.closeAllPositions();
*/
//___________________________________TRADE______________________________________
/*
namespace BuyEnhancedServer
{
    class Program
    {
        static void Main()
        {
            ProxyList list = ProxyList.Instance();

            AddNewValidProxiesThread add = AddNewValidProxiesThread.Instance(ref list);

            RemoveInvalidProxiesThread remove = RemoveInvalidProxiesThread.Instance(ref list);

            string api_key = "QVFHFBNTZMVTCKPDWX";
            string api_secret = "EJPBUSLHKXTHCJGYVYHWGBVCPNAWWCTDCONB";

            Trader trader = new("EE4F67875522537CAF940E5BDA96AF2C", ref list, api_key, api_secret);

            remove.Start();

            add.Start();
            
            while (list.count() < 5)
            {

            }

            trader.Start();

            return;
        }
    }
}

*/
//___________________________________PROXY______________________________________
/*
ProxyList list = ProxyList.Instance();

AddNewValidProxiesThread add = AddNewValidProxiesThread.Instance(ref list);
RemoveInvalidProxiesThread remove = RemoveInvalidProxiesThread.Instance(ref list);

remove.Start();
add.Start();
*/
//___________________________________API______________________________________
/*
const string MyAllowSpecificOrigins = "AllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*");
                      });
});

var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);

API server = API.Instance();

app.MapGet("/", () => "Hello World!");

app.MapGet("/proxyCount", server.getProxyCount);

app.MapGet("/isRemoveActiv", server.isRemoveActiv);

app.MapGet("/startRemove", server.startRemove);

app.MapGet("/stopRemove", server.stopRemove);

app.MapGet("/isAddActiv", server.isAddActiv);

app.MapGet("/startAdd", server.startAdd);

app.MapGet("/stopAdd", server.stopAdd);

app.MapGet("/getSubscriptionList", server.getSubscriptionList);

app.MapPost("/areValidInformations", server.areValidInformations);

app.MapPost("/addTrader", server.addTrader);

app.MapPost("/launchSubscription", server.launchSubscription);

app.MapPost("/brutalStop", server.brutalStop);

app.MapPost("/softStop", server.softStop);

app.MapPost("/deleteTrader", server.deleteTrader);

app.Run();
*/

//___________________________________WS______________________________________
/*
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

// <snippet_UseWebSockets>
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};

app.UseWebSockets(webSocketOptions);

app.MapControllers();

Thread thread = new Thread(app.Run);

thread.Start();

while (true)
{
    await WebSocketController.SendToAdd("This is the add message");
    await WebSocketController.SendToRemove("This is the remove message");
    await WebSocketController.SendToTrader("This is the trader message");

    Thread.Sleep(1000);
}*/

//___________________________________API et WS et Serveur Web______________________________________

const string MyAllowSpecificOrigins = "AllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*");
                      });
});

var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

API server = API.Instance();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};

app.UseWebSockets(webSocketOptions);


app.MapControllers();

app.MapGet("/proxyCount", server.getProxyCount);

app.MapGet("/isRemoveActiv", server.isRemoveActiv);

app.MapGet("/startRemove", server.startRemove);

app.MapGet("/stopRemove", server.stopRemove);

app.MapGet("/isAddActiv", server.isAddActiv);

app.MapGet("/startAdd", server.startAdd);

app.MapGet("/stopAdd", server.stopAdd);

app.MapGet("/getSubscriptionList", server.getSubscriptionList);

app.MapPost("/areValidInformations", server.areValidInformations);

app.MapPost("/addTrader", server.addTrader);

app.MapPost("/launchSubscription", server.launchSubscription);

app.MapPost("/brutalStop", server.brutalStop);

app.MapPost("/softStop", server.softStop);

app.MapPost("/deleteTrader", server.deleteTrader);

app.MapPost("/openTraderSocket", server.openTraderSocket);

app.MapGet("/closeTraderSocket", server.closeTraderSocket);

app.MapGet("/getRemovePourcentage", server.getRemovePourcentage);


Thread thread = new Thread(app.Run);

thread.Start();