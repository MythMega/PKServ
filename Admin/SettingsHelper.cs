using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace PKServ.Admin
{
    public static class SettingsHelper
    {
        /// <summary>
        /// Parcourt récursivement toutes les propriétés d'un objet et, pour chaque propriété qui vaut null,
        /// lui affecte la valeur correspondante issue de l'objet defaults.
        /// </summary>
        /// <param name="target">L'objet à compléter.</param>
        /// <param name="defaults">L'objet contenant les valeurs par défaut.</param>
        public static void MergeWithDefaults(object target, object defaults)
        {
            if (target == null || defaults == null)
                return;

            Type type = target.GetType();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Ignorer les propriétés qui nécessitent des paramètres (ex : indexées)
                if (property.GetIndexParameters().Length > 0)
                    continue;

                // Nous ne considérons que les propriétés qui sont à la fois lisibles et modifiables
                if (!property.CanRead || !property.CanWrite)
                    continue;

                // Récupère la valeur actuelle dans l'objet target et la valeur par défaut
                object targetValue = property.GetValue(target);
                object defaultValue = property.GetValue(defaults);

                // Si la propriété target est null et que defaultValue ne l'est pas, on affecte la valeur par défaut
                if (targetValue == null && defaultValue != null)
                {
                    property.SetValue(target, defaultValue);
                }
                // Si la propriété est un objet complexe (classe) à l'exclusion des types simples, on parcourt récursivement
                else if (targetValue != null && defaultValue != null &&
                         property.PropertyType.IsClass &&
                         property.PropertyType != typeof(string))
                {
                    MergeWithDefaults(targetValue, defaultValue);
                }
            }
        }
    }
}