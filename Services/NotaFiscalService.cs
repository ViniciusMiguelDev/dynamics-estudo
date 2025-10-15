using Microsoft.Xrm.Sdk;
using System;
using System.Web.Services.Description;

public class NotaFiscalService
{
	public void validAndUpdate(Entity targetEntity, decimal totalICMS, IOrganizationService service)
	{
        if (targetEntity == null || targetEntity.LogicalName != "vi_notafiscal")
            throw new InvalidPluginExecutionException("Nota fiscal não encontrada");

        // Evita atualizar se o valor é o mesmo
        var currentICMS = targetEntity.GetAttributeValue<Money>("ava_icmstotal")?.Value ?? 0m;
        if (currentICMS != totalICMS)
        {
            if (targetEntity == null)
                throw new InvalidPluginExecutionException("Entidade está nula");

            targetEntity["ava_icmstotal"] = new Money(totalICMS);
            service.Update(targetEntity);
        }
    }
}
