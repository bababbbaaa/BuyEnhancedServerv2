/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      18/02/2023 -> codage et commentaires des fonctions ProxyList,getIndex,GetProxy
 *  Role : Définition de la classe ProxyList qui sert à gérer la distribution des proxies
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BuyEnhancedServerv2.Utils;

namespace BuyEnhancedServer.Proxies
{
    /*
        *    Nom : ProxyList
        *    Role : Classe singleton qui sert à faciliter la maintenance des proxies (distribution, enregistrement et récupération)
        *    Inspiration : https://www.techcoil.com/blog/how-to-Save-and-Load-objects-to-and-from-file-in-c/
        */
    internal class ProxyList
    {
        //l'instance singleton
        private static ProxyList? proxyList;

        // nom du fichier de sauvegarde de la liste
        //C:\Users\gilda\source\repos\BuyEnhancedServer\BuyEnhancedServer\bin\Debug\net6.0\proxyList.dat
        private const string DATA_FILENAME = "proxyList.dat";

        //la liste de proxy
        private List<WebProxy> list;

        //le mutex pour sécuriser l'accès à la liste
        private Mutex mutex;

        private int cursor;
        //ajouter le mutex d'accession à la liste

        /*
        *    Nom : Instance
        *    Role : Créer l'unique objet statique de type ProxyList
        *    Fiabilité : Sure
        */
        public static ProxyList Instance()
        {
            Log.TraceInformation("ProxyList.Instance", "Appel");

            if (ProxyList.proxyList == null)
            {
                ProxyList.proxyList = new ProxyList();
            }

            return ProxyList.proxyList;
        }

        /*
        *    Nom : ProxyList
        *    Role : Créer un objet de type ProxyList (privée pour pouvoir obligé l'instanciation avec la méthode Instance())
        *    Fiabilité : Sure
        */
        private ProxyList() 
        {
            Log.TraceInformation("ProxyList", "Appel du constructeur");

            //Création de la liste pour stocker les proxies
            this.list = new List<WebProxy>();

            //on charge la sauvergarde
            this.load();

            if(this.list.Count == 0)
            {
                this.list.Add(null!);
            }

            //Afficher la liste et sa longueur au démarrage (déboggage)
            Console.WriteLine(this.list.Count);
            this.afficher();

            //initialisation du curseur au premier élément de la liste
            this.cursor = 0;

            //initialisation du mutex
            this.mutex = new Mutex();
        }

        /*
        *    Nom : Save
        *    Role : Enregistrer l'objet this.list dans le fichier
        *    Fiabilité : Sure (très faible chance d'échouer)
        */
        public void save()
        {
            Log.TraceInformation("ProxyList.Save", "Appel");

            try
            {
                //On converti notre liste en JSON
                byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(this.list);
                //On écrite au format utf8 le JSON dans le fichier de sauvegarde
                File.WriteAllBytes(ProxyList.DATA_FILENAME, jsonUtf8Bytes);
            }
            catch (Exception)
            {
                Console.WriteLine("ProxyList --> Impossible d'enregistrer les proxies");
                Log.TraceError("ProxyList.Save", "Impossible d'enregistrer les proxies");
            }
        }


        /*
        *    Nom : Load
        *    Role : Charger les informations sur les proxies sauvegardées dans le fichier dans l'objet this.list
        *    Fiabilité : Sure (très faible chance d'échouer)
        */
        private void load()
        {
            Log.TraceInformation("ProxyList.Load", "Appel");

            // On vérifie que le fichier existe
            if (File.Exists(DATA_FILENAME))
            {

                try
                {
                    //on récupere le JSON (en UTF8)
                    byte[] jsonUtf8Bytes = File.ReadAllBytes(ProxyList.DATA_FILENAME);
                    //on crée un lecteur UTF8
                    var utf8Reader = new Utf8JsonReader(jsonUtf8Bytes);
                    //on charge notre objet
                    this.list = JsonSerializer.Deserialize<List<WebProxy>>(ref utf8Reader)!;
                }
                catch (Exception)
                {
                    Console.WriteLine("Un fichier qui contient des informations sur des proxies semblent exister mais il est impossible de charger les informations");
                    Log.TraceError("ProxyList.Save", "Un fichier qui contient des informations sur des proxies semblent exister mais il est impossible de charger les informations");
                }

            }

        }

        /*
        *    Nom : add
        *    Paramètre E : un WebProxy à ajouter à this.list
        *    Role : Permettre d'ajouter une proxy à la liste
        *    Fiabilité : Sure
        */
        public void add(WebProxy proxy)
        {
            this.list.Add(proxy);
        }

        /*
        *    Nom : remove
        *    Paramètre E : un entier qui correspond à l'index de l'élément à supprimer
        *    Role : Permettre de supprimer un proxy de la liste
        *    Fiabilité : Sure
        */
        public void remove(int i)
        {
            this.list.RemoveAt(i);
        }

        /*
        *    Nom : elementAt
        *    Retour : Entier qui correspond à la taille de la liste
        *    Role : Permettre de connaitre la taille de la liste
        *    Fiabilité : Sure
        */
        public int count()
        {
            return this.list.Count;
        }

        /*
        *    Nom : elementAt
        *    Retour : WebProxy stocké à l'index i de la liste
        *    Paramètre E : un entier qui correspond à l'index de l'élément à obtenir
        *    Role : Permettre d'accèder à un élément de la liste
        *    Fiabilité : Sure
        */
        public WebProxy elementAt(int i)
        {
            return this.list.ElementAt(i);
        }

        /*
        *    Nom : IsInTheList
        *    Retour : true si le proxy est déjà dans la liste, false si il ne l'est pas encore
        *    Paramètre E : un WebProxy
        *    Role : Indiquer si le proxy est déjà dans la liste ou pas
        *    Fiabilité : sure
        */
        public bool isInTheList(WebProxy testProxy)
        {
            //si un proxy de la liste de proxy a la meme adresse et le meme port alors on considère qu'ils sont égaux
            foreach (WebProxy proxy in this.list)
            {
                if(proxy != null)
                {
                    if (proxy.Address!.Host == testProxy.Address!.Host && proxy.Address.Port == testProxy.Address.Port)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /*
        *    Nom : afficher
        *    Role : Afficher la liste des proxies (debug)
        *    Fiabilité : Sure
        */
        private void afficher()
        {
            foreach (WebProxy proxy in this.list)
            {
                if(proxy != null)
                {
                    Console.WriteLine("Adresse : " + proxy.Address!.Host + "    Port : " + proxy.Address.Port.ToString());
                }
            }
        }

        /*
        *    Nom : getIndex
        *    Retour : L'index du curseur pour récuperer un proxy dans la liste
        *    Role : Décaler le curseur de la liste et le retourner
        *    Fiabilité : Sure
        */
        private int getIndex() 
        {
            if (this.cursor >= this.list.Count - 1)
            {
                this.cursor = 0;
            }
            else
            {
                this.cursor++;
            }

            return cursor; 
        }

        /*
        *    Nom : GetProxy
        *    Retour : un nouveau WebProxy ou null si la liste est vide (attention le proxy null est quand meme utilisé et donc les requetes ne fonctionnent pas avec)
        *    Role : Donner un nouveau propxy
        *    Fiabilité : Sure (si cela échoue, il y a une erreur dans le code)
        */
        public WebProxy getProxy()
        {
            Log.TraceInformation("ProxyList.getProxy", "Appel");

            Console.WriteLine("1");
            int i;

            Console.WriteLine("2");
            i = this.getIndex();

            Console.WriteLine("3");
            if (i < this.list.Count)
            {
                Console.WriteLine("4");
                return this.list.ElementAt(i);
            }

            Console.WriteLine("-1");
            Log.TraceError("ProxyList.getProxy", "L'index fourni par getIndex est invalide");
            throw new Exception("L'index fourni par getIndex est invalide");
        }

        /*
        *    Nom : WaitOne
        *    Retour : un booléen indiquant si l'acquisition a réussi
        *    Role : Acquérir le mutex
        *    Fiabilité : Sure
        */
        public bool WaitOne()
        {
            return this.mutex.WaitOne();
        }

        /*
        *    Nom : ReleaseMutex
        *    Role : Libérer le mutex
        *    Fiabilité : Sure
        */
        public void ReleaseMutex()
        {
            this.mutex.ReleaseMutex();
        }
    }
}
