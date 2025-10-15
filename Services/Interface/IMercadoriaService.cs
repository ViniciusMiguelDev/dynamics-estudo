using Microsoft.Xrm.Sdk;
    public interface IMercadoriaService
{
     void ProcessarICMS(IPluginExecutionContext context, IOrganizationService service);
     (Entity, EntityReference) ValidarContextoTrigger(IPluginExecutionContext context);
     void AtualizarICMS(EntityReference notaFiscalRef, decimal totalICMS, IOrganizationService service);
}

