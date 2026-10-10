using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRSNC.Anatomia
{
    /// <summary>
    /// Lê contratos JSON legados com nomes de campos próprios em português.
    /// JsonUtility não aplica FormerlySerializedAs no JSON; a migração altera
    /// somente as chaves conhecidas, nunca descrições, identificadores ou fontes.
    /// </summary>
    public static class LeitorJsonCompativel
    {
        public static T Ler<T>(string texto)
        {
            var nomesHistoricos = new Dictionary<string, string>(StringComparer.Ordinal);
            ColetarNomesHistoricos(typeof(T), nomesHistoricos, new HashSet<Type>());
            var resultado = new StringBuilder(texto.Length);
            var inicioCopia = 0;
            for (var indice = 0; indice < texto.Length; indice++)
            {
                if (texto[indice] != '"') continue;
                var inicio = indice++;
                while (indice < texto.Length && texto[indice] != '"')
                {
                    if (texto[indice] == '\\') indice++;
                    indice++;
                }
                if (indice >= texto.Length) throw new FormatException("String JSON não terminada.");
                var fim = indice;
                var proximo = fim + 1;
                while (proximo < texto.Length && char.IsWhiteSpace(texto[proximo])) proximo++;
                // Apenas tokens de chave: strings de conteúdo são copiadas integralmente.
                if (proximo >= texto.Length || texto[proximo] != ':') continue;
                var chave = texto.Substring(inicio + 1, fim - inicio - 1);
                if (!nomesHistoricos.TryGetValue(chave, out var nomeAtual)) continue;
                resultado.Append(texto, inicioCopia, inicio - inicioCopia);
                resultado.Append('"').Append(nomeAtual).Append('"');
                inicioCopia = fim + 1;
            }
            resultado.Append(texto, inicioCopia, texto.Length - inicioCopia);
            return JsonUtility.FromJson<T>(resultado.ToString());
        }

        private static void ColetarNomesHistoricos(Type tipo, Dictionary<string, string> nomesHistoricos, HashSet<Type> visitados)
        {
            if (tipo.IsArray) tipo = tipo.GetElementType();
            if (tipo.IsPrimitive || tipo.IsEnum || tipo == typeof(string) || !visitados.Add(tipo)) return;
            foreach (var campo in tipo.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (FormerlySerializedAsAttribute nomeHistorico in campo.GetCustomAttributes(typeof(FormerlySerializedAsAttribute), false))
                {
                    if (nomesHistoricos.TryGetValue(nomeHistorico.oldName, out var existente) && existente != campo.Name)
                        throw new FormatException("Nome JSON histórico ambíguo: " + nomeHistorico.oldName);
                    nomesHistoricos[nomeHistorico.oldName] = campo.Name;
                }
                ColetarNomesHistoricos(campo.FieldType, nomesHistoricos, visitados);
            }
        }
    }
}
