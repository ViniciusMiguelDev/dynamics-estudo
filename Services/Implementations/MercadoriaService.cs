using Microsoft.Xrm.Sdk;
using System;

public class MercadoriaService : IMercadoriaService
{
    private readonly Repository _repository;
    private readonly EstadoAliquotaMap _aliquotaUtil;
    private readonly CalcularICMS _calcularICMS;

    public MercadoriaService(Repository repository, EstadoAliquotaMap aliquotaUtil, CalcularICMS calcularICMS)
    {
        _repository = repository;
        _aliquotaUtil = aliquotaUtil;
        _calcularICMS = calcularICMS;
    }

    public void ProcessarICMS(IPluginExecutionContext context, IOrganizationService service)
    {
        // Valida e obtém as entidades do contexto
        var (mercadoria, notaFiscalRef) = ValidarContextoTrigger(context);

        if (notaFiscalRef == null || mercadoria == null)
            throw new InvalidPluginExecutionException("Entidades nulas no contexto.");

        // Busca a nota fiscal
        var notaFiscal = _repository.GetNotaFiscal(notaFiscalRef, service)
            ?? throw new InvalidPluginExecutionException("Nota fiscal não encontrada.");

        // Busca as mercadorias vinculadas
        var mercadorias = _repository.GetMercadorias(notaFiscalRef, service);

        // Calcula a alíquota e o ICMS total
        decimal aliquota = _aliquotaUtil.GetAliquota(notaFiscal);
        decimal totalICMS = _calcularICMS.CalcularICMSTotal(mercadorias, aliquota);

        // Atualiza a nota fiscal com o valor calculado
        AtualizarICMS(notaFiscalRef, totalICMS, service);
    }

    public (Entity, EntityReference) ValidarContextoTrigger(IPluginExecutionContext context)
    {
        if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity targetEntity))
            throw new InvalidPluginExecutionException("Target inválido no contexto.");

        EntityReference notaFiscalRef = null;

        // Caso de DELETE
        if (context.MessageName.Equals("Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.PreEntityImages.Contains("PreImage"))
                throw new InvalidPluginExecutionException("PreImage ausente no contexto de Delete.");

            targetEntity = context.PreEntityImages["PreImage"];
            notaFiscalRef = targetEntity.GetAttributeValue<EntityReference>("vi_notafiscal");
        }
        else
        {
            // CREATE / UPDATE
            if (targetEntity.Contains("vi_notafiscal"))
                notaFiscalRef = targetEntity.GetAttributeValue<EntityReference>("vi_notafiscal");
            else if (context.PreEntityImages.Contains("PreImage"))
                notaFiscalRef = context.PreEntityImages["PreImage"].GetAttributeValue<EntityReference>("vi_notafiscal");
        }

        if (notaFiscalRef == null)
            throw new InvalidPluginExecutionException("Nota fiscal vinculada não encontrada.");

        return (targetEntity, notaFiscalRef);
    }

    private void AtualizarICMS(EntityReference notaFiscalRef, decimal totalICMS, IOrganizationService service)
    {
        var nota = new Entity("vi_notafiscal", notaFiscalRef.Id)
        {
            ["ava_icmstotal"] = new Money(Math.Round(totalICMS, 2))
        };

        service.Update(nota);
    }

    void IMercadoriaService.AtualizarICMS(EntityReference notaFiscalRef, decimal totalICMS, IOrganizationService service)
    {
        AtualizarICMS(notaFiscalRef, totalICMS, service);
    }
}
