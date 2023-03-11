/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      28/02/2023 -> codage des fonctions et commentaires en intégralité
 *  Role : Définition des classes TradingLog et ProxyLog qui permet de faciliter les logs
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyEnhancedServerv2.Utils
{
    /*
        *    Nom : TradingLog
        *    Role : Classe qui sert à faciliter les logs du logiciel
    */
    internal class Log
    {
        //indique si le listener a été configuré
        private static bool isThereListener = false;

        /*
        *    Nom : TraceInformation
        *    Paramètre E : fonction qui est le nom de la fonction d'origine et message qui est le message à enregistrer dans les logs
        *    Role : Enregistrer une information dans un fichier log
        *    Fiabilite : sure
        */
        public static void TraceInformation(string fonction, string message)
        {
            if (!isThereListener)
            {
                Trace.Listeners.Add(new TextWriterTraceListener("BuyEnhancedServer.log", "BuyEnhancedServerListener"));
                isThereListener = true;
            }
            string dateTime = DateTime.Now.ToString();
            Trace.TraceInformation(dateTime + " : " + fonction + " : " + message);
            Trace.Flush();
        }

        /*
        *    Nom : TraceError
        *    Paramètre E : fonction qui est le nom de la fonction d'origine et message qui est le message à enregistrer dans les logs
        *    Role : Enregistrer une erreur dans un fichier log
        *    Fiabilite : sure
        */
        public static void TraceError(string fonction, string message)
        {
            if (!isThereListener)
            {
                Trace.Listeners.Add(new TextWriterTraceListener("BuyEnhancedServer.log", "BuyEnhancedServerListener"));
                isThereListener = true;
            }
            string dateTime = DateTime.Now.ToString();
            Trace.TraceError(dateTime + " : " + fonction + " : " + message);
            Trace.Flush();
        }

        /*
        *    Nom : TraceWarning
        *    Paramètre E : fonction qui est le nom de la fonction d'origine et message qui est le message à enregistrer dans les logs
        *    Role : Enregistrer un avertissement dans un fichier log
        *    Fiabilite : sure
        */
        public static void TraceWarning(string fonction, string message)
        {
            if (!isThereListener)
            {
                Trace.Listeners.Add(new TextWriterTraceListener("BuyEnhancedServer.log", "BuyEnhancedServerListener"));
                isThereListener = true;
            }
            string dateTime = DateTime.Now.ToString();
            Trace.TraceWarning(dateTime + " : " + fonction + " : " + message);
            Trace.Flush();
        }
    }
}
