/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      26/02/2023 -> codage de l'intégralité + commentaire
*  Role : Classe hérité d'exception qui permet d'avoir un type d'exception Bybit et qui stocke notamment le code de retour de l'API en cas d'erreur
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BuyEnhancedServer.Bybit
{
    /*
        *    Nom : BybitException
        *    Role : Classe qui sert à gérer les erreurs renvoyer par l'API Bybit
    */
    internal class BybitException : Exception
    {
        //code d'erreur renvoyé par l'API ou 1 pour les erreurs personnalisé
        public int code { get; }

        /*
        *    Nom : BybitException
        *    Paramètre E : code de retour de l'API (ou 1 personnalisé) et message d'erreur de l'API (ou personnalisé)
        *    Role : Constructeur de BybitManager, instance qui sert d'interface pour la communication avec l'API Bybit
        */
        public BybitException(int aCode, string message) : base(message) 
        {
            this.code = aCode;
        }
    }
}
