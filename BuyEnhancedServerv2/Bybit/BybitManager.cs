﻿/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      26/02/2023 -> codage des fonctions PrepareRequest,takePosition,ClosePosition,GetBalanceUSD,switchToHedgeMode,GetSymbolInfo,GetPositionSize
 *      27/02/2023 -> mise à jour de l'algorythmie de ClosePosition, commentaires de chaque fonctions (pas tout détailler, le principe est le même à chaque fois)
 *  Role : Définition de la classe BybitManager qui contient les méthodes pour la gestion d'un compte Bybit via l'api (obtention d'information, gestion des positions...)
 */


using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Io;
using AngleSharp.Text;
using BuyEnhancedServerv2.Utils;
using Newtonsoft.Json.Linq;
using RestSharp;


namespace BuyEnhancedServer.Bybit
{
    /*
        *    Nom : BybitManager
        *    Role : Classe qui sert à faciliter la communication avec l'API Bybit
    */
    internal class BybitManager
    {
        //endpoint de l'API
        private readonly string endpoint;
        //clé API
        private readonly string apiKey;
        //clé secrète API
        private readonly string apiSecret;
        //booléen indiquant si on est bien en HedgeMode
        private bool positionModeSet;
        //entier en millisecondes indiquant la durée de vie d'une requête vers l'API Bybit 
        private readonly int recvWindow;
        //client pour interroger l'API Bybit
        private readonly RestClient restClient;

        /*
        *    Nom : BybitManager
        *    Retour : aucun
        *    Paramètre E : la clé API et la clé API secrète
        *    Role : Constructeur de BybitManager, instance qui sert d'interface pour la communication avec l'API Bybit
        *    Fiabilité : Sure
        */
        public BybitManager(string anApi_key, string anApi_secret)
        {
            Log.TraceInformation("BybitManager", "Appel du constructeur");

            this.apiKey = anApi_key;

            this.apiSecret = anApi_secret;

            this.endpoint = "https://api-testnet.bybit.com";

            this.restClient = new RestClient(this.endpoint);

            this.recvWindow = 5000;

            this.positionModeSet = this.SwitchToHedgeMode();
        }

        /*
        *    Nom : GetTimestamp
        *    Retour : long indiquant le timestamp actuel au format universel
        *    Role : retourner le timestamp actuel au format universel
        *    Fiabilité : Sure
        */
        private static long GetTimestamp()
        {
            var now = (double)DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            return (long)Math.Round(now);
        }

        /*
        *    Nom : PrepareRequest
        *    Retour : une RestRequest prête à être envoyée
        *    Paramètre E : APIfunction qui est la chaine de caractère qui complète de le endpoint de l'API (ressource de l'API), request qui est une BybitRequest contenant les paramètres de la requête
        *    Role : préparer et retourner une RestRequest prête à être envoyer à partir d'une chaine de caractère (ressource API) et d'une BybitRequest 
        *    Fiabilité : Sure
        */
        private RestRequest PrepareRequest(string APIfunction, BybitRequest request)
        {
            RestRequest restRequest;

            if (request.isGetRequest)
            {
                if (request.paramStr != "")
                {
                    restRequest = new RestRequest(APIfunction + "?" + request.paramStr);
                }
                else
                {
                    restRequest = new RestRequest(APIfunction);
                }
            }
            else
            {
                restRequest = new RestRequest(APIfunction);
                restRequest.AddJsonBody(request.obj!);
            }

            double timestamp = BybitManager.GetTimestamp();

            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("X-BAPI-SIGN", request.getSign(this.apiSecret, timestamp, this.apiKey, this.recvWindow));
            restRequest.AddHeader("X-BAPI-API-KEY", this.apiKey);
            restRequest.AddHeader("X-BAPI-TIMESTAMP", timestamp);
            restRequest.AddHeader("X-BAPI-RECV-WINDOW", this.recvWindow);

            return restRequest;
        }

        /*
        *    Nom : TakePosition
        *    Retour : entier indiquant l'état de la requête, 0 : OK, 1 : symbol invalide, 2 : erreur d'origine inconue
        *    Paramètre E : une position qui contient les informations de la position à prendre et size qui correspond à la taille de la position à prendre
        *    Role : prendre une position à partir d'un objet Position et d'une taille
        *    Fiabilité : Sure (retourne le code d'erreur)
        */
        public int TakePosition(Position position, double size)
        {
            Log.TraceInformation("BybitManager.takePosition", "Appel");

            //on vérifie qu'on est bien en HedgeMode
            int switchingTry = 0;
            while (!this.positionModeSet)
            {
                switchingTry++;

                this.positionModeSet = this.SwitchToHedgeMode();

                if(switchingTry > 2)
                {
                    Console.WriteLine("Impossible de passer en HedgeMode");
                    Log.TraceError("BybitManager.takePosition", "Impossible de passer en HedgeMode");
                    return 2;
                }
            }

            //on crée le JSON body pour notre requête
            var jsonBody = new
            {
                category = "linear",
                position.symbol,
                position.side,
                orderType = "Market",
                qty = size.ToString(CultureInfo.InvariantCulture),
                position.positionIdx
            };

            //à partir du JSON body on crée une requête Bybit (facilite l'obtention de la signature)
            var bybitRequest = new BybitRequest(jsonBody);

            RestResponse response;

            //on essaye 5 fois
            for(int i = 1; i <= 5; i++)
            {
                try
                {
                    //on prépare la requête à partir de la requête Bybit précédemment créée
                    var restRequest = this.PrepareRequest("/v5/order/create", bybitRequest);

                    //on envoie simplement la requête
                    response = this.restClient.Post(restRequest);
                    
                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(response.Content);

                    if(response.Content != null)
                    {
                        JObject jsonResponse = JObject.Parse(response.Content);

                        if(jsonResponse["retCode"] != null)
                        {
                            int retCode = (int)jsonResponse["retCode"]!;

                            //si le code n'est pas 0 alors il y'a une erreur
                            if (retCode != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            return 0;
                        }
                    }

                    throw new Exception("Réponse de l'API bybit incorrecte : contenu ou code de retour null");
                }
                catch(BybitException ex)
                {
                    //si le code correspond à une requête invalide
                    if (ex.code == 10001)
                    {
                        if (ex.Message.Contains("symbol invalid"))
                        {
                            Log.TraceWarning("BybitManager.takePosition", "Symbole invalide");
                            return 1;
                        }
                        else
                        {
                            Console.WriteLine("BybitManager --> takePosition --> Erreur : requête invalide\nMessage : " + ex.Message);
                            Log.TraceError("BybitManager.takePosition", "Erreur : requête invalide\nMessage : " + ex.Message);

                            return 2;
                        }
                    }
                    
                    Console.WriteLine("BybitManager --> takePosition --> Erreur lors de l'essai numéro :" + i.ToString() + "\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.takePosition", "Erreur lors de l'essai numéro :" + i.ToString() + "\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("BybitManager --> takePosition --> Erreur lors de l'essai numéro :" + i.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.takePosition", "Erreur lors de l'essai numéro :" + i.ToString() + "\nMessage : " + ex.Message);
                }
                
            }

            Log.TraceError("BybitManager.takePosition", "Erreur lors de la prise de la position");

            return 2;
        }

        /*
        *    Nom : ClosePosition
        *    Retour : entier indiquant l'état de la requête, 0 : OK, 1 : symbol invalide, 2 : erreur d'origine inconue
        *    Paramètre E : une position qui contient les informations de la position à fermer
        *    Role : fermer une position à partir d'un objet Position
        *    Fiabilité : Sure (retourne le code d'erreur)
        */
        public int ClosePosition(Position position)
        {
            Log.TraceInformation("BybitManager.ClosePosition", "Appel");

            //on vérifie qu'on est bien en HedgeMode
            int switchingTry = 0;
            while (!this.positionModeSet)
            {
                switchingTry++;

                this.positionModeSet = this.SwitchToHedgeMode();

                if (switchingTry > 2)
                {
                    Console.WriteLine("Impossible de passer en HedgeMode");
                    Log.TraceError("BybitManager.ClosePosition", "Impossible de passer en HedgeMode");
                    return 2;
                }
            }

            double size;

            //on essaye d'obtenir la taille
            try
            {
                size = this.GetPositionSize(position);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return 2;
            }

            //on crée le JSON body pour notre requête
            var jsonBody = new
            {
                category = "linear",
                position.symbol,
                side = position.side == "Buy"?"Sell":"Buy",
                orderType = "Market",
                qty = size.ToString(CultureInfo.InvariantCulture),
                position.positionIdx
            };

            //à partir du JSON body on crée une requête Bybit (facilite l'obtention de la signature)
            var bybitRequest = new BybitRequest(jsonBody);

            RestResponse response;

            //on essaye 5 fois
            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    //on prépare la requête à partir de la requête Bybit précédemment créée
                    var restRequest = this.PrepareRequest("/v5/order/create", bybitRequest);

                    //on envoie simplement la requête
                    response = this.restClient.Post(restRequest);

                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(response.Content);

                    if(response.Content != null)
                    {
                        JObject jsonResponse = JObject.Parse(response.Content);

                        if(jsonResponse["retCode"] != null)
                        {
                            //si le code n'est pas 0 alors il y'a une erreur
                            if ((int)jsonResponse["retCode"]! != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            return 0;
                        }
                    }

                    throw new Exception("Réponse de l'API bybit incorrecte : contenu ou code de retour null");
                }
                catch (BybitException ex)
                {
                    //si le code correspond à une requête invalide
                    if (ex.code == 10001)
                    {
                        if (ex.Message.Contains("symbol invalid"))
                        {
                            Log.TraceError("BybitManager.ClosePosition", "Symbole invalide");
                            return 1;
                        }
                        else
                        {
                            Console.WriteLine("BybitManager --> ClosePosition --> Erreur : requête invalide\nMessage : " + ex.Message);
                            Log.TraceError("BybitManager.ClosePosition", "Erreur : requête invalide\nMessage : " + ex.Message);

                            return 2;
                        }
                    }

                    Console.WriteLine("BybitManager --> ClosePosition --> Erreur lors de l'essai numéro :" + i.ToString() + "\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.ClosePosition", "Erreur lors de l'essai numéro :" + i.ToString() + "\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BybitManager --> ClosePosition --> Erreur lors de l'essai numéro :" + i.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.ClosePosition", "Erreur lors de l'essai numéro :" + i.ToString() + "\nMessage : " + ex.Message);
                }

            }

            Log.TraceError("BybitManager.ClosePosition", "Erreur lors de la prise de la position");

            return 2;
        }

        /*
        *    Nom : GetBalanceUSD
        *    Retour : double indiquant le montant en USD disponible sur le compte courrant
        *    Role : retourner le montant en USDT disponible
        *    Fiabilité : Possibilité de lever une Exception
        */
        public double GetBalanceUSD()
        {
            Log.TraceInformation("BybitManager.GetBalanceUSD", "Appel");

            //paramètre de la requête GET
            string paramStr = "accountType=CONTRACT&coin=USDT";

            //à partir du JSON body on crée une requête Bybit (facilite l'obtention de la signature)
            BybitRequest bybitRequest = new (paramStr);

            RestResponse response;

            for (int i =1;i<=5;i++)
            {
                try
                {

                    //on prépare la requête à partir de la requête Bybit précédemment créée
                    RestRequest restRequest = this.PrepareRequest("/v5/account/wallet-balance", bybitRequest);

                    //on envoie simplement la requête
                    response = this.restClient.Get(restRequest);

                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(response.Content);

                    if(response.Content != null)
                    {
                        JObject jsonResponse = JObject.Parse(response.Content);

                        if (jsonResponse["retCode"] != null)
                        {
                            if ((int)jsonResponse["retCode"]! != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            double result = double.Parse(((string)jsonResponse["result"]!["list"]!.Children().ToList()[0]["coin"]!.Children().ToList()[0]["availableToWithdraw"])!, CultureInfo.InvariantCulture);

                            return result;
                        }
                    }

                    throw new Exception("Réponse de l'API bybit incorrecte : contenu ou code de retour null");
                }
                catch(BybitException ex)
                {
                    Console.WriteLine("BybitManager --> GetBalanceUSD --> Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.GetBalanceUSD", "Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("BybitManager --> GetBalanceUSD --> Erreur\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.GetBalanceUSD", "Erreur\nMessage : " + ex.Message);
                }
            }

            Log.TraceError("BybitManager.GetBalanceUSD", "Impossible d'obtenir le solde disponible sur le compte");
            throw new Exception("Impossible d'obtenir le solde disponible sur le compte");
        }

        /*
        *    Nom : SwitchToHedgeMode
        *    Retour : booléen indiquant l'état du mode Hedge; false : mode Hedge non actif, true : mode Hedge actif
        *    Role : passer en mode Hedge pour chaque symbole USDT
        *    Fiabilité : sure
        */
        private bool SwitchToHedgeMode()
        {
            Log.TraceInformation("BybitManager.switchToHedgeMode", "Appel");

            var jsonBody = new
            {
                category = "linear",
                coin = "USDT",
                mode = 3
            };

            var bybitRequest = new BybitRequest(jsonBody);

            RestResponse response;

            for (int i = 1; i <= 5; i++)
            {
                try
                {

                    RestRequest restRequest = this.PrepareRequest("/v5/position/switch-mode", bybitRequest);

                    response = this.restClient.Post(restRequest);

                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(response.Content);

                    if(response.Content != null)
                    {
                        JObject jsonResponse = JObject.Parse(response.Content);

                        if(jsonResponse["retCode"] != null)
                        {
                            if ((int)jsonResponse["retCode"]! != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            return true;
                        }
                    }

                    throw new Exception("Réponse de l'API bybit incorrecte : contenu ou code de retour null");
                }
                catch (BybitException ex)
                {
                    Console.WriteLine("BybitManager --> switchToHedgeMode --> Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.switchToHedgeMode", "Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BybitManager --> switchToHedgeMode --> Erreur\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.switchToHedgeMode", "Erreur\nMessage : " + ex.Message);
                }
            }

            Log.TraceError("BybitManager.switchToHedgeMode", "Impossible de passer en mode de couverture");
            return false;
        }

        /*
        *    Nom : GetSymbolInfo
        *    Retour : JToken contenant les informations correspondant au symbole passé en paramètre
        *    Paramètre E : un symbole pour lequel on veut obtenir des informations
        *    Role : retourner les informations sur un symbole sous forme de JToken
        *    Fiabilité : Possibilité de lever une BybitException (code=1 : invalid symbol) ou Exception
        */
        public void GetSymbolInfo(string aSymbol, out double minimum, out double step)
        {
            Log.TraceInformation("BybitManager.GetSymbolInfo", "Appel");

            HttpClient client = new();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string url = "https://api-testnet.bybit.com/";

            string fonction = "/v5/market/instruments-info?category=linear&symbol=" + aSymbol;

            string responseString;

            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    responseString = client.GetStringAsync(url + fonction).WaitAsync(TimeSpan.FromMilliseconds(this.recvWindow)).Result;

                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(responseString);

                    JObject jsonResponse = JObject.Parse(responseString);

                    if (jsonResponse != null)
                    {
                        if (jsonResponse["retCode"] != null)
                        {
                            if ((int)jsonResponse["retCode"]! != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            var symbolList = jsonResponse["result"]!["list"]!.Children().ToList();

                            if(symbolList.Count == 0)
                            {
                                throw new BybitException(1, "params error: symbol invalid");
                            }

                            minimum = double.Parse((string)symbolList.ElementAt(0)["lotSizeFilter"]!["minOrderQty"]!, CultureInfo.InvariantCulture);
                            step = double.Parse((string)symbolList.ElementAt(0)["lotSizeFilter"]!["qtyStep"]!, CultureInfo.InvariantCulture);

                            return;
                        }
                    }

                    throw new Exception("Réponse de l'API bybit incorrecte : contenu ou code de retour null");
                }
                catch (BybitException ex)
                {
                    if ((ex.code == 10001 || ex.code == 1) && ex.Message.Contains("symbol invalid"))
                    {
                        Log.TraceWarning("BybitManager.GetSymbolInfo", "Symbole invalide trouvé, exception levé (" + aSymbol + ")");
                        throw ex;
                    }
                    Console.WriteLine("BybitManager --> GetSymbolInfo --> Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.GetSymbolInfo", "Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BybitManager --> GetSymbolInfo --> Erreur\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.GetSymbolInfo", "Erreur\nMessage : " + ex.Message);
                }
            }

            Log.TraceError("BybitManager.GetSymbolInfo", "Impossible de trouver des informations sur  le symbole demandé après 5 essais");
            throw new Exception("Impossible de trouver des informations sur  le symbole demandé après 5 essais");
        }

        /*
        *    Nom : GetPositionSize
        *    Retour : double indiquant la taille de la position passé en paramètre
        *    Paramètre E : une position pour laquelle on veut obtenir la taille
        *    Role : retourner la taille d'une position à partir des informations contenues dans la position passé en paramètre
        *    Fiabilité : Possibilité de lever une Exception
        */
        private double GetPositionSize(Position aPosition)
        {
            Log.TraceInformation("BybitManager.GetPositionSize", "Appel");

            string paramStr = "category=linear&symbol=" + aPosition.symbol;

            BybitRequest bybitRequest = new(paramStr);

            RestResponse response;

            for (int i = 1; i <= 5; i++)
            {
                try
                {

                    RestRequest restRequest = this.PrepareRequest("/v5/position/list", bybitRequest);

                    response = this.restClient.Get(restRequest);

                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(response.Content);

                    if(response.Content != null)
                    {
                        JObject jsonResponse = JObject.Parse(response.Content);

                        if(jsonResponse["retCode"] != null)
                        {
                            if ((int)jsonResponse["retCode"]! != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            var positionList = jsonResponse["result"]!["list"]!.Children().ToList();

                            foreach (JToken position in positionList)
                            {
                                //Console.WriteLine(position);

                                if(position["side"] != null)
                                {
                                    if ((string)position["side"]! == aPosition.side)
                                    {
                                        return (double)position["size"]!;
                                    }
                                }
                            }

                            throw new Exception("Impossible de trouver la taille de la position demandée --> Essai numéro " + i.ToString());
                        }
                    }

                    throw new Exception("Réponse de l'API bybit incorrecte : un champ utilisé semble null");
                }
                catch (BybitException ex)
                {
                    Console.WriteLine("BybitManager --> switchToHedgeMode --> Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "Message : " + ex.Message);
                    Log.TraceWarning("BybitManager.GetPositionSize", "Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "Message : " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BybitManager --> switchToHedgeMode --> Erreur\nMessage: " + ex.Message);
                    Log.TraceWarning("BybitManager.GetPositionSize", "Erreur\nMessage: " + ex.Message);
                }
            }

            Log.TraceError("BybitManager.GetPositionSize", "Impossible de trouver des informations sur  le symbole demandé après 5 essais");
            throw new Exception("Impossible de trouver des informations sur  le symbole demandé après 5 essais");
        }

        /*
        *    Nom : PrepareVerifyRequest
        *    Retour : une RestRequest prête à être envoyée
        *    Paramètre E : APIfunction qui est la chaine de caractère qui complète de le endpoint de l'API (ressource de l'API), 2 chaine de caractères qui sont les paramètres d'authentification
        *    Role : préparer et retourner une RestRequest prête à être envoyer à partir d'une chaine de caractère (ressource API) et d'une combinaison de api key et api secret
        *    Fiabilité : Sure
        */
        static private RestRequest PrepareVerifyRequest(string APIfunction,string anApiKey,string anApiSecret)
        {
            RestRequest restRequest;

            restRequest = new RestRequest(APIfunction);

            BybitRequest bybitRequest = new("");

            double timestamp = BybitManager.GetTimestamp();

            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("X-BAPI-SIGN", bybitRequest.getSign(anApiSecret, timestamp, anApiKey, 5000));
            restRequest.AddHeader("X-BAPI-API-KEY", anApiKey);
            restRequest.AddHeader("X-BAPI-TIMESTAMP", timestamp);
            restRequest.AddHeader("X-BAPI-RECV-WINDOW", 5000);

            return restRequest;
        }

        /*
        *    Nom : verifyAuthentificationInformations
        *    Retour : bool indiquant si les indifiants sont correct (true: indentifiants corrects,  false: identifiants incorrects)
        *    Role : retourner un booléen idiquant la validite de la combinaison clé api et api secret
        *    Fiabilité : Possibilité de lever une Exception
        */
        static public bool verifyAuthentificationInformations(string anApiKey, string anApiSecret)
        {
            Log.TraceInformation("BybitManager.verifyAuthentificationInformations", "Appel");

            RestResponse response;

            RestClient restClient = new RestClient("https://api-testnet.bybit.com");

            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    //on prépare la requête à partir de la requête Bybit précédemment créée
                    RestRequest restRequest = BybitManager.PrepareVerifyRequest("/v5/user/query-api", anApiKey, anApiSecret);

                    //on envoie simplement la requête
                    response = restClient.Get(restRequest);

                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(response.Content);

                    if (response.Content != null)
                    {
                        JObject jsonResponse = JObject.Parse(response.Content);

                        if (jsonResponse["retCode"] != null)
                        {
                            if ((int)jsonResponse["retCode"]! == 10003 || (int)jsonResponse["retCode"]! == 10004)
                            {
                                return false;
                            }
                            if ((int)jsonResponse["retCode"]! != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            //double result = double.Parse(((string)jsonResponse["result"]!["list"]!.Children().ToList()[0]["coin"]!.Children().ToList()[0]["availableToWithdraw"])!, CultureInfo.InvariantCulture);
                            if (jsonResponse["result"]!["permissions"]!["ContractTrade"]!.Children().ToList().Contains("Order")
                            && jsonResponse["result"]!["permissions"]!["ContractTrade"]!.Children().ToList().Contains("Position")
                            && jsonResponse["result"]!["permissions"]!["Spot"]!.Children().ToList().Contains("SpotTrade")
                            && jsonResponse["result"]!["permissions"]!["Wallet"]!.Children().ToList().Contains("AccountTransfer")
                            && jsonResponse["result"]!["permissions"]!["Wallet"]!.Children().ToList().Contains("SubMemberTransfer")
                            && jsonResponse["result"]!["permissions"]!["Options"]!.Children().ToList().Contains("OptionsTrade")
                            && jsonResponse["result"]!["permissions"]!["Derivatives"]!.Children().ToList().Contains("DerivativesTrade")
                            && jsonResponse["result"]!["permissions"]!["CopyTrading"]!.Children().ToList().Contains("CopyTrading")
                            && jsonResponse["result"]!["permissions"]!["Exchange"]!.Children().ToList().Contains("ExchangeHistory"))
                            {
                                return true;
                            }

                            return false;
                        }
                    }
                    
                    throw new Exception("Réponse de l'API bybit incorrecte : contenu ou code de retour null");
                }
                catch (BybitException ex)
                {
                    Console.WriteLine("BybitManager --> verifyAuthentificationInformations --> Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.verifyAuthentificationInformations", "Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "\nMessage : " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BybitManager --> verifyAuthentificationInformations --> Erreur\nMessage : " + ex.Message);
                    Log.TraceWarning("BybitManager.verifyAuthentificationInformations", "Erreur\nMessage : " + ex.Message);
                }
            }

            Log.TraceError("BybitManager.verifyAuthentificationInformations", "Impossible de vérifier les informations d'authentification");
            throw new Exception("Impossible de vérifier les informations d'authentification");
        }

        /*
        *    Nom : getAllPositions
        *    Retour : Une liste de toutes les positions
        *    Paramètre E : aucun
        *    Role : retourner la liste de toutes les positions en cours
        *    Fiabilité : Possibilité de lever une Exception
        */
        public List<Position> getAllPositions()
        {
            Log.TraceInformation("BybitManager.getAllPositions", "Appel");

            string paramStr = "category=linear&settleCoin=USDT";

            BybitRequest bybitRequest = new(paramStr);

            RestResponse response;

            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    RestRequest restRequest = this.PrepareRequest("/v5/position/list", bybitRequest);

                    response = this.restClient.Get(restRequest);

                    //decommenter pour afficher la réponse au format JSON
                    //Console.WriteLine(response.Content);

                    if (response.Content != null)
                    {
                        JObject jsonResponse = JObject.Parse(response.Content);

                        if (jsonResponse["retCode"] != null)
                        {
                            if ((int)jsonResponse["retCode"]! != 0)
                            {
                                throw new BybitException((int)jsonResponse["retCode"]!, (string)jsonResponse["retMsg"]!);
                            }

                            var positionList = jsonResponse["result"]!["list"]!.Children().ToList();

                            List<Position> positions = new List<Position>();

                            foreach (JToken position in positionList)
                            {
                                //Console.WriteLine(position);
                                positions.Add(new Position((string)position["symbol"]!, (double)position["markPrice"]!, (string)position["updatedTime"]!, 0, (string)position["side"]!, (double)position["size"]!, (int)position["positionIdx"]!));
                            }

                            return positions;

                            throw new Exception("Impossible de trouver la taille de la position demandée --> Essai numéro " + i.ToString());
                        }
                    }

                    throw new Exception("Réponse de l'API bybit incorrecte : un champ utilisé semble null");
                }
                catch (BybitException ex)
                {
                    Console.WriteLine("BybitManager --> getAllPositions --> Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "Message : " + ex.Message);
                    Log.TraceWarning("BybitManager.getAllPositions", "Erreur avec la communication Bybit\nCode d'erreur : " + ex.code.ToString() + "Message : " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BybitManager --> getAllPositions --> Erreur\nMessage: " + ex.Message);
                    Log.TraceWarning("BybitManager.getAllPositions", "Erreur\nMessage: " + ex.Message);
                }
            }

            Log.TraceError("BybitManager.getAllPositions", "Impossible de trouver la liste des positions");
            throw new Exception("Impossible de trouver la liste des positions");
        }

        /*
        *    Nom : closeAllPositions
        *    Paramètre E : aucun
        *    Role : fermer toutes les positions en cours
        *    Fiabilité : Possibilité de lever une Exception
        */
        public void closeAllPositions()
        {
            List<Position> positions = this.getAllPositions();

            foreach (Position position in positions)
            {
                this.ClosePosition(position);
            }
        }
    }
}
