/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      27/02/2023 -> codage et commentaires de l'intégralité
 *  Role : Définition de la classe InvalidSymbolList qui sert à sauvegarder la liste des symboles invalides
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

namespace BuyEnhancedServer.Binance
{
    /*
        *    Nom : ProxyList
        *    Role : Classe singleton qui sert à faciliter la maintenance des symboles invalides (ajout, enregistrement et récupération)
        *    Inspiration : https://www.techcoil.com/blog/how-to-Save-and-Load-objects-to-and-from-file-in-c/
        */
    internal class InvalidSymbolList
    {
        //l'instance singleton
        private static InvalidSymbolList? invalidSymbolList;

        // nom du fichier de sauvegarde de la liste
        //C:\Users\gilda\source\repos\BuyEnhancedServer\BuyEnhancedServer\bin\Debug\net6.0\InvalidSymbol.dat
        private const string DATA_FILENAME = "InvalidSymbol.dat";

        //la liste de symbole
        private List<string> list;

        /*
        *    Nom : Instance
        *    Role : Créer l'unique objet statique de type InvalidSymbolList
        *    Fiabilité : sure
        */
        public static InvalidSymbolList Instance()
        {
            Log.TraceInformation("InvalidSymbolList.Instance", "Appel");

            if (InvalidSymbolList.invalidSymbolList == null)
            {
                InvalidSymbolList.invalidSymbolList = new InvalidSymbolList();
                Log.TraceInformation("InvalidSymbolList.Instance", "Instantiation de l'objet singleton InvalidSymbolList");
            }

            return InvalidSymbolList.invalidSymbolList;
        }

        /*
        *    Nom : InvalidSymbolList
        *    Role : Créer un objet de type InvalidSymbolList (privée pour pouvoir obligé l'instanciation avec la méthode Instance())
        *    Fiabilité : sure
        */
        private InvalidSymbolList()
        {
            Log.TraceInformation("InvalidSymbolList", "Appel du constructeur");

            //Création de la liste pour stocker les proxies
            this.list = new List<string>();

            //on charge la sauvergarde
            this.load();

            //Afficher la liste et sa longueur au démarrage (déboggage)
            //Console.WriteLine(this.list.Count);
            //this.afficher();
        }

        /*
        *    Nom : Save
        *    Role : Enregistrer l'objet this.list dans le fichier
        *    Fiabilité : sure (très faible chance que cela échoue)
        */
        private void save()
        {
            Log.TraceInformation("InvalidSymbolList.Save", "Appel");

            try
            {
                //On converti notre liste en JSON
                byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(this.list);
                //On écrite au format utf8 le JSON dans le fichier de sauvegarde
                File.WriteAllBytes(InvalidSymbolList.DATA_FILENAME, jsonUtf8Bytes);
            }
            catch (Exception)
            {
                Console.WriteLine("ProxyList --> Impossible d'enregistrer les proxies");
                Log.TraceError("InvalidSymbolList.Save", "Impossible d'enregistrer la liste de proxy");
            }
        }


        /*
        *    Nom : Load
        *    Role : Charger les informations sur les symboles sauvegardées dans le fichier dans l'objet this.list
        *    Fiabilité : sure (très faible chance que cela échoue)
        */
        private void load()
        {
            Log.TraceInformation("InvalidSymbolList.Load", "Appel");

            // On vérifie que le fichier existe
            if (File.Exists(DATA_FILENAME))
            {

                try
                {
                    //on récupere le JSON (en UTF8)
                    byte[] jsonUtf8Bytes = File.ReadAllBytes(InvalidSymbolList.DATA_FILENAME);
                    //on crée un lecteur UTF8
                    var utf8Reader = new Utf8JsonReader(jsonUtf8Bytes);
                    //on charge notre objet
                    this.list = JsonSerializer.Deserialize<List<string>>(ref utf8Reader)!;
                }
                catch (Exception)
                {
                    Console.WriteLine("Un fichier qui contient des informations la liste de symbole invalide semblent exister mais il est impossible de charger les informations");
                    Log.TraceError("InvalidSymbolList.Load", "Un fichier qui contient des informations la liste de symbole invalide semblent exister mais il est impossible de charger les informations");
                }

            }

        }

        /*
        *    Nom : IsInTheList
        *    Retour : un booléen indiquant si le symbole est déjà dans la liste, true : déjà dans la liste, false : pas encore dans la liste
        *    Paramètre E : un WebProxy à ajouter à this.list
        *    Role : Permettre d'ajouter une proxy à la liste
        *    Fiabilité : sure
        */
        public bool isInTheList(string aSymbol)
        {
            return this.list.Contains(aSymbol);
        }

        /*
        *    Nom : add
        *    Retour : aucun
        *    Paramètre E : un symbole à ajouter à this.list
        *    Role : Permettre d'ajouter une symbole à la liste
        *    Fiabilité : sure
        */
        public void add(string symbol)
        {
            if (!isInTheList(symbol))
            {
                this.list.Add(symbol);
                this.save();
            }
        }

        /*
        *    Nom : remove
        *    Retour : aucun
        *    Paramètre E : un entier qui correspond à l'index de l'élément à supprimer
        *    Role : Permettre de supprimer un symbole de la liste
        *    Fiabilité : sure
        */
        private void remove(int i)
        {
            this.list.RemoveAt(i);
        }

        /*
        *    Nom : afficher
        *    Role : Afficher la liste des symboles (debug)
        *    Fiabilité : sure
        */
        private void afficher()
        {
            foreach (string symbol in this.list)
            {
                Console.WriteLine(symbol);
            }
        }
    }
}
