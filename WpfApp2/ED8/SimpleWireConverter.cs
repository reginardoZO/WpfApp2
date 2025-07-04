using System;
using System.Collections.Generic;

namespace WireCodeConverter
{
    /// <summary>
    /// Função simples para conversão de códigos de fios
    /// </summary>
    public static class SimpleWireConverter
    {
        /// <summary>
        /// Função principal que converte string da esquerda para string da direita
        /// conforme a tabela fornecida
        /// </summary>
        /// <param name="inputCode">Código de entrada (ex: "1 #12")</param>
        /// <returns>Descrição correspondente ou null se não encontrado</returns>
        public static string ConvertWireCode(string inputCode)
        {
            // Dicionário com os mapeamentos exatos da tabela
            var mappings = new Dictionary<string, string>
            {
                { "1 #12", "1 x (3 x 12 AWG + 1 x 12 AWG)" },
                { "1 #10", "1 x (3 x 12 AWG + 1 x 12 AWG)" },
                { "1 #8", "1 x (3 x 8 AWG + 1 x 10 AWG)" },
                { "1 #6", "1 x (3 x 6 AWG + 1 x 8 AWG)" },
                { "1 #4", "1 x (3 x 4 AWG + 1 x 8 AWG)" },
                { "1 #2", "1 x (3 x 2 AWG + 1 x 6 AWG)" },
                { "1 #1", "1 x (3 x 1 AWG + 1 x 6 AWG)" },
                { "1 #2/0", "1 x (3 x 2/0 AWG + 1 x 6 AWG)" },
                { "1 #4/0", "3 x 4/0 AWG + 1 x 4 AWG" },
                { "1 #350Kcmil", "3 x 350 Kcmil + 1 x 4 AWG" },
                { "1 #500Kcmil", "3 x 500 Kcmil + 1 x 3 AWG" },
                { "2 #4", "2 x (3 x 4 AWG + 1 x 8 AWG)" },
                { "2 #350Kcmil", "6 x 350 Kcmil + 2 x 4 AWG" },
                { "2 #500Kcmil", "6 x 500 Kcmil + 2 x 3 AWG" },
                { "3 #350Kcmil", "9 x 350 Kcmil + 3 x 4 AWG" }
            };

            // Retorna o valor correspondente ou null se não encontrado
            return mappings.TryGetValue(inputCode?.Trim(), out string result) ? result : null;
        }
    }

    /// <summary>
    /// Exemplo de uso da função
    /// </summary>
  
}

