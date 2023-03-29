using BuyEnhancedServer.Binance;
using BuyEnhancedServer.Bybit;
using BuyEnhancedServer.Proxies;
using BuyEnhancedServerv2.Utils;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace BuyEnhancedServerv2.API
{
    public class API
    {
        private static API? server;
        ProxyList list;
        AddNewValidProxiesThread add;
        RemoveInvalidProxiesThread remove;
        Dictionary<string,Trader> subscriptions;

        /*
        *    Nom : Instance
        *    Role : Créer l'unique objet statique de type Server
        *    Fiabilité : Sure
        */
        public static API Instance()
        {
            Log.TraceInformation("Server.Instance", "Appel");

            if (API.server == null)
            {
                API.server = new API();
            }

            return API.server;
        }

        private API() 
        {
            this.list = ProxyList.Instance();

            this.add = AddNewValidProxiesThread.Instance(ref this.list);
            this.remove = RemoveInvalidProxiesThread.Instance(ref this.list);

            this.subscriptions = new Dictionary<string, Trader>();

            //je pense stocker dans cet objet les souscriptions dans un dictionnaire 
            //Dictionary<string, Trader> subscrition;
        }
        public Object getProxyCount()
        {
            return new { 
                retCode = 0,
                result = new { proxyCount = this.list.count()}
            };
        }

        public Object isRemoveActiv()
        {
            return new
            {
                retCode = 0,
                result = new { isActiv = this.remove.isActiv() }
            };
        }

        public Object startRemove()
        {
            try
            {
                this.remove.Start();

                return new { retCode = 0 };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object stopRemove()
        {
            try
            {
                this.remove.Stop();

                return new { retCode = 0 };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object isAddActiv()
        {
            try
            {
                return new
                {
                    retCode = 0,
                    result = new { isActiv = this.add.isActiv() }
                };
            }
            catch(Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object startAdd()
        {
            try
            {
                this.add.Start();

                return new { retCode = 0 };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object stopAdd()
        {
            try
            {
                this.add.Stop();

                return new { retCode = 0 };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object getSubscriptionList()
        {
            try
            {
                List<Object> subscriptionList = new List<Object>();

                foreach(string anEncryptedUid in this.subscriptions.Keys)
                {
                    string state;

                    switch (this.subscriptions[anEncryptedUid].GetState())
                    {
                        case State.running:
                            state = "running";
                            break;
                        case State.softStopped:
                            state = "soft stop";
                            break;
                        case State.stopped:
                            state = "stopped";
                            break;
                        default:
                            state = "unknown";
                            break;
                    }

                    subscriptionList.Add(new
                    {
                        encryptedUid = anEncryptedUid,
                        state = state,
                        isActiv = this.subscriptions[anEncryptedUid].isActiv()
                    });
                }

                return new { retCode = 0 , result = new { list = subscriptionList } };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object areValidInformations(HttpContext context)
        {
            try
            {
                string anEncryptedUid, anApiKey, anApiSecret;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    JObject jsonResponse = JObject.Parse(reader.ReadToEndAsync().WaitAsync(TimeSpan.FromMilliseconds(5000)).Result);

                    if (jsonResponse["anEncryptedUid"] != null && jsonResponse["anApiKey"] != null && jsonResponse["anApiSecret"] != null)
                    {
                        anEncryptedUid = (string)jsonResponse["anEncryptedUid"]!;
                        anApiKey = (string)jsonResponse["anApiKey"]!;
                        anApiSecret = (string)jsonResponse["anApiSecret"]!;

                        return new
                        {
                            retCode = 0,
                            result = new {
                                isValid = Trader.verifyEncryptedUid(anEncryptedUid) && BybitManager.verifyAuthentificationInformations(anApiKey, anApiSecret)
                            }
                        };
                    }
                }

                return new {
                    retCode = 1,
                    retMessage = "Le requête semble invalide"
                };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object addTrader(HttpContext context)
        {
            try
            {
                string anEncryptedUid, anApiKey, anApiSecret;
                int aPourcentage;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    JObject jsonResponse = JObject.Parse(reader.ReadToEndAsync().WaitAsync(TimeSpan.FromMilliseconds(5000)).Result);

                    if (jsonResponse["anEncryptedUid"] != null && jsonResponse["anApiKey"] != null && jsonResponse["anApiSecret"] != null && jsonResponse["aPourcentage"] != null)
                    {
                        anEncryptedUid = (string)jsonResponse["anEncryptedUid"]!;
                        anApiKey = (string)jsonResponse["anApiKey"]!;
                        anApiSecret = (string)jsonResponse["anApiSecret"]!;
                        aPourcentage = (int)jsonResponse["aPourcentage"]!;

                        if (Trader.verifyEncryptedUid(anEncryptedUid) && BybitManager.verifyAuthentificationInformations(anApiKey, anApiSecret))
                        {

                            if (!this.subscriptions.ContainsKey(anEncryptedUid))
                            {
                                this.subscriptions.Add(anEncryptedUid, new Trader(anEncryptedUid, ref this.list, anApiKey, anApiSecret, aPourcentage));

                                return new { retCode = 0, retMessage = "Trader ajouté avec succès" };
                            }

                            return new
                            {
                                retCode = 7,
                                retMessage = "Une souscription pour ce trader existe déjà"
                            };
                        }

                        return new
                        {
                            retCode = 4,
                            retMessage = "Les informations d'authentification à l'api ou l'identifiant du trader est incorrect"
                        };
                    }
                }

                return new
                {
                    retCode = 1,
                    retMessage = "Le requête semble invalide"
                };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object launchSubscription(HttpContext context)
        {
            try
            {
                string anEncryptedUid;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    JObject jsonResponse = JObject.Parse(reader.ReadToEndAsync().WaitAsync(TimeSpan.FromMilliseconds(5000)).Result);

                    if (jsonResponse["anEncryptedUid"] != null)
                    {
                        anEncryptedUid = (string)jsonResponse["anEncryptedUid"]!;

                        if (this.subscriptions.ContainsKey(anEncryptedUid))
                        {
                            if(Trader.verifyEncryptedUid(anEncryptedUid))
                            {
                                if (!this.subscriptions[anEncryptedUid].isActiv())
                                {
                                    subscriptions[anEncryptedUid].Start();

                                    return new { retCode = 0, retMessage = "Souscription lancée avec succès" };
                                }

                                return new
                                {
                                    retCode = 5,
                                    retMessage = "La souscription est déjà lancé"
                                };
                            }

                            return new
                            {
                                retCode = 4,
                                retMessage = "Les informations d'authentification à l'api ou l'identifiant du trader est incorrect"
                            };

                        }

                        return new
                        {
                            retCode = 6,
                            retMessage = "Le trader n'est pas enregistré"
                        };
                    }
                }

                return new {
                    retCode = 1,
                    retMessage = "Le requête semble invalide"
                };
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }

        }

        public Object brutalStop(HttpContext context)
        {
            try
            {
                string anEncryptedUid;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    JObject jsonResponse = JObject.Parse(reader.ReadToEndAsync().WaitAsync(TimeSpan.FromMilliseconds(5000)).Result);

                    if(jsonResponse["anEncryptedUid"] != null)
                    {
                        anEncryptedUid = (string)jsonResponse["anEncryptedUid"]!;

                        if (this.subscriptions.ContainsKey(anEncryptedUid))
                        {
                            this.subscriptions[anEncryptedUid].Stop();

                            return new { retCode = 0, retMessage = "Souscription arrêtée avec succès" };
                        }

                        return new
                        {
                            retCode = 3,
                            retMessage = "Impossible de trouver des informations liées à une souscription avec cet identifiant de trader"
                        };
                    }

                    return new {
                        retCode = 1,
                        retMessage = "Le requête semble invalide"
                    };
                }

            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object softStop(HttpContext context)
        {
            try
            {
                string anEncryptedUid;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    JObject jsonResponse = JObject.Parse(reader.ReadToEndAsync().WaitAsync(TimeSpan.FromMilliseconds(5000)).Result);

                    if (jsonResponse["anEncryptedUid"] != null)
                    {
                        anEncryptedUid = (string)jsonResponse["anEncryptedUid"];

                        if (this.subscriptions.ContainsKey(anEncryptedUid))
                        {
                            this.subscriptions[anEncryptedUid].softStop();

                            return new { retCode = 0, retMessage = "Souscription arrêtée avec succès" };
                        }

                        return new
                        {
                            retCode = 3,
                            retMessage = "Impossible de trouver des informations liées à une souscription avec cet identifiant de trader"
                        };
                    }

                    return new
                    {
                        retCode = 1,
                        retMessage = "Le requête semble invalide"
                    };
                }

            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }

        public Object getTraderState(HttpContext context)
        {
            try
            {
                string anEncryptedUid;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    JObject jsonResponse = JObject.Parse(reader.ReadToEndAsync().WaitAsync(TimeSpan.FromMilliseconds(5000)).Result);

                    if(jsonResponse["anEncryptedUid"] != null)
                    {
                        anEncryptedUid = (string)jsonResponse["anEncryptedUid"];

                        if (this.subscriptions.ContainsKey(anEncryptedUid))
                        {

                            string state;

                            switch (this.subscriptions[anEncryptedUid].GetState())
                            {
                                case State.running:
                                    state = "running";
                                    break;
                                case State.softStopped:
                                    state = "soft stop";
                                    break;
                                case State.stopped:
                                    state = "stopped";
                                    break;
                                default:
                                    state = "unknown";
                                    break;
                            }

                            return new
                            {
                                retCode = 0,
                                result = new
                                {
                                    isActiv = this.subscriptions[anEncryptedUid].isActiv(),
                                    state = state,
                                }
                            };
                        }
                        
                        return new
                        {
                            retCode = 6,
                            retMessage = "Aucun trader avec ces informations n'est enregistré"
                        };
                    }

                    return new
                    {
                        retCode = 1,
                        retMessage = "Le requête semble invalide"
                    };

                }
            }
            catch (Exception e)
            {
                return new { retCode = 2, retMessage = e.ToString() };
            }
        }
    }
}
