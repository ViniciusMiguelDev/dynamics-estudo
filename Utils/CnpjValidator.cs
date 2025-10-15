using Microsoft.Xrm.Sdk;
using System;
using System.Runtime.Remoting.Contexts;

public class CnpjValidator
{
	public void ValidarCnpj(Entity targetEntity, IPluginExecutionContext context)
	{
        // valide se é notaFisal e se é nulo 
        if (targetEntity == null || targetEntity.LogicalName != "vi_notafiscal")
            throw new InvalidPluginExecutionException("Nota fiscal não encontrada");

        // Valida o CNPJ
        string cnpj = null;
        if (targetEntity.Attributes.Contains("vi_cnpj"))
            cnpj = targetEntity.GetAttributeValue<string>("vi_cnpj");
        else if (context.PreEntityImages.Contains("PreImage"))
            cnpj = context.PreEntityImages["PreImage"].GetAttributeValue<string>("vi_cnpj");

        if (string.IsNullOrWhiteSpace(cnpj) || !IsCnpjValid(cnpj))
            throw new InvalidPluginExecutionException("CNPJ inválido na nota fiscal.");
    }

    private bool IsCnpjValid(string cnpj)
    {
        if (new string(cnpj[0], 14) == cnpj)
            return false;

        int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        string tempCnpj = cnpj.Substring(0, 12);
        int soma = 0;

        for (int i = 0; i < 12; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

        int resto = (soma % 11);
        if (resto < 2) resto = 0;
        else resto = 11 - resto;

        string digito = resto.ToString();
        tempCnpj += digito;
        soma = 0;

        for (int i = 0; i < 13; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

        resto = (soma % 11);
        if (resto < 2) resto = 0;
        else resto = 11 - resto;

        digito += resto.ToString();

        return cnpj.EndsWith(digito);
    }
}
