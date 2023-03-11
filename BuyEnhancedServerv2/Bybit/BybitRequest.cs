/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      25/02/2023 -> codage et commentaire de l'intégralité
 *  Role : Définition de la classe BybitRequest qui facilite la création des requêtes pour la communication avec l'API Bybit
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace BuyEnhancedServer.Bybit
{
    /*
        *    Nom : BybitRequest
        *    Role : Définition de la classe BybitRequest qui facilite la création des requêtes pour la communication avec l'API Bybit
    */
    public class BybitRequest
    {
        public Object? obj { get; }
        public string? paramStr { get; }
        public bool isGetRequest { get; }

        /*
        *    Nom : BybitRequest
        *    Paramètre E : un objet quelconque dont les attribut corresponde au champs de la requête en JSON
        *    Role : Constructeur de BybitRequest à partir d'un objet quelconque (pour les requête en POST)
        *    Fiabilité : Sure
        */
        public BybitRequest(Object anObject)
        {
            this.obj = anObject;
            this.isGetRequest= false;
        }

        /*
        *    Nom : BybitRequest
        *    Paramètre E : une chaine de caractère représentant les paramètres de la requêt
        *    Role : Constructeur de BybitRequest à partir d'une chaine de caractère représentant les paramètres de la requête (pour les requête en GET)
        *    Fiabilité : Sure
        */
        public BybitRequest(string aParamStr)
        {
            this.paramStr = aParamStr;
            this.isGetRequest = true;
        }

        /*
        *    Nom : getSign
        *    Retour : chaine de caractère qui correspond à la signature de la requête
        *    Paramètre E : la clé secrète d'API, le timestamp actuel au format universel, la clé API et le paramètre recvWindow
        *    Role : Obtenir la signature de la requête
        *    Fiabilité : Sure
        */
        public string getSign(string secret, double timestamp, string apiKey, int recvWindow)
        {
            //chaine pour ranger la signature
            string sign;

            //chaine à partir de laquelle on obtient la signature
            string paramStr;

            //on la remplie selon la documentation (on gère ici, si elle est POST ou GET)
            if (isGetRequest)
            {
                paramStr = timestamp.ToString(CultureInfo.InvariantCulture) + apiKey + recvWindow + this.paramStr;
            }
            else
            {
                paramStr = timestamp.ToString(CultureInfo.InvariantCulture) + apiKey + recvWindow + JsonSerializer.Serialize(this.obj);
            }
            
            //décommenter pour afficher la requête à partir de laquelle la signature a été générée
            //Console.Write("origin string : ");
            //Console.WriteLine(paramStr);

            //obtenir la signature à partir de la clé secrète et le paramètre en chaine de caractère
            sign = Encryption.CreateSignature(secret, paramStr);

            return sign;
        }
    }
}
