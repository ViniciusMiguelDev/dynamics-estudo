using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

public class Repository
{
	public DataCollection<Entity> getMercadorias(EntityReference notaFiscalRef, IOrganizationService service)
	{
        // Consulta mercadorias vinculadas
        var query = new QueryExpression("vi_mercadoria")
        {
            // Consulta apenas preço e quantidade 
            ColumnSet = new ColumnSet("vi_preco", "vi_quantidade"),
            // Filtro para pegar apenas as mercadorias da nota fiscal atual
            Criteria = { Conditions = { new ConditionExpression("vi_notafiscal", ConditionOperator.Equal, notaFiscalRef.Id) } }
        };

        // Executa a consulta e pega as entidades
        var mercadorias = service.RetrieveMultiple(query).Entities;

        return mercadorias;
    }

    public Entity getNotaFiscal (EntityReference notaFiscalRef, IOrganizationService service)
    {
        var nota = service.Retrieve("vi_notafiscal", notaFiscalRef.Id, new ColumnSet("vi_estado"));
        if (nota == null) throw new InvalidPluginExecutionException("Nota Fiscal não encontrada.");
        return nota;
    }
}
