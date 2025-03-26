using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace PKServ.Admin
{
    public static class SettingsHelper
    {
        /// <summary>
        /// Parcourt récursivement toutes les propriétés d'un objet et, pour chaque propriété qui vaut null
        /// ou qui est égale à sa valeur par défaut (pour les types valeur non-nullables),
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
                // Ignorer les propriétés indexées
                if (property.GetIndexParameters().Length > 0)
                    continue;

                // Considérer uniquement les propriétés lisibles et modifiables
                if (!property.CanRead || !property.CanWrite)
                    continue;

                object targetValue = property.GetValue(target);
                object defaultValue = property.GetValue(defaults);

                // Si la propriété est de type complexe (classe, à l'exception de string)
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    // Si targetValue est null alors essayer de créer une nouvelle instance
                    if (targetValue == null && defaultValue != null)
                    {
                        ConstructorInfo ctor = property.PropertyType.GetConstructor(Type.EmptyTypes);
                        if (ctor != null)
                        {
                            // Création d'une nouvelle instance pour target
                            targetValue = Activator.CreateInstance(property.PropertyType);
                            property.SetValue(target, targetValue);
                        }
                        else
                        {
                            // Sinon, on affecte directement l'instance default
                            property.SetValue(target, defaultValue);
                            continue;
                        }
                    }
                    // Fusionner récursivement les propriétés imbriquées
                    if (targetValue != null && defaultValue != null)
                    {
                        MergeWithDefaults(targetValue, defaultValue);
                    }
                }
                else
                {
                    // Traitement pour les types simples

                    // Cas classique pour les types référence (qui peuvent être null)
                    if (targetValue == null && defaultValue != null)
                    {
                        property.SetValue(target, defaultValue);
                    }
                    // Cas pour les types valeur non-nullables (int, bool, etc.)
                    else if (!property.PropertyType.IsClass &&
                        EqualityComparer<object>.Default.Equals(targetValue, Activator.CreateInstance(property.PropertyType)) &&
                        !EqualityComparer<object>.Default.Equals(defaultValue, Activator.CreateInstance(property.PropertyType)))
                    {
                        property.SetValue(target, defaultValue);
                    }
                }
            }
        }
    }
}