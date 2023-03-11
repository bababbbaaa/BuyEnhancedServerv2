/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      21/02/2023 -> codage et commentaires de l'intégralité
 *  Role : Définition de la classe Position qui sert à définir à créer un type unique de position pour faciliter la manipulation
 */


using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BuyEnhancedServerv2.Utils
{
    /*
       *    Nom : Position
       *    Role : Définition de la classe Position qui facilite la manipulation des positions dans le code
   */
    internal class Position
    {
        //symbole de la position
        public string symbol { get; set; }

        //prix d'entrée de la position
        public double entryPrice { get; }

        //timestamp à laquelle la dernière modification a été effectué sur la position
        public string updateTimeStamp { get; }

        //coefficient qui permet d'obtenir la taille de notre position par rapport à celle du trader a qui appartient cette position (mySize = coef * traderSize)
        public double coef { get; set; }

        //sens de la position ("Buy" ou "Sell")
        public string side { get; }

        //taille de la position
        public double size { get; set; }

        //position
        public int positionIdx { get; set; }

        /*
        *    Nom : Position
        *    Paramètre E : le symbole, le prix d'entrée, l'updateTimeStamp et le montant (chaine de caractère)
        *    Role : Construteur qui sert à créer une position en données brute (pour prendre une position personnalisé)
        *    Fiabilité : sure
        */
        public Position(string aSymbol, double anEntryPrice, string anUpdateTimeStamp, string anAmount)
        {
            symbol = aSymbol;
            entryPrice = anEntryPrice;
            updateTimeStamp = anUpdateTimeStamp;
            coef = 0;

            if (anAmount.Contains("-"))
            {
                side = "Sell";
                positionIdx = 2;

                anAmount = anAmount.TrimStart('-');
            }
            else
            {
                side = "Buy";
                positionIdx = 1;
            }

            size = double.Parse(anAmount, CultureInfo.InvariantCulture);
        }

        /*
        *    Nom : Position
        *    Paramètre E : le symbole, le prix d'entrée, l'updateTimeStamp et le montant (chaine de caractère)
        *    Role : Construteur qui sert à créer une position à partir du JSON représentant une position (sert pour la sauvegarde, à des fins de test)
        *    Fiabilité : sure
        */
        [JsonConstructor]
        public Position(string symbol, double entryPrice, string updateTimeStamp, double coef, string side, double size, int positionIdx)
        {
            this.symbol = symbol;
            this.entryPrice = entryPrice;
            this.updateTimeStamp = updateTimeStamp;
            this.coef = coef;
            this.side = side;
            this.size = size;
            this.positionIdx = positionIdx;
        }

        /*
        *    Nom : Position
        *    Paramètre E : JSON d'une position récupérée sur Binance
        *    Role : Construteur qui sert à créer une position à partir du JSON récupérer sur Binance
        *    Fiabilité : sure
        */
        public Position(JToken listElement)
        {
            symbol = (string)listElement["symbol"]!;
            entryPrice = (double)listElement["entryPrice"]!;
            updateTimeStamp = (string)listElement["updateTimeStamp"]!;
            coef = 0;

            string anAmount = (string)listElement["amount"]!;

            //Console.WriteLine("anAmount : " + anAmount);

            if (anAmount.Contains("-"))
            {
                side = "Sell";
                positionIdx = 2;

                anAmount = anAmount.TrimStart('-');
            }
            else
            {
                side = "Buy";
                positionIdx = 1;
            }

            size = double.Parse(anAmount, CultureInfo.InvariantCulture);
        }

        /*
        *    Nom : ToString
        *    Retour : chaine de caractère représentant la position
        *    Role : Obtenir une représentation de la position en chaine de caractère
        *    Fiabilité : sure
        */
        public override string ToString()
        {
            string obj = string.Empty;
            obj += "symbol : " + symbol;
            obj += "    entryPrice : " + entryPrice;
            obj += "    updateTimeStamp : " + updateTimeStamp;
            obj += "    side : " + side;
            obj += "    size : " + size;

            return obj;
        }

        /*
        *    Nom : opérateur ==
        *    Retour : booléen indiquant si les positions sont égales, true : positions égales, false : positions différentes
        *    Paramètre E : 2 positions à comparer
        *    Role : Comparer 2 positions
        *    Fiabilité : sure
        */
        public static bool operator ==(Position pos1, Position pos2)
        {
            return pos1.symbol == pos2.symbol && pos1.side == pos2.side && pos1.size == pos2.size;
        }

        /*
        *    Nom : Equals
        *    Retour : retourne false
        *    Paramètre E : un objet 
        *    Role : aucun (pas utilisé mais il faut faire sauter l'avertissement)
        *    Fiabilité : à ne pas utiliser
        */
        public override bool Equals(object? o)
        {
            return false;
        }

        /*
        *    Nom : GetHashCode
        *    Retour : retourne 0
        *    Role : aucun (pas utilisé mais il faut faire sauter l'avertissement)
        *    Fiabilité : à ne pas utiliser
        */
        public override int GetHashCode()
        {
            return 0;
        }

        /*
        *    Nom : opérateur !=
        *    Retour : booléen indiquant si les positions sont différentes, true : positions différentes, false : positions égales
        *    Paramètre E : 2 positions à comparer
        *    Role : Comparer 2 positions
        *    Fiabilité : sure
        */
        public static bool operator !=(Position pos1, Position pos2)
        {
            return !(pos1 == pos2);
        }

        /*
        *    Nom : isSamePosition
        *    Retour : booléen indiquant si les positions correspondent (même symbole et même sens), true : correspondent , false : ne correspondent pas
        *    Paramètre E : 2 positions à comparer
        *    Role : Comparer 2 positions
        *    Fiabilité : sure
        */
        public bool isSamePosition(Position position)
        {
            return position.symbol == symbol && position.side == side;
        }

        /*
        *    Nom : hasSameSymbol
        *    Retour : booléen indiquant si les positions ont le même symbole, true : même symbole , false : symbole différent
        *    Paramètre E : 2 positions à comparer
        *    Role : Comparer 2 positions
        *    Fiabilité : sure
        */
        private bool hasSameSymbol(Position position)
        {
            return position.symbol == symbol;
        }

        /*
        *    Nom : display
        *    Role : afficher une représentation de la position au format textuel
        *    Fiabilité : sure
        */
        public void display()
        {
            Console.WriteLine("symbol: " + symbol + "  entryPrice: " + entryPrice.ToString(CultureInfo.InvariantCulture) + "    side: " + side + "  size: " + size.ToString(CultureInfo.InvariantCulture));
        }
    }
}
