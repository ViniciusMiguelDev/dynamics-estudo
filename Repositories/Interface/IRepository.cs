using Microsoft.Xrm.Sdk;

public interface IRepository
{
    DataCollection<Entity> GetMercadorias(EntityReference notaFiscalRef, IOrganizationService service);
    Entity GetNotaFiscal(EntityReference notaFiscalRef, IOrganizationService service);
}
