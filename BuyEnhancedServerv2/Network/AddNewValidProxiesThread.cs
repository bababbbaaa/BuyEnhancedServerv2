/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      18/02/2023 -> codage et commentaires des fonctions getNewProxiesFromGeonode,getNewProxiesFromFreeProxy,getNewProxiesFromProxyScrape,isGoodProxy,IsInTheList,getOnlyNewProxies
 *      21/02/2023 -> ajout du paramètre proxyList et prise en charge
 *      27/02/2023 -> Mise à jour du code pour enlever les await et attente dans les méthodes d'origine
 *  Role : Définition de la classe AddNewValidProxiesThread qui sert à ajouter des proxies valides à une liste passé en paramètre
 */


using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;
using BuyEnhancedServer.Binance;
using BuyEnhancedServerv2.Utils;
using BuyEnhancedServerv2.WebSocketController;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace BuyEnhancedServer.Proxies
{
    /*
       *    Nom : AddNewValidProxiesThread
       *    Role : Définition de la classe AddNewValidProxiesThread qui sert à définir le comportement d'un Thread qui garni une liste de proxy passé en paramètre
   */
    internal class AddNewValidProxiesThread
    {
        //unique instance de AddNewValidProxiesThread
        private static AddNewValidProxiesThread? add;
        // proxyList : objet de type ProxyList qui permet de stocker et manipuler une liste de WebProxy (ProxyList.list)
        private ProxyList proxyList;
        // timeout : nombre entier qui correspond au nombre de millisecondes écoulé à partir duquel on considère un proxy comme invalide
        private int timeout;
        // le client pour le scraping de proxy
        private HttpClient scraperClient;
        //état du thread (true: actif, false: inactif)
        private bool state;
        //Thread pour lancer en parallèle l'ajout de proxy
        Thread thread;


        /*
        *    Nom : Instance
        *    Role : Créer l'unique objet statique de type AddNewValidProxiesThread
        *    Fiabilité : Sure
        */
        public static AddNewValidProxiesThread Instance(ref ProxyList aProxyList, int aTimeout = 3000)
        {
            Log.TraceInformation("AddNewValidProxiesThread.Instance", "Appel");

            if (AddNewValidProxiesThread.add == null)
            {
                AddNewValidProxiesThread.add = new AddNewValidProxiesThread(ref aProxyList, aTimeout);
            }

            return AddNewValidProxiesThread.add;
        }


        /*
        *    Nom : AddNewValidProxiesThread (Constructeur)
        *    Paramètre E : [timeout] correspondant au nombre de millisecondes pour le test d'un proxy
        *    Paramètre E/S : aProxyList est la liste de proxy à remplir
        *    Role : Créer un objet de type AddNewValidProxiesThread
        *    Fiabilité : sure
        */
        private AddNewValidProxiesThread(ref ProxyList aProxyList, int aTimeout = 3000)
        {
            Log.TraceInformation("AddNewValidProxiesThread", "Appel du constructeur");

            this.proxyList = aProxyList;
            this.timeout = aTimeout;
            this.scraperClient = new HttpClient();
            this.state = false;
            this.thread = new(this.run);

            //ajouter les entêtes
            Dictionary<string, string> headers = new Dictionary<string, string> {
                        {"authority","www.google.com" },
                        {"accept-language","fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7" },
                        {"origin","https://www.google.com" },
                        {"clienttype","web" },
                        {"cache-control","max-age=0" },
                        {"sec-fetch-dest","document" },
                        {"sec-fetch-mode","navigate" },
                        {"sec-fetch-site","same-origin" },
                        {"sec-fetch-user","\"?1\"" },
                        {"upgrade-insecure-requests","\"1\"" },
                        {"user-agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36" }
                    };

            foreach (KeyValuePair<string, string> header in headers)
            {
                this.scraperClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            // 10 secondes de timeout car cela est exécuté qu'une seule fois chaque heure environ
            this.scraperClient.Timeout = TimeSpan.FromMilliseconds(10000);
        }

        /*
        *    Nom : Start
        *    Role : Démarrer le thread 
        *    Fiabilité : Sure
        */
        public void Start()
        {
            Log.TraceInformation("AddNewValidProxiesThread.Start", "Appel");
            this.state = true;
            this.thread.Start();

            _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
            {
                level = "information",
                message = "Add est lancé"
            }));
        }

        /*
        *    Nom : Stop
        *    Role : Arrêter le thread 
        *    Fiabilité : Sure
        */
        public void Stop()
        {
            Log.TraceInformation("AddNewValidProxiesThread.Stop", "Appel");
            this.state = false;
            while (this.isActiv()) ;
            this.thread = new(this.run);
        }

        /*
        *    Nom : isActiv
        *    Role : Donner l'état du thread
        *    Retour : booléen indiquant si le thread est actif (true: actif, false: inactif)
        *    Fiabilité : Sure
        */
        public bool isActiv()
        {
            Log.TraceInformation("AddNewValidProxiesThread.IsAlive", "Appel");
            return this.thread.IsAlive;
        }


        /*
        *    Nom : run
        *    Role : ajouter de nouveaux proxy à la liste passé en paramètre
        *    Fiabilité : Sure
        */
        public void run() 
        {
            Log.TraceInformation("AddNewValidProxiesThread.Run", "Appel");

            int count;

            try
            {
                while(this.state)
                {
                    Console.WriteLine("Add lancé");
                    Log.TraceInformation("AddNewValidProxiesThread.Run", "Nouvelle itération");

                    List<WebProxy> newProxies= new List<WebProxy>();

                    //exemple avec l'ancienne méthode (avec async-await)
                    //newProxies = newProxies.Join(await this.getNewProxiesFromProxyScrape());

                    //on ajoute chaque nouveau proxy à une liste nommée newProxies
                    try
                    {
                        _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                        {
                            level = "information",
                            message = "Recherche de proxy sur Proxy Scrape"
                        }));

                        if (!this.state){return;}
                        newProxies = newProxies.Join(this.getNewProxiesFromProxyScrape());
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Add --> Run --> Erreur lors de l'obtention de nouveau proxy depuis Proxy Scrape\nMessage : " + e.Message);
                        Log.TraceWarning("AddNewValidProxiesThread.Run", "Erreur lors de l'obtention de nouveau proxy depuis Proxy Scrape\nMessage : " + e.Message);

                        _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                        {
                            level = "warning",
                            message = "Erreur lors de l'obtention de nouveau proxy depuis Proxy Scrape"
                        }));
                    }

                    try
                    {
                        _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                        {
                            level = "information",
                            message = "Recherche de proxy sur Geonode"
                        }));

                        if (!this.state) { return; }
                        newProxies = newProxies.Join(this.getNewProxiesFromGeonode());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Add --> Run --> Erreur lors de l'obtention de nouveau proxy depuis Geonode\nMessage : " + e.Message);
                        Log.TraceWarning("AddNewValidProxiesThread.Run", "Erreur lors de l'obtention de nouveau proxy depuis Geonode\nMessage : " + e.Message);

                        _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                        {
                            level = "warning",
                            message = "Erreur lors de l'obtention de nouveau proxy depuis Geonode"
                        }));
                    }

                    try
                    {
                        _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                        {
                            level = "information",
                            message = "Recherche de proxy sur FreeProxy"
                        }));

                        if (!this.state) { return; }
                        newProxies = newProxies.Join(this.getNewProxiesFromFreeProxy());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Add --> Run --> Erreur lors de l'obtention de nouveau proxy depuis FreeProxy\nMessage : " + e.Message);
                        Log.TraceWarning("AddNewValidProxiesThread.Run", "Erreur lors de l'obtention de nouveau proxy depuis FreeProxy\nMessage : " + e.Message);

                        _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                        {
                            level = "warning",
                            message = "Erreur lors de l'obtention de nouveau proxy depuis FreeProxy"
                        }));
                    }
                    
                    //on enlève les proxies déjà présent dans la liste
                    newProxies = this.getOnlyNewProxies(newProxies);

                    count = 0;

                    //pour chaque proxy de la liste newProxies, on les teste et on les ajoutes à la liste s'ils sont valides
                    foreach (WebProxy newProxy in newProxies)
                    {
                        if (!this.state) { return; }

                        count++;

                        Console.WriteLine($"Add --> {count}/{newProxies.Count}");

                        _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                        {
                            level = "infomation",
                            message = $"Test : {count}/{newProxies.Count}"
                        }));

                        if (this.isGoodProxy(newProxy))
                        {
                            //try-finally : pour s'assurer que le mutex est toujours libéré
                            try
                            {
                                this.proxyList.WaitOne();
                                this.proxyList.add(newProxy);
                            }
                            finally { this.proxyList.ReleaseMutex(); }
                        }

                        //sauvegarde de la liste chaque 20 passages
                        if(count%20 == 0)
                        {
                            //try-finally : pour s'assurer que le mutex est toujours libéré
                            try
                            {
                                this.proxyList.WaitOne();
                                this.proxyList.save();
                            }
                            finally { this.proxyList.ReleaseMutex(); }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Add --> Run --> Erreur générale : \nDétails : " + ex.ToString());
                Log.TraceError("AddNewValidProxiesThread.Run", "Erreur générale : \nDétails : " + ex.ToString());
            }
        }

        /*
        *    Nom : getNewProxiesFromGeonode
        *    Retour : Liste de WebProxy si réussie sinon null
        *    Role : Renvoyer une liste de WebProxy obtenu à partir du site Geonode
        *    Fiabilité : Possibilité de lever une Exception
        */
        private List<WebProxy> getNewProxiesFromGeonode()
        {
            Log.TraceInformation("AddNewValidProxiesThread.getNewProxiesFromGeonode", "Appel");

            int nbTries = 0;

            while (nbTries < 5)
            {
                try
                {

                    //informations pour la requete
                    string url = "https://proxylist.geonode.com/";
                    string fonction = "api/proxy-list";
                    string param = "?limit=500&page=1&sort_by=upTime&sort_type=desc&protocols=http";

                    //on recupere le JSON
                    var responseString = this.scraperClient.GetStringAsync(url + fonction + param).WaitAsync(TimeSpan.FromMilliseconds(10000)).Result;
                    
                    //decommenter pour afficher le code HTML
                    //Console.WriteLine(responseString);

                    //on crée un analyser JSON dans lequel on met la réponse JSON
                    JObject o = JObject.Parse(responseString);

                    if (o["data"] != null)
                    {
                        //Dans ce JSON on récupere une liste dans le champ data
                        List<JToken> list = o["data"]!.Children().ToList();

                        List<WebProxy> proxies = new List<WebProxy>();

                        //pour chaque element de la liste
                        foreach (JToken token in list)
                        {
                            if (token["ip"] != null && token["port"] != null)
                            {
                                //on prend la champ ip et port
                                string ip = (string)token["ip"]!;
                                int port = (int)token["port"]!;

                                //on crée un WebProxy que l'on ajoute à la liste que l'on retournera 
                                proxies.Add(new WebProxy(ip, port));
                            }
                        }

                        return proxies;
                    }

                    throw new Exception("Impossible de récupérer les informations depuis Geonode");
                }
                catch(Exception ex)
                {
                    Log.TraceWarning("AddNewValidProxiesThread.getNewProxiesFromGeonode", "Erreur lors de l'obtention de nouveau proxy\nMessage : " + ex.Message);
                    nbTries++;
                }

            }

            Log.TraceWarning("AddNewValidProxiesThread.getNewProxiesFromGeonode", "Impossible de récupérer de nouveau proxy");
            throw new Exception("Add --> getNewProxiesFromGeonode --> Echec de l'obtention de nouveau proxy");
        }

        /*
        *    Nom : getNewProxiesFromFreeProxy
        *    Retour : Liste de WebProxy si réussie sinon null
        *    Role : Renvoyer une liste de WebProxy obtenu à partir du site FreeProxy
        *    Fiabilité : Possibilité de lever une Exception
        */
        private List<WebProxy> getNewProxiesFromFreeProxy()
        {
            Log.TraceInformation("AddNewValidProxiesThread.getNewProxiesFromFreeProxy", "Appel");

            int nbTries = 0;

            while (nbTries < 5)
            {
                try
                {

                    string url = "https://free-proxy-list.net/";

                    //obtention de la page HTML
                    var responseString = this.scraperClient.GetStringAsync(url).WaitAsync(TimeSpan.FromMilliseconds(10000)).Result;

                    //decommenter pour voir la page HTML
                    //Console.WriteLine(responseString);

                    //création d'un analyseur HTML
                    var parser = new HtmlParser();

                    //création du document pour manipuler le HTML
                    var document = parser.ParseDocumentAsync(responseString).WaitAsync(TimeSpan.FromMilliseconds(5000)).Result;

                    //décommenter pour voir le code HTML du permier tableau de la page
                    //Console.WriteLine(document.QuerySelector("table").OuterHtml);

                    if(document.QuerySelector("table > tbody") != null)
                    {
                        //tableChildren contient les balises filles du corps du premier tableau de la page
                        var tableChildren = document.QuerySelector("table > tbody")!.Children;

                        List<WebProxy> proxies = new List<WebProxy>();

                        //pour chaque ligne du contenu du tableau 
                        foreach (var child in tableChildren)
                        {
                            //on prend la premier colone (ip) et la deuxieme (port)
                            string ip = (string)child.Children[0].InnerHtml;
                            int port = int.Parse(child.Children[1].InnerHtml);

                            //à partir de ces informations on crée un WebProxy et on l'ajoute à la liste que l'on retournera
                            proxies.Add(new WebProxy(ip, port));
                        }

                        return proxies;
                    }

                    throw new Exception("Impossible de récupérer les informations depuis Free Proxy");
                }
                catch(Exception ex)
                {
                    Log.TraceWarning("AddNewValidProxiesThread.getNewProxiesFromFreeProxy", "Erreur lors de l'obtention de nouveau proxy\nMessage : " + ex.Message);
                    nbTries++;
                }
            }

            Log.TraceWarning("AddNewValidProxiesThread.getNewProxiesFromFreeProxy", "Impossible de récupérer de nouveau proxy");
            throw new Exception("Add --> getNewProxiesFromFreeProxy --> Echec de l'obtention de nouveau proxy");
        }


        /*
        *    Nom : getNewProxiesFromProxyScrape
        *    Retour : Liste de WebProxy si réussie sinon null
        *    Role : Renvoyer une liste de WebProxy obtenu à partir du site ProxyScrape
        *    Fiabilité : Possibilité de lever une Exception
        */
        private List<WebProxy> getNewProxiesFromProxyScrape()
        {
            Log.TraceInformation("AddNewValidProxiesThread.getNewProxiesFromProxyScrape", "Appel");

            int nbTries = 0;

            while(nbTries < 5)
            {
                try
                {

                    //informations pour la requete
                    string url = "https://api.proxyscrape.com/";
                    string fonction = "v2/";
                    string param = "?request=displayproxies&protocol=http&timeout=2000&country=all&ssl=all&anonymity=all&_ga=2.17107697.950134058.1671362691-2076448087.1671362691";

                    //on recupere la réponse sous forme de texte
                    var responseString = this.scraperClient.GetStringAsync(url + fonction + param).WaitAsync(TimeSpan.FromMilliseconds(10000)).Result;

                    //decommenter pour voir le contenu de la reponse
                    //Console.WriteLine(responseString);

                    /*Exemple format de la reponse : 
                        103.160.134.206:80
                        195.189.226.31:3128
                        117.54.114.35:80
                        45.248.138.150:8080
                        43.245.94.229:4995
                        123.205.68.113:80
                        103.153.149.211:8181
                        116.98.178.158:10003
                     */


                    //cette partie analyse le texte reçu
                    /*
                     On parcourt chaque caratère :
                        - on ajoute chaque caractère à l'ip
                        - si le caractère est ":" alors isPort devient vrai et les prochains caractères seront ajouter au port
                        - si le caractère est un caractère de fin de ligne alors on ajoute un WebProxy qui a été créé à partir des infos obtenues (ip et port) à la liste de proxy que l'on retournera             
                     */
                    string ip = "";
                    string port = "";

                    bool isPort = false;

                    List<WebProxy> proxies = new List<WebProxy>();

                    foreach (char letter in responseString)
                    {
                        if (letter != '\n' && letter != '\r')
                        {
                            if (letter == ':')
                            {
                                isPort = true;
                            }
                            else
                            {
                                if (isPort)
                                {
                                    port += letter;
                                }
                                else
                                {
                                    ip += letter;
                                }
                            }
                        }
                        else if (ip != "" || port != "" || isPort == true)
                        {
                            proxies.Add(new WebProxy(ip, int.Parse(port)));

                            ip = "";
                            port = "";
                            isPort = false;
                        }
                    }

                    return proxies;
                }
                catch(Exception ex)
                {
                    Log.TraceWarning("AddNewValidProxiesThread.getNewProxiesFromProxyScrape", "Erreur lors de l'obtention de nouveau proxy\nMessage : " + ex.Message);
                    nbTries++;
                }
            }

            Log.TraceWarning("AddNewValidProxiesThread.getNewProxiesFromProxyScrape", "Impossible de récupérer de nouveau proxy");
            throw new Exception("Add --> getNewProxiesFromProxyScrape --> Echec de l'obtention de nouveau proxy");
        }


        /*
        *    Nom : isGoodProxy
        *    Retour : true si le proxy est valide, false si il est invalide
        *    Paramètre E : un WebProxy
        *    Role : Indiquer si un proxy est valide ou non
        *    Fiabilité : sure
        */
        private bool isGoodProxy(WebProxy proxy)
        {
            //création d'un gestionnaire de client pour utiliser un proxy
            var httpClientHandler = new HttpClientHandler{Proxy = proxy};

            //création du client qui utilise le gestionnaire de client (toujours pour le proxy)
            HttpClient client = new HttpClient(handler: httpClientHandler, disposeHandler: true);

            //mettre un timeout 
            client.Timeout = TimeSpan.FromMilliseconds(this.timeout);

            //url de test (amazon)
            string url = "https://unagi-na.amazon.com/1/events/com.amazon.csm.nexusclient.prod";

            //json content de la requete de test
            string body = "{ \"cs\":{ \"dct\":{ \"#0\":\"domains\",\"#1\":\"www.amazon.com\",\"#2\": \"m.media-amazon.com\",\"#3\":\"images-na.ssl-images-amazon.com\",\"#4\":\"completion.amazon.com\",\"#5\": \"fls-na.amazon.com\",\"#6\": \"unagi.amazon.com\",\"#7\": \"unagi-na.amazon.com\",\"#8\": \"s.amazon-adsystem.com\",\"#9\": \"pageType\",\"#10\": \"Gateway\",\"#11\": \"subPageType\",\"#12\": \"desktop\",\"#13\": \"pageTypeId\",\"#14\": \"producerId\",\"#15\": \"schemaId\",\"#16\": \"csm.CrossOriginDomains.2\",\"#17\": \"timestamp\",\"#18\": \"messageId\",\"#19\": \"sessionId\",\"#20\": \"139 -2208358-8966228\",\"#21\": \"requestId\",\"#22\": \"0C02DRJ1AJ871YQH8NDK\",\"#23\": \"obfuscatedMarketplaceId\",\"#24\": \"ATVPDKIKX0DER\"} },\"events\": [{ \"data\": { \"#0\": { \"#1\": 16,\"#2\": 193,\"#3\": 58,\"#4\": 1,\"#5\": 14,\"#6\": 2,\"#7\": 1,\"#8\": 1},\"#9\": \"#10\",\"#11\": \"#12\",\"#13\": \"#12\",\"#14\": \"csm\",\"#15\": \"#16\",\"#17\": \"2022-12-14T16:08:16.398Z\",\"#18\": \"0C02DRJ1AJ871YQH8NDK-1671034096398-4549308886\",\"#19\": \"#20\",\"#21\": \"#22\",\"#23\": \"#24\"} }]}}";
            
            //conversion du json en contenu HTTP
            StringContent queryString = new StringContent(body);

            try
            {
                //exécution de la requete
                var response = client.PostAsync(url, queryString).WaitAsync(client.Timeout).Result;

                //lecture de la réponse
                string responseBody = response.Content.ReadAsStringAsync().WaitAsync(client.Timeout).Result;

                //si le code d'état est différent de 200 alors on considère que la requete a échoué
                if ((int)response.StatusCode != 200)
                {
                    throw new Exception();
                }

                //si aucune erreur n'est leve alors on considère que le proxy est valide                
                Console.WriteLine("add --> isGoodProxy --> nouveau proxy valide : " + proxy.Address!.ToString());

                _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                {
                    level = "infomation",
                    message = "Proxy valide : " + proxy.Address!.ToString()
                }));

                return true;
            }
            catch(Exception)
            {
                Console.WriteLine("add --> isGoodProxy --> Proxy invalide");

                _ = WebSocketController.SendToAdd(JsonSerializer.Serialize(new
                {
                    level = "infomation",
                    message = "Proxy invalide"
                }));
            }
            
            return false;
        }

        /*
        *    Nom : getOnlyNewProxies
        *    Retour : Liste de WebProxy sans les proxies qui sont déjà dans la liste
        *    Paramètre E : un WebProxy
        *    Role : Filtrer une liste de WebProxy en retirant ceux qui y sont déjà
        *    Fiabilité : sure
        */
        private List<WebProxy> getOnlyNewProxies(List<WebProxy> newProxies)
        {
            Log.TraceInformation("AddNewValidProxiesThread.getOnlyNewProxies", "Appel");

            var onlyNewProxies = new List<WebProxy>();

            //try-finally : pour s'assurer que le mutex est toujours libéré
            try
            {
                this.proxyList.WaitOne();
                
                foreach (WebProxy newProxy in newProxies)
                {
                    if (!this.proxyList.isInTheList(newProxy))
                    {
                        onlyNewProxies.Add(newProxy);
                        Console.WriteLine("Add --> Nouveau proxy trouvee");
                    }
                }
            }
            finally { this.proxyList.ReleaseMutex(); }

            return onlyNewProxies;
        }
    }
}
