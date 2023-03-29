/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      21/02/2023 -> codage et commentaires des fonctions Run,isGoodProxy
 *      27/02/2023 -> Mise à jour du code pour enlever les await et attente dans les méthodes d'origine
 *  Role : Définition de la classe RemoveInvalidProxiesThread qui sert à retirer des proxies invalides d'une liste passé en paramètre
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuyEnhancedServerv2.Utils;

namespace BuyEnhancedServer.Proxies
{
    /*
       *    Nom : RemoveInvalidProxiesThread
       *    Role : Définition de la classe RemoveInvalidProxiesThread qui sert à définir le comportement d'un Thread nettoyeur pour la liste passé en paramètre
   */
    internal class RemoveInvalidProxiesThread
    {
        //unique instance de RemoveInvalidProxiesThread
        private static RemoveInvalidProxiesThread? remove;
        // proxyList : objet de type ProxyList qui permet de stocker et manipuler une liste de WebProxy (ProxyList.list)
        private ProxyList proxyList;
        // timeout : nombre entier qui correspond au nombre de millisecondes écoulé à partir duquel on considère un proxy comme invalide
        private int timeout;
        //état du thread (true: actif, false: inactif)
        private bool state;
        //Thread pour lancer en parallèle l'ajout de proxy
        Thread thread;



        /*
        *    Nom : Instance
        *    Role : Créer l'unique objet statique de type RemoveInvalidProxiesThread
        *    Fiabilité : Sure
        */
        public static RemoveInvalidProxiesThread Instance(ref ProxyList aProxyList, int aTimeout = 3000)
        {
            Log.TraceInformation("RemoveInvalidProxiesThread.Instance", "Appel");

            if (RemoveInvalidProxiesThread.remove == null)
            {
                RemoveInvalidProxiesThread.remove = new RemoveInvalidProxiesThread(ref aProxyList, aTimeout);
            }

            return RemoveInvalidProxiesThread.remove;
        }

        /*
        *    Nom : RemoveInvalidProxiesThread (Constructeur)
        *    Paramètre E : [timeout] correspondant au nombre de millisecondes pour le test d'un proxy
        *    Paramètre E/S : aProxyList est la liste de proxy à nettoyer 
        *    Role : Créer un objet de type AddNewValidProxiesThread
        *    Fiabilité : Sure
        */
        private RemoveInvalidProxiesThread(ref ProxyList aProxyList, int aTimeout) 
        {
            Log.TraceInformation("RemoveInvalidProxiesThread", "Appel du constructeur");

            this.proxyList = aProxyList;
            this.timeout = aTimeout;
            this.state = false;
            this.thread = new(this.run);
        }

        /*
        *    Nom : Start
        *    Role : Démarrer le thread 
        *    Fiabilité : Sure
        */
        public void Start()
        {
            Log.TraceInformation("RemoveInvalidProxiesThread.Start", "Appel");
            this.state = true;
            this.thread.Start();
        }

        /*
        *    Nom : Stop
        *    Role : Arrêter le thread 
        *    Fiabilité : Sure
        */
        public void Stop()
        {
            Log.TraceInformation("RemoveInvalidProxiesThread.Stop", "Appel");
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
            Log.TraceInformation("RemoveInvalidProxiesThread.IsAlive", "Appel");
            return this.thread.IsAlive;
        }

        /*
        *    Nom : Run
        *    Role : retirer les proxies invalides de la liste passé en paramètre
        *    Fiabilité : Sure
        */
        public void run()
        {
            Log.TraceInformation("RemoveInvalidProxiesThread.Run", "Appel");

            try
            {
                while (this.state)
                {
                    Console.WriteLine("Remove proxy lancé");
                    Log.TraceInformation("RemoveInvalidProxiesThread.Run", "Nouvelle itération");

                    int i = 0;

                    //try-finally : pour s'assurer que le mutex est toujours libéré
                    try
                    {
                        this.proxyList.WaitOne();

                        //On parcourt la liste, si un proxy n'est pas valide on le supprime
                        while (i < this.proxyList.count())
                        {
                            if (!this.state) { return; }
                            Console.WriteLine("Nombre de proxy valide : " + this.proxyList.count().ToString());

                            if (!(this.isGoodProxy(this.proxyList.elementAt(i))))
                            {
                                this.proxyList.remove(i);
                                i--;
                            }

                            this.proxyList.ReleaseMutex();
                            i++;
                            this.proxyList.WaitOne();
                        }
                    }
                    finally { this.proxyList.ReleaseMutex(); }

                    Thread.Sleep(10000);

                    //try-finally : pour s'assurer que le mutex est toujours libéré
                    try
                    {
                        this.proxyList.WaitOne();
                        this.proxyList.save();
                    }
                    finally { this.proxyList.ReleaseMutex(); }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Remove --> Run --> Erreur générale : \nDétails : " + ex.ToString());
                Log.TraceError("RemoveInvalidProxiesThread.Run", "Erreur générale : \nDétails : " + ex.ToString());
            }     
        }

        /*
        *    Nom : isGoodProxy
        *    Retour : true si le proxy est valide, false si il est invalide
        *    Paramètre E : un WebProxy
        *    Role : Indiquer si un proxy est valide ou non
        *    Fiabilité : Sure
        */
        private bool isGoodProxy(WebProxy proxy)
        {
            //le proxy null est équivalent à ne pas utiliser de proxy
            if(proxy == null)
            {
                return true;
            }

            //création d'un gestionnaire de client pour utiliser un proxy
            var httpClientHandler = new HttpClientHandler { Proxy = proxy };

            //création du client qui utilise le gestionnaire de client (toujours pour le proxy)
            HttpClient client = new HttpClient(handler: httpClientHandler, disposeHandler: true);

            //mettre un timeout de 3 secondes 
            client.Timeout = TimeSpan.FromMilliseconds(this.timeout);

            //url de test (amazon)
            string url = "https://unagi-na.amazon.com/1/events/com.amazon.csm.nexusclient.prod";

            //json content de la requete de test
            string body = "{ \"cs\":{ \"dct\":{ \"#0\":\"domains\",\"#1\":\"www.amazon.com\",\"#2\": \"m.media-amazon.com\",\"#3\":\"images-na.ssl-images-amazon.com\",\"#4\":\"completion.amazon.com\",\"#5\": \"fls-na.amazon.com\",\"#6\": \"unagi.amazon.com\",\"#7\": \"unagi-na.amazon.com\",\"#8\": \"s.amazon-adsystem.com\",\"#9\": \"pageType\",\"#10\": \"Gateway\",\"#11\": \"subPageType\",\"#12\": \"desktop\",\"#13\": \"pageTypeId\",\"#14\": \"producerId\",\"#15\": \"schemaId\",\"#16\": \"csm.CrossOriginDomains.2\",\"#17\": \"timestamp\",\"#18\": \"messageId\",\"#19\": \"sessionId\",\"#20\": \"139 -2208358-8966228\",\"#21\": \"requestId\",\"#22\": \"0C02DRJ1AJ871YQH8NDK\",\"#23\": \"obfuscatedMarketplaceId\",\"#24\": \"ATVPDKIKX0DER\"} },\"events\": [{ \"data\": { \"#0\": { \"#1\": 16,\"#2\": 193,\"#3\": 58,\"#4\": 1,\"#5\": 14,\"#6\": 2,\"#7\": 1,\"#8\": 1},\"#9\": \"#10\",\"#11\": \"#12\",\"#13\": \"#12\",\"#14\": \"csm\",\"#15\": \"#16\",\"#17\": \"2022-12-14T16:08:16.398Z\",\"#18\": \"0C02DRJ1AJ871YQH8NDK-1671034096398-4549308886\",\"#19\": \"#20\",\"#21\": \"#22\",\"#23\": \"#24\"} }]}}";

            //conversion du json en contenu HTTP
            StringContent queryString = new StringContent(body);

            //on teste 2 fois le proxy
            for(int i=0;i<2;i++)
            {
                try
                {
                    //exécution de la requete
                    var response = client.PostAsync(url, queryString).WaitAsync(TimeSpan.FromMilliseconds(this.timeout)).Result;

                    //lecture de la réponse
                    string responseBody = response.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromMilliseconds(this.timeout)).Result;

                    //si le code d'état est différent de 200 alors on considère que la requete a échoué
                    if ((int)response.StatusCode != 200)
                    {
                        throw new Exception();
                    }

                    //si aucune erreur n'est leve alors on considère que le proxy est valide                
                    Console.WriteLine("remove --> isGoodProxy --> proxy valide : ");
                    return true;
                }
                catch (Exception)
                {
                    Console.WriteLine("remove --> isGoodProxy --> proxy invalide");
                }
            }

            return false;
        }
    }
}
