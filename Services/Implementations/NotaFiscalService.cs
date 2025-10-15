using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

public class NotaFiscalService : INotaFiscalService
{
    private readonly Repository _repository;

    public NotaFiscalService(Repository repository)
    {
        _repository = repository;
    }

    public void ProcessarNotaFiscal(IPluginExecutionContext context, IOrganizationService service)
    {
        if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity targetEntity))
            throw new InvalidPluginExecutionException("Target inválido no contexto.");

        EntityReference notaFiscalRef = new EntityReference(targetEntity.LogicalName, targetEntity.Id);

        CnpjValidator.ValidarCnpj(targetEntity, context);

        var mercadorias = _repository.GetMercadorias(notaFiscalRef, service);

        decimal aliquota = EstadoAliquotaMap.GetAliquota(targetEntity);
        decimal totalICMS = CalcularICMS.CalcularICMSTotal(mercadorias, aliquota);

        AtualizarICMSSeNecessario(targetEntity, totalICMS, service);
    }

    public void AtualizarICMSSeNecessario(Entity targetEntity, decimal totalICMS, IOrganizationService service)
    {
        if (targetEntity == null || targetEntity.LogicalName != "vi_notafiscal")
            throw new InvalidPluginExecutionException("Entidade de nota fiscal inválida.");

        var currentICMS = targetEntity.GetAttributeValue<Money>("ava_icmstotal")?.Value ?? 0m;

        if (currentICMS != totalICMS)
        {
            var notaToUpdate = new Entity("vi_notafiscal", targetEntity.Id)
            {
                ["ava_icmstotal"] = new Money(Math.Round(totalICMS, 2))
            };

            service.Update(notaToUpdate);
        }
    }
}
