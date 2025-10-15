using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

public class NotaFiscalService : INotaFiscalService
{
    private readonly Repository _repository;
    private readonly EstadoAliquotaMap _aliquotaUtil;
    private readonly CalcularICMS _calcularICMS;
    private readonly CnpjValidator _cnpjValidator;

    public NotaFiscalService(Repository repository, EstadoAliquotaMap aliquotaUtil, CalcularICMS calcularICMS, CnpjValidator cnpjValidator)
    {
        _repository = repository;
        _aliquotaUtil = aliquotaUtil;
        _calcularICMS = calcularICMS;
        _cnpjValidator = cnpjValidator;
    }

    public void ProcessarNotaFiscal(IPluginExecutionContext context, IOrganizationService service)
    {
        if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity targetEntity))
            throw new InvalidPluginExecutionException("Target inválido no contexto.");

        EntityReference notaFiscalRef = new EntityReference(targetEntity.LogicalName, targetEntity.Id);

        _cnpjValidator.ValidarCnpj(targetEntity, context);

        var mercadorias = _repository.GetMercadorias(notaFiscalRef, service);

        decimal aliquota = _aliquotaUtil.GetAliquota(targetEntity);
        decimal totalICMS = _calcularICMS.CalcularICMSTotal(mercadorias, aliquota);

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
