using BuyEnhancedServer.Binance;
using BuyEnhancedServer.Bybit;
using BuyEnhancedServer.Proxies;
using BuyEnhancedServerv2.Utils;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace BuyEnhancedServerv2.Service
{
    public class Server
    {
        private static Server? server;
        ProxyList list;
        AddNewValidProxiesThread add;
        RemoveInvalidProxiesThread remove;

        /*
        *    Nom : Instance
        *    Role : Créer l'unique objet statique de type Server
        *    Fiabilité : Sure
        */
        public static Server Instance()
        {
            Log.TraceInformation("Server.Instance", "Appel");

            if (Server.server == null)
            {
                Server.server = new Server();
            }

            return Server.server;
        }

        private Server() 
        {
            this.list = ProxyList.Instance();

            this.add = AddNewValidProxiesThread.Instance(ref list);
            this.remove = RemoveInvalidProxiesThread.Instance(ref list);

            //je pense stocker dans cet objet les souscriptions dans un dictionnaire 
            //Dictionary<string, Trader> subscrition;
        }
        public string getProxyCount()
        {
            string ret;

            try
            {
                byte[] result = JsonSerializer.SerializeToUtf8Bytes(new {proxyCount = this.list.count() });

                ret = JsonSerializer.Serialize(new {retCode = 0, result = result});
            }
            catch (Exception ex)
            {
                ret = JsonSerializer.Serialize(new { retCode = 1, result = ex.Message });
            }

            return ret;
        }

        public bool getRemoveState()
        {
            return this.remove.isActiv();
        }

        public bool isRemoveActiv()
        {
            return this.remove.isActiv();
        }

        public void startRemove()
        {
            this.remove.Start();
        }

        public void stopRemove()
        {
            this.remove.Stop();
        }

        public void isAddActiv()
        {
            this.remove.Stop();
        }

        public void startAdd()
        {
            this.add.Start();
        }

        public void stopAdd()
        {
            this.add.Stop();
        }

        public Object areValidInformations(HttpContext context)
        {
            string anEncryptedUid, anApiKey, anApiSecret;

            using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                string jsonstring = reader.ReadToEndAsync().WaitAsync(TimeSpan.FromMilliseconds(5000)).Result;

                JObject jsonResponse = JObject.Parse(jsonstring);

                Console.WriteLine((string)jsonResponse["anEncryptedUid"]);
                Console.WriteLine((string)jsonResponse["anApiKey"]);
                Console.WriteLine((string)jsonResponse["anApiSecret"]);

                anEncryptedUid = (string)jsonResponse["anEncryptedUid"];
                anApiKey = (string)jsonResponse["anApiKey"];
                anApiSecret = (string)jsonResponse["anApiSecret"];
            }

            return new { 
                retCode = 0,
                result = new {
                    isValid = /*Trader.verifyEncryptedUid(anEncryptedUid) && */BybitManager.verifyAuthentificationInformations(anApiKey, anApiSecret)
                }
            }; 
        }


    }
}
