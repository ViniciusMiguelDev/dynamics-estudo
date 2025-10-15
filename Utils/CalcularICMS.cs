using Microsoft.Xrm.Sdk;
using System;

public class CalcularICMS
{
	public decimal CalcularICMSTotal(DataCollection<Entity> mercadorias, decimal aliquota)
	{
        decimal totalICMS = 0m;

        // Calcula o ICMS somando o valor de cada mercadoria (Preço * Quantidade * Alíquota)
        foreach (var m in mercadorias)
        {
            // Pega o preço e a quantidade
            decimal preco = m.GetAttributeValue<decimal?>("vi_preco") ?? 0m;
            int qtd = m.GetAttributeValue<int?>("vi_quantidade") ?? 0;

            // Acumula o ICMS
            totalICMS += preco * qtd * aliquota;
            
        }
        return totalICMS;
    }
}
