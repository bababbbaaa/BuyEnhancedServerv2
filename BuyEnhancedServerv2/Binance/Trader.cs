/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      26/02/2023 -> codage des fonctions GetPositionsJSON, GetPositions
 *      27/02/2023 -> codage des fonctions Trader, Run, IsInTheList, PrepareSize, UpdateCoef, GetSize, StartFollowing, UpdateOldPositions, UpdateUntakedPositions,
 *      UpdateUnclosedPositions, UpdateUnalteredPositions, TakePositions, ClosePositions, AlterPositions + fonctions de test
 *      + l'intégralité des commentaires
 *  Role : Définition de la classe BybitManager qui contient les méthodes pour la gestion d'un compte Bybit via l'api (obtention d'information, gestion des positions...)
 */

using BuyEnhancedServer.Bybit;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BuyEnhancedServer.Proxies;
using AngleSharp.Html;
using System.Drawing;
using AngleSharp.Dom;
using System.Text.Json;
using System.Globalization;
using BuyEnhancedServerv2.Utils;

namespace BuyEnhancedServer.Binance
{
    enum State
    {
        stopped,
        running,
        softStopped
    }

    /*
        *    Nom : Trader
        *    Role : Classe qui sert à suivre un trader dont l'encryptedUid est spécifié en paramètre du constructeur
    */
    internal class Trader
    {
        //listes pour l'analyse des positions
        private readonly List<Position> oldPositions;
        private readonly List<Position> untakedPositions;
        private readonly List<Position> unclosedPositions;
        private readonly List<Position> unalteredPositions;
        private readonly List<Position> botPositions;

        //coefficient pour obtenir la taille de notre position à partir de celle du trader (mySize = coef*traderSize)
        private double coef;

        //clé api pour la connexion à Bybit
        private readonly string api_key;

        //clé secrète pour la connexion à Bybit
        private readonly string api_secret;

        //instance qui bybitManager qui permet la communication avec Bybit
        private readonly BybitManager bybitManager;

        //instance qui contient principalement la liste des proxy à utiliser pour récupérer les positions
        private readonly ProxyList proxyList;

        //UID du trader
        private readonly string encryptedUid;

        //pourcentage que l'on veut investir par trade 
        private readonly double pourcentage;

        //instance qui contient principalement la liste des symboles invalides et quelques méthodes de mainupulation
        private readonly InvalidSymbolList invalidSymbol;

        //client pour récupérer les positions
        private readonly RestClient restClient;

        //instance qui contient principalement la liste des tailles du trader et quelques méthodes de mainupulation
        private readonly SizeList sizeList;

        //indique si c'est la première fois que l'on récupère des positions
        private bool isFirstRun;

        //Thread pour lancer en parallèle une souscription
        Thread thread;

        //état du thread (true: actif, false: inactif)
        private State state;


        /*
        *    Nom : Trader
        *    Paramètre E : anEncrytepUid qui correspond à l'identifiant du Trader à suivre, une liste ProxyList pour bénéficier de la gestion de proxy,
        *    et optionnellement le pourcentage de notre Balance que nous comptons investir par trade (par défaut à 3)
        *    Role : Constructeur de Trader
        *    Fiabilite : sure
        */
        public Trader(string anEncryptedUid, ref ProxyList aProxyList,string anApiKey, string anApiSecret, double aPourcentage = 3)
        {
            Log.TraceInformation("Trader", "Appel du constructeur");

            this.api_key = anApiKey;
            this.api_secret = anApiSecret;
            this.bybitManager = new BybitManager(api_key, api_secret);
            this.proxyList = aProxyList;
            this.encryptedUid = anEncryptedUid;
            this.sizeList = new SizeList(this.encryptedUid);
            this.pourcentage = aPourcentage;
            this.invalidSymbol = InvalidSymbolList.Instance();
            this.isFirstRun = true;

            this.oldPositions = new List<Position>();
            this.untakedPositions = new List<Position>();
            this.unclosedPositions = new List<Position>();
            this.unalteredPositions = new List<Position>();
            this.botPositions = new List<Position>();

            this.thread = new(this.run);
            this.state = State.stopped;

            //Configuration du client REST
            this.restClient = new RestClient("http://www.binance.com/");

            Dictionary<string, string> headers = new Dictionary<string, string> {
                {"authority","www.binance.com" },
                {"accept","*/*" },
                {"accept-encoding","gzip, deflate, br" },
                {"accept-language","fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7" },
                {"origin","https://www.binance.com" },
                {"clienttype","web" },
                {"cache-control","max-age=0" },
                {"sec-fetch-dest","document" },
                {"sec-fetch-mode","navigate" },
                {"sec-fetch-site","same-origin" },
                {"sec-fetch-user","\"?1\"" },
                {"upgrade-insecure-requests","\"1\"" },
                {"user-agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36" },
                {"referer","https://www.binance.com/en/futures-activity/leaderboard/user?encryptedUid=" + this.encryptedUid}
            };

            this.restClient.AddDefaultHeaders(headers);
            
        }

        /*
        *    Nom : Start
        *    Role : Démarrer le thread 
        *    Fiabilité : Sure
        */
        public void Start()
        {
            Log.TraceInformation("Trader.Start", "Appel");
            this.state = State.running;
            this.thread.Start();
        }

        /*
        *    Nom : Stop
        *    Role : Arrêter le thread rapidement 
        *    Fiabilité : Sure
        */
        public void Stop()
        {
            Log.TraceInformation("Trader.Stop", "Appel");
            this.state = State.stopped;
            while (this.isActiv()) ;
            this.thread = new(this.run);
        }

        /*
        *    Nom : softStop
        *    Role : Arrêter la souscription de façon lente mais optimiser
        *    Fiabilité : Sure
        */
        public void softStop()
        {
            Log.TraceInformation("Trader.softStop", "Appel");
            this.state = State.softStopped;
        }

        /*
        *    Nom : isActiv
        *    Role : Donner l'état du thread
        *    Retour : booléen indiquant si le thread est actif (true: actif, false: inactif)
        *    Fiabilité : Sure
        */
        public bool isActiv()
        {
            Log.TraceInformation("Trader.IsAlive", "Appel");
            return this.thread.IsAlive;
        }

        /*
        *    Nom : Run
        *    Role : Assurer le suivi intégral des positions du trader spécifié
        *    Fiabilite : Sure
        */
        public void run()
        {
            Log.TraceInformation("Trader.Run", "Appel");

            try
            {
                while (this.state != State.stopped)
                {
                    Console.WriteLine("Nouvelle update position");
                    Log.TraceInformation("Trader.Run", "Nouvelle itération de la routine de mise à jour");

                    List<Position> traderPositions;

                    try
                    {
                        //traderPositions = this.GetFakePositions();

                        //peut lever une erreur
                        traderPositions = this.GetPositions();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Trader --> Run --> Impossible d'obtenir les positions du trader\nMessage : " + ex.Message);
                        Log.TraceWarning("Trader.Run", "GetPositions a générée une erreur\nMessage : " + ex.Message);
                        continue;
                    }

                    if (this.isFirstRun)
                    {
                        this.StartFollowing(traderPositions);
                        this.isFirstRun = false;
                    }

                    if(this.state != State.softStopped)
                    {
                        this.UpdateOldPositions(traderPositions);
                        this.UpdateUntakedPositions(traderPositions);
                    }
                    this.UpdateUnclosedPositions(traderPositions);
                    this.UpdateUnalteredPositions(traderPositions);
                    this.ClosePositions();
                    this.TakePositions();
                    this.AlterPositions(traderPositions);

                    Console.WriteLine("\nPortefeuille :");

                    foreach (Position position in this.botPositions)
                    {
                        position.display();
                    }

                    if (this.state == State.stopped) { break; }

                    if (this.state == State.softStopped && this.botPositions.Count == 0)
                    {
                        this.Stop();
                        break;
                    }

                    Thread.Sleep(3000);
                }

                this.bybitManager.closeAllPositions();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Trader --> Run --> Erreur générale : \nDétails : " + ex.ToString());
                Log.TraceError("Trader.Run", "Erreur générale : \nDétails : " + ex.ToString());
            }
        }

        /*
        *    Nom : GetPositionsJSON
        *    Retour : la liste des positions courrantes du trader au format JSON
        *    Role : Obtenir la liste des positions courrantes du trader au format JSON
        *    Fiabilite : Possibilité de lever une Exception
        */
        private List<JToken> GetPositionsJSON()
        {
            Log.TraceInformation("Trader.GetPositionsJSON", "Appel");

            var request = new RestRequest("/bapi/futures/v1/public/future/leaderboard/getOtherPosition");

            try
            {
                this.proxyList.WaitOne();
                this.restClient.Options.Proxy = this.proxyList.getProxy();
            }
            finally
            {
                Console.WriteLine("6");
                this.proxyList.ReleaseMutex();
            }

            Console.WriteLine("7");

            request.AddJsonBody(new {
                this.encryptedUid,
                tradeType = "PERPETUAL"
            });

            for(int i = 1; i <= 5; i++)
            {
                try
                {
                    var response = this.restClient.Post(request);

                    JObject jsonResponse;

                    if (response.Content != null)
                    {
                        jsonResponse = JObject.Parse(response.Content);

                        if (jsonResponse["data"] != null && jsonResponse["data"]!["otherPositionRetList"] != null)
                        {
                            return jsonResponse["data"]!["otherPositionRetList"]!.Children().ToList();
                        }
                    }

                    throw new Exception();

                }
                catch(Exception)
                {
                    Console.WriteLine("Trader --> Impossible de récupérer les positions du Trader --> Essai numéro " + i.ToString());
                    Log.TraceWarning("Trader.GetPositionsJSON", "Impossible de récupérer les positions du Trader --> Essai numéro " + i.ToString());
                }
            }

            Log.TraceError("Trader.GetPositionsJSON", "Impossible de récupérer les positions du Trader");
            throw new Exception("GetPositionsJSON : Impossible de récupérer les positions du Trader");
        }

        /*
        *    Nom : GetPositions
        *    Retour : la liste des positions courrantes du trader
        *    Role : Obtenir la liste des positions courrantes du trader
        *    Fiabilite : Possibilité de lever une Exception
        */
        private List<Position> GetPositions()
        {
            Log.TraceInformation("Trader.GetPositions", "Appel");

            //peut lever une erreur
            List<JToken> liste = this.GetPositionsJSON();

            List<Position> positions = new ();

            foreach (JToken token in liste)
            {
                positions.Add(new Position(token));
            }

            return positions;
        }

        /*
        *    Nom : IsInTheList
        *    Retour : booléen indiquant si une position est dans une liste spécifié, true : la position est dans la liste, false : la position n'est pas dans la liste
        *    Paramètre E : la position pour laquelle on veut tester la présence dans une liste et cette liste
        *    Role : Indiquer si une position est dans une liste spécifié
        *    Fiabilite : sure
        */
        private static bool IsInTheList(Position aPosition, List<Position> aPositionList)
        {
            foreach (Position position in aPositionList)
            {
                if (aPosition.isSamePosition(position))
                {
                    return true;
                }
            }
            return false;
        }

        /*
        *    Nom : PrepareSize
        *    Retour : double indiquant la taille de la position respectant la forme sur Bybit
        *    Paramètre E : la taille de la position à préparer et le symbole pour lequel on prépare la taille
        *    Role : Préparer une taille au format Bybit
        *    Fiabilite : possibilité de lever une BybitException ou Exception
        */
        private double PrepareSize(double size,string symbol)
        {
            Log.TraceInformation("Trader.PrepareSize", "Appel");

            double minimum;
            double step;

            //peut lever une erreur
            this.bybitManager.GetSymbolInfo(symbol,out minimum,out step);

            if (minimum < step)
            {
                minimum = step;
            }

            if (size < minimum)
            {
                return minimum;
            }

            if (step < 1)
            {
                int nbDec = (int)Math.Round(Math.Log(1 / step, 10));

                return Math.Round(size, nbDec);
            }

            return Math.Round(size);            
        }

        /*
        *    Nom : UpdateCoef
        *    Role : Mettre à jour le coefficient qui permet d'obtenir la taille de notre position en faisant coef*taille du trader
        *    Fiabilite : possibilité de lever une BybitException ou Exception
        */
        private void UpdateCoef()
        {
            Log.TraceInformation("Trader.UpdateCoef", "Appel");

            List<Position> positions = this.oldPositions.Join(this.oldPositions).Join(this.botPositions);

            foreach(Position position in positions)
            {
                this.sizeList.add(new SizeFormat { size = position.size * position.entryPrice, timestamp = position.updateTimeStamp });
            }

            this.sizeList.save();

            double mediane = this.sizeList.calculateMedian();

            //peut lever une erreur (BybitException ou Exception)
            double balance = this.bybitManager.GetBalanceUSD();

            //decommenter pour afficher la médiane du trader
            //Console.WriteLine(mediane);

            Log.TraceInformation("Trader.UpdateCoef", "mediane : " + mediane.ToString(CultureInfo.InvariantCulture));
            Log.TraceInformation("Trader.UpdateCoef", "balance : " + balance.ToString(CultureInfo.InvariantCulture));

            this.coef = this.pourcentage / 100 * balance / mediane;
        }

        /*
        *    Nom : GetSize
        *    Retour : double indiquant la taille pour notre position au format Bybit
        *    Paramètre E : la taille de la position du trader et le symbole de la position en question
        *    Role : Obtenir la taille au format Bybit pour prendre une position
        *    Fiabilite : possibilité de lever une BybitException ou Exception
        */
        private double GetSize(double size,string symbol)
        {
            Log.TraceInformation("Trader.GetSize", "Appel");

            //peut lever une erreur (BybitException ou Exception)
            this.UpdateCoef();

            double unPrepared = this.coef * size;

            //peut lever une erreur (BybitException ou Exception)
            return this.PrepareSize(unPrepared, symbol);
        }

        /*
        *    Nom : StartFollowing
        *    Paramètre E : la liste des positions courrantes du trader
        *    Role : Remplir oldPositions de toute les positions du trader (première itération, on ne veut pas prendre des trades obsolètes)
        *    Fiabilite : sure
        */
        private void StartFollowing(List<Position> traderPositions)
        {
            Log.TraceInformation("Trader.StartFollowing", "Appel");

            foreach (Position position in traderPositions)
            {
                if (!this.invalidSymbol.isInTheList(position.symbol))
                {
                    this.oldPositions.Add(position);
                }
            }
        }

        /*
        *    Nom : UpdateOldPositions
        *    Paramètre E : la liste des positions courrantes du trader
        *    Role : Retirer de oldPositions les positions qui ne sont plus dans les positions courrantes du trader
        *    Fiabilite : sure
        */
        private void UpdateOldPositions(List<Position> traderPositions)
        {
            Log.TraceInformation("Trader.UpdateOldPositions", "Appel");

            int i = 0;

            while (i < this.oldPositions.Count)
            {
                if (!Trader.IsInTheList(this.oldPositions.ElementAt(i), traderPositions))
                {
                    Console.WriteLine("UpdateOldPositions : suppression du symbol de oldPositions : "+ this.oldPositions.ElementAt(i).symbol);
                    this.oldPositions.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        /*
        *    Nom : UpdateUntakedPositions
        *    Paramètre E : la liste des positions courrantes du trader
        *    Role : Ajouter à untakedPositions les positions courrantes du trader qui ne sont pas dans oldPositions, ni dans botPositions et qui n'ont pas un symbole invalide
        *    Fiabilite : sure
        */
        private void UpdateUntakedPositions(List<Position> traderPositions)
        {
            Log.TraceInformation("Trader.UpdateUntakedPositions", "Appel");

            foreach (Position position in traderPositions)
            {
                if (!Trader.IsInTheList(position, this.oldPositions) && !Trader.IsInTheList(position, this.botPositions) && !this.invalidSymbol.isInTheList(position.symbol))
                {
                    Console.WriteLine("UpdateUntakedPositions : ajout de la devise a untakedPositions : " + position.symbol);
                    this.untakedPositions.Add(position);
                }
            }
        }

        /*
        *    Nom : UpdateUnclosedPositions
        *    Paramètre E : la liste des positions courrantes du trader
        *    Role : Déplacer dans unclosedPositions les positions de botPositions qui ne sont plus dans les positions courrantes du trader
        *    Fiabilite : sure
        */
        private void UpdateUnclosedPositions(List<Position> traderPositions)
        {
            Log.TraceInformation("Trader.UpdateUnclosedPositions", "Appel");

            int i = 0;

            while ( i < this.botPositions.Count)
            {
                if (!Trader.IsInTheList(this.botPositions.ElementAt(i),traderPositions))
                {
                    Console.WriteLine("UpdateUnclosedPositions : ajout de la devise a unclosedPositions : " + this.botPositions.ElementAt(i).symbol);

                    this.unclosedPositions.Add(this.botPositions.ElementAt(i));

                    Console.WriteLine("UpdateUnclosedPositions : suppression devise de botPositions : " + this.botPositions.ElementAt(i).symbol);

                    this.botPositions.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        /*
        *    Nom : UpdateUnalteredPositions
        *    Paramètre E : la liste des positions courrantes du trader
        *    Role : Ajouter à unalteredPositions les positions qui sont dans les positions du robot comme dans celle du trader mais qui n'ont pas la même taille
        *    Fiabilite : sure
        */
        private void UpdateUnalteredPositions(List<Position> traderPositions)
        {
            Log.TraceInformation("Trader.UpdateUnalteredPositions", "Appel");

            foreach (Position traderPosition in traderPositions)
            {
                foreach(Position botPosition in this.botPositions)
                {
                    if(traderPosition.isSamePosition(botPosition))
                    {
                        if(traderPosition != botPosition)
                        {
                            double diff = traderPosition.size - botPosition.size;

                            if(traderPosition.side == "Sell")
                            {
                                diff = -diff;
                            }

                            Position positionToAlter = new (traderPosition.symbol, traderPosition.entryPrice,traderPosition.updateTimeStamp,diff.ToString(CultureInfo.InvariantCulture));

                            if (botPosition.side == "Buy")
                            {
                                positionToAlter.positionIdx= 1;
                            }
                            else
                            {
                                positionToAlter.positionIdx = 2;
                            }

                            positionToAlter.coef = botPosition.coef;

                            if((diff > 0 && traderPosition.side == "Buy")||(diff < 0 && traderPosition.side == "Sell"))
                            {
                                Console.WriteLine("UpdateUnalteredPositions(+) : ajout de la devise a unalteredPositions : " + positionToAlter.symbol + " : " + botPosition.size.ToString(CultureInfo.InvariantCulture) + "-->" + traderPosition.size.ToString(CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                Console.WriteLine("UpdateUnalteredPositions(-) : ajout de la devise a unalteredPositions : " + positionToAlter.symbol + " : " + botPosition.size.ToString(CultureInfo.InvariantCulture) + "-->" + traderPosition.size.ToString(CultureInfo.InvariantCulture));
                            }

                            this.unalteredPositions.Add(positionToAlter);
                        }
                    }
                }
            }
        }

        /*
        *    Nom : TakePositions
        *    Role : Prendre chaque position qui sont dans untakedPositions, si cela échoue pour une alors cela la met dans oldPositions
        *    Fiabilite : sure
        */
        private void TakePositions()
        {
            Log.TraceInformation("Trader.TakePositions", "Appel");

            while (this.untakedPositions.Count > 0)
            {
                double size;

                //possibilité de lever une BybitException ou Exception
                try
                {
                    size = this.GetSize(this.untakedPositions.ElementAt(0).size, this.untakedPositions.ElementAt(0).symbol);
                    Log.TraceInformation("Trader.TakePositions", "size : " + size.ToString(CultureInfo.InvariantCulture));
                }
                catch(BybitException e)
                {
                    if ((e.code == 10001 || e.code == 1) && e.Message.Contains("symbol invalid"))
                    {
                        Console.WriteLine("Ce symbol n'est pas disponible sur bybit");
                        this.invalidSymbol.add(this.untakedPositions.ElementAt(0).symbol);
                        this.untakedPositions.RemoveAt(0);
                        return;
                    }

                    Log.TraceWarning("Trader.TakePositions", "Erreur lors de l'obtention de la taille, position déplacé dans oldPositions\nMessage : " + e.Message);
                    Console.WriteLine("Trader --> TakePositions --> Erreur lors de l'obtention de la taille\nCode d'erreur : " + e.code.ToString()+ "\nMessage : " + e.Message);
                    this.oldPositions.Add(this.untakedPositions.ElementAt(0));
                    this.untakedPositions.RemoveAt(0);
                    return;
                }
                catch(Exception e)
                {
                    Log.TraceWarning("Trader.TakePositions", "Erreur lors de l'obtention de la taille, position déplacé dans oldPositions\nMessage : " + e.Message);
                    Console.WriteLine("Trader --> TakePositions --> Erreur lors de l'obtention de la taille\nMessage : " + e.Message);
                    this.oldPositions.Add(this.untakedPositions.ElementAt(0));
                    this.untakedPositions.RemoveAt(0);
                    return;
                }

                int retour = this.bybitManager.TakePosition(this.untakedPositions.ElementAt(0), size);

                if(retour == 0)
                {
                    this.untakedPositions.ElementAt(0).coef = this.coef;
                    this.botPositions.Add(this.untakedPositions.ElementAt(0));
                    Console.WriteLine("Trader --> TakePositions --> position ouverte : " + this.untakedPositions.ElementAt(0).symbol);
                    Log.TraceInformation("Trader.TakePositions", "position ouverte : " + this.untakedPositions.ElementAt(0).symbol);
                }
                else if (retour == 1)
                {
                    Console.WriteLine("Trader --> TakePositions --> Erreur symbole, symbole ajouté à la liste des symboles invalides");
                    Log.TraceWarning("Trader.TakePositions", "Erreur symbole, symbole ajouté à la liste des symboles invalides");
                    this.invalidSymbol.add(this.untakedPositions.ElementAt(0).symbol);
                }
                else
                {
                    Console.WriteLine("Trader --> TakePositions --> Erreur lors de la prise de la position, position déplacé dans oldPositions");
                    Log.TraceWarning("Trader.TakePositions", "Erreur lors de la prise de la position, position déplacé dans oldPositions");
                    this.oldPositions.Add(this.untakedPositions.ElementAt(0));
                }
                this.untakedPositions.RemoveAt(0);
            }
        }

        /*
        *    Nom : ClosePositions
        *    Role : Fermer chaque position qui sont dans unclosedPositions
        *    Fiabilite : sure
        */
        private void ClosePositions()
        {
            Log.TraceInformation("Trader.ClosePositions", "Appel");

            while(this.unclosedPositions.Count > 0)
            {
                int retour = this.bybitManager.ClosePosition(this.unclosedPositions.ElementAt(0));

                if(retour == 0) 
                {
                    Console.WriteLine("Trader --> ClosePositions --> position fermée : " + this.unclosedPositions.ElementAt(0).symbol);
                    Log.TraceInformation("Trader.ClosePositions", "position fermée : " + this.unclosedPositions.ElementAt(0).symbol);
                    this.unclosedPositions.RemoveAt(0);
                }
                else
                {
                    Console.WriteLine("Trader --> TakePositions --> Erreur lors de la fermeture de la position");
                    Log.TraceError("Trader.ClosePositions", "Erreur lors de la fermeture de la position");
                }
            }
        }

        /*
        *    Nom : AlterPositions
        *    Role : Modifier chaque position qui sont dans unalteredPositions
        *    Fiabilite : sure
        */
        private void AlterPositions(List<Position> traderPositions)
        {
            Log.TraceInformation("Trader.AlterPositions", "Appel");

            while (this.unalteredPositions.Count > 0)
            {
                Console.WriteLine("AlterPositions :     size:" + this.unalteredPositions.ElementAt(0).size.ToString(CultureInfo.InvariantCulture), "  Coef :", this.unalteredPositions.ElementAt(0).coef.ToString(CultureInfo.InvariantCulture));

                double size = this.unalteredPositions.ElementAt(0).size * this.unalteredPositions.ElementAt(0).coef;

                try
                {
                    size = this.PrepareSize(size, this.unalteredPositions.ElementAt(0).symbol);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Trader --> AlterPositions --> Impossible de préparer la taille de la position\nMessage : " + ex.Message);
                    Log.TraceWarning("Trader.AlterPositions", "Impossible de préparer la taille de la position\nMessage : " + ex.Message);
                    continue;
                }

                int retour = this.bybitManager.TakePosition(this.unalteredPositions.ElementAt(0),size);

                if(retour == 0)
                {
                    Console.WriteLine("position modifie : " + this.unalteredPositions.ElementAt(0).symbol);
                    Log.TraceWarning("Trader.AlterPositions", "Position modifie : "+ this.unalteredPositions.ElementAt(0).symbol);
                }
                else
                {
                    Console.WriteLine("Trader --> AlterPositions --> Erreur lors de la modification de la taille de la position, la position ne sera pas modifié");
                    Log.TraceWarning("Trader.AlterPositions", "Erreur lors de la modification de la taille de la position, la position ne sera pas modifié");
                }

                foreach (Position botPosition in this.botPositions)
                {
                    foreach (Position traderPosition in traderPositions)
                    {
                        if (botPosition.isSamePosition(traderPosition))
                        {
                            botPosition.size = traderPosition.size;
                            break;
                        }
                    }
                }

                this.unalteredPositions.RemoveAt(0);
            }
        }


        /*
        *    Nom : verifyEncryptedUid
        *    Retour : booléen indiquant si les informations sont valides
        *    Role : vérifier la validité des informations
        *    Fiabilite : Possibilité de lever une Exception
        */
        static public bool verifyEncryptedUid(string anEncryptedUid)
        {
            Log.TraceInformation("Trader.verifyEncryptedUid", "Appel");

            var request = new RestRequest("/bapi/futures/v1/public/future/leaderboard/getOtherPosition");

            request.AddJsonBody(new
            {
                encryptedUid = anEncryptedUid,
                tradeType = "PERPETUAL"
            });

            RestClient restClient = new RestClient("http://www.binance.com/");

            Dictionary<string, string> headers = new Dictionary<string, string> {
                {"authority","www.binance.com" },
                {"accept","*/*" },
                {"accept-encoding","gzip, deflate, br" },
                {"accept-language","fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7" },
                {"origin","https://www.binance.com" },
                {"clienttype","web" },
                {"cache-control","max-age=0" },
                {"sec-fetch-dest","document" },
                {"sec-fetch-mode","navigate" },
                {"sec-fetch-site","same-origin" },
                {"sec-fetch-user","\"?1\"" },
                {"upgrade-insecure-requests","\"1\"" },
                {"user-agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36" },
                {"referer","https://www.binance.com/en/futures-activity/leaderboard/user?encryptedUid=" + anEncryptedUid}
            };

            restClient.AddDefaultHeaders(headers);

            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    var response = restClient.Post(request);

                    JObject jsonResponse;

                    if (response.Content != null)
                    {
                        jsonResponse = JObject.Parse(response.Content);

                        if (jsonResponse["data"] != null && jsonResponse["data"]!["otherPositionRetList"] != null)
                        {
                            var stck = jsonResponse["data"]!["otherPositionRetList"]!;

                            return !(stck.ToString() == "");
                        }
                    }

                    throw new Exception();

                }
                catch (Exception)
                {
                    Console.WriteLine("Trader --> Impossible de récupérer les positions du Trader --> Essai numéro " + i.ToString());
                    Log.TraceWarning("Trader.GetPositionsJSON", "Impossible de récupérer les positions du Trader --> Essai numéro " + i.ToString());
                }
            }

            Log.TraceError("Trader.GetPositionsJSON", "Impossible de récupérer les positions du Trader");
            throw new Exception("GetPositionsJSON : Impossible de récupérer les positions du Trader");
        }

        /*
        *    Nom : verifyEncryptedUid
        *    Retour : booléen indiquant si les informations sont valides
        *    Role : vérifier la validité des informations
        *    Fiabilite : Possibilité de lever une Exception
        */

        public State GetState() { return state; }


        //_____________________________________________________FONCTIONS DE TESTS________________________________________________________________

        private void Save(List<Position> positionList)
        {
            try
            {
                //On converti notre liste en JSON
                byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(positionList);
                //On écrite au format utf8 le JSON dans le fichier de sauvegarde
                File.WriteAllBytes("fakePosition.dat", jsonUtf8Bytes);
            }
            catch (Exception)
            {
                Console.WriteLine("Save of FakePosition --> Impossible d'enregistrer les positions");
            }
        }


        public void SetPositions()
        {
            List<Position> liste = new ();

            liste.Add(new Position("XMRUSDT", 151.8738075949, "1677511286276", "1.975"));
            liste.Add(new Position("XMRUSDT", 150.1962443665, "1677437066206", "-3.994"));
            liste.Add(new Position("NEARBUSD", 2.456474649141, "1677357910551", "3256.3"));
            liste.Add(new Position("NEARBUSD", 2.303791390538, "1677510482268", "-1735.3"));
            liste.Add(new Position("XLMUSDT", 0.1001829361641, "1668028264103", "209600"));

            this.Save(liste);
        }

        private List<Position> Load()
        {

            // On vérifie que le fichier existe
            if (File.Exists("fakePosition.dat"))
            {

                try
                {
                    //on récupere le JSON (en UTF8)
                    byte[] jsonUtf8Bytes = File.ReadAllBytes("fakePosition.dat");
                    //on crée un lecteur UTF8
                    var utf8Reader = new Utf8JsonReader(jsonUtf8Bytes);
                    //on charge notre objet
                    return JsonSerializer.Deserialize<List<Position>>(ref utf8Reader)!;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Un fichier qui contient des informations sur des proxies semblent exister mais il est impossible de charger les informations\nDétails : " + e.ToString());
                }
            }

            throw new Exception("Erreur Load de fakePositions");
        }

        private List<Position> GetFakePositions()
        {
            return this.Load();
        }
    }
}
