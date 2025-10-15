using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

public static class EstadoAliquotaMap
{
	public static decimal GetAliquota(Entity notaFiscal)
	{
        // valide se é notaFisal e se é nulo 
        if (notaFiscal == null || notaFiscal.LogicalName != "vi_notafiscal")
            throw new InvalidPluginExecutionException("Nota fiscal não encontrada");

        OptionSetValue estadoValor = notaFiscal.GetAttributeValue<OptionSetValue>("vi_estado");
        if (estadoValor == null) return 0m;

        var estadoMap = new Dictionary<int, string>
                {
                    { 1, "SP" }, { 2, "RJ" }, { 3, "MG" }, { 4, "ES" },
                    { 5, "PR" }, { 6, "SC" }, { 7, "RS" }
                };

        var aliquotas = new Dictionary<string, decimal>
                {
                    { "SP", 0.18m }, { "RJ", 0.20m }, { "MG", 0.17m },
                    { "ES", 0.17m }, { "PR", 0.18m }, { "SC", 0.17m }, { "RS", 0.18m }
                };

        // Determina a alíquota com base no estado 
        string estadoSigla = estadoMap.ContainsKey(estadoValor.Value) ? estadoMap[estadoValor.Value] : null;
        decimal aliquota = (estadoSigla != null && aliquotas.ContainsKey(estadoSigla)) ? aliquotas[estadoSigla] : 0m;

        return aliquota;
    }
}
