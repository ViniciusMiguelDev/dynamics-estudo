using Microsoft.Xrm.Sdk;

public interface INotaFiscalService
{
    void ProcessarNotaFiscal(IPluginExecutionContext context, IOrganizationService service);
    void AtualizarICMSSeNecessario(Entity targetEntity, decimal totalICMS, IOrganizationService service);
}
