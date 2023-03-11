/*  Créer par : FEDEVIEILLE Gildas
 *  Edition : 
 *      25/02/2023 -> codage et commentaires de l'intégalité
 *  Role : Définition de la classe Extension qui ajout une la possibilité de joindre 2 listes
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyEnhancedServerv2.Utils
{
    /*
       *    Nom : Extension
       *    Role : Définition de la classe Extension qui facilite permet de joindre deux liste qui contiennent le même type de données
   */
    public static class Extension
    {
        /*
        *    Nom : Join
        *    Retour : une liste de type T obtenu par concatenation des deux listes passé en paramètre
        *    Paramètre E : deux listes à concatener
        *    Role : concatener deux liste de type T en prenant en compte le cas où une est nulle (echec des fonctions qui obtiennent les proxies)
        *    Fiabilité : sure
        */
        public static List<T> Join<T>(this List<T> first, List<T> second)
        {
            if (first == null)
            {
                return second;
            }
            if (second == null)
            {
                return first;
            }

            return first.Concat(second).ToList();
        }
    }
}
