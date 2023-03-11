/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      26/02/2023 -> codage et commentaires de l'intégralité
 *  Role : Définition de la classe SizeList qui sert à sauvergader les tailles des positions pris par un trader et calculer la médiane
 */

using BuyEnhancedServer.Proxies;
using BuyEnhancedServerv2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuyEnhancedServer.Binance
{
    //class hérité de IComparable pour être trié en fonction de la taille (grâce à l'implémentation via l'attribut size)
    /*
        *    Nom : SizeFormat
        *    Role : Classe qui sert à définir le format d'enregistrement des tailles positions
        */
    class SizeFormat : IComparable
    {
        //taille de la position en euro
        public double size { get; set; }

        //timestamp qui marque la milliseconde à laquelle la position a été prise (sert d'identifiant)
        public string? timestamp { get; set; }

        //implémentation de la classe IComparable (utilisé par List<SizeFormat>.Sort)
        public int CompareTo(object? obj)
        {
            if (obj == null) return 1;

            SizeFormat otherSizeFormat = (SizeFormat)obj;

            if (otherSizeFormat != null)
            {
                return this.size.CompareTo(otherSizeFormat.size);
            }
            else
            {
                throw new ArgumentException("Object is not a SizeFormat");
            }
        }
    }


    /*
        *    Nom : SizeList
        *    Role : Classe qui sert à faciliter la maintenance des tailles dans l'historique des positions d'un trader (ajout, enregistrement et récupération, calcul de la médianne)
    */
    internal class SizeList
    {

        // nom du fichier de sauvegarde de la liste
        //C:\Users\gilda\source\repos\BuyEnhancedServer\BuyEnhancedServer\bin\Debug\net6.0\<encryptedUid>.dat
        private string DATA_FILENAME;

        //la liste de taille
        private List<SizeFormat> list;


        /*
        *    Nom : SizeList
        *    Role : Créer un objet de type ProxyList
        *    Fiabilite : sure (très peu de chance que cela échoue)
        */
        public SizeList(string encryptedUid)
        {
            Log.TraceInformation("SizeList", "Appel du constructeur");

            //on nomme le fichier au nom passé en paramètre et rajoute l'extension .dat
            this.DATA_FILENAME = encryptedUid + ".dat";

            //Création de la liste pour stocker les proxies
            this.list = new List<SizeFormat>();

            //on charge la sauvergarde
            this.load();
        }

        /*
        *    Nom : Save
        *    Role : Enregistrer l'objet this.list dans le fichier
        *    Fiabilite : sure (très peu de chance que cela échoue)
        */
        public void save()
        {
            Log.TraceInformation("SizeList.Save", "Appel");

            try
            {
                //On converti notre liste en JSON
                byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(this.list);
                //On écrite au format utf8 le JSON dans le fichier de sauvegarde
                File.WriteAllBytes(this.DATA_FILENAME, jsonUtf8Bytes);
            }
            catch (Exception)
            {
                Console.WriteLine("SizeList --> Impossible d'enregistrer les tailles");
                Log.TraceError("SizeList.Save", "Impossible d'enregistrer les tailles");
            }
        }


        /*
        *    Nom : Load
        *    Role : Charger les informations sur les tailles sauvegardées dans le fichier dans l'objet this.list
        *    Fiabilite : sure (très peu de chance que cela échoue)
        */
        private void load()
        {
            Log.TraceInformation("SizeList.Load", "Appel");

            // On vérifie que le fichier existe
            if (File.Exists(DATA_FILENAME))
            {

                try
                {
                    //on récupere le JSON (en UTF8)
                    byte[] jsonUtf8Bytes = File.ReadAllBytes(this.DATA_FILENAME);
                    //on crée un lecteur UTF8
                    var utf8Reader = new Utf8JsonReader(jsonUtf8Bytes);
                    //on charge notre objet
                    this.list = JsonSerializer.Deserialize<List<SizeFormat>>(ref utf8Reader)!;
                }
                catch (Exception)
                {
                    Console.WriteLine("Un fichier qui contient des informations sur des tailles d'un trader semblent exister mais il est impossible de charger les informations");
                    Log.TraceError("SizeList.Load", "Un fichier qui contient des informations sur des tailles d'un trader semblent exister mais il est impossible de charger les informations");
                }

            }

        }

        /*
        *    Nom : add
        *    Paramètre E : un SizeFormat à ajouter à this.list
        *    Role : Permettre d'ajouter une taille à la liste (uniquement si cette position n'a pas déjà été prise en compte)
        *    Fiabilite : sure 
        */
        public void add(SizeFormat size)
        {
            if (!this.isInTheList(size.timestamp!))
            {
                this.list.Add(size);
            }
        }

        /*
        *    Nom : IsInTheList
        *    Retour : booléen indiquant si la taille a déjà été prise en compte, true : déjà prise en compte , false :  non-prise en compte
        *    Paramètre E : un timestamp qui permet d'identifier les positions
        *    Role : Indiquer si la tailler a déjà été prise en compte ou non  
        *    Fiabilite : sure 
        */
        private bool isInTheList(string timestamp)
        {
            foreach (SizeFormat size in this.list) 
            {
                if (size.timestamp == timestamp)
                {
                    return true;
                }
            }

            return false;
        }

        /*
        *    Nom : calculateMedian
        *    Retour : double qui indique la taille médiane des positions du trader
        *    Role : donner la taille médiane des positions d'un trader en euro
        *    Fiabilite : sure 
        */
        public double calculateMedian()
        {
            Log.TraceInformation("SizeList.calculateMedian", "Appel");

            this.list.Sort();

            if(this.list.Count < 1)
            {
                Log.TraceWarning("SizeList.calculateMedian", "La liste est vide");
                throw new Exception("SizeList --> calculateMedian --> La liste est vide");
            }
            if(this.list.Count % 2 != 0) 
            {
                return this.list.ElementAt((int)(this.list.Count-1)/2).size;
            }

            int mid1 = (int)(this.list.Count/2)-1;
            int mid2 = (int)this.list.Count / 2;

            return (this.list.ElementAt(mid1).size + this.list.ElementAt(mid2).size) / 2;
        }

        /*
        *    Nom : afficher
        *    Role : Afficher la liste des tailles (debug)
        *    Fiabilite : sure 
        */
        private void afficher()
        {
            foreach (SizeFormat size in this.list)
            {
                Console.WriteLine("Size : " + size.size + "    Timestamp : " + size.timestamp);
            }
        }
    }
}
