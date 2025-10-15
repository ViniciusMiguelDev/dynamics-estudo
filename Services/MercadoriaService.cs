using Microsoft.Xrm.Sdk;
using System;

public class MercadoriaService
{
    public (Entity, EntityReference) triggerValidate(IPluginExecutionContext context)
    {
        Entity targetEntity = context.InputParameters["Target"] as Entity;
        EntityReference targetRef = null;

        if (context.MessageName.Equals("Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.PreEntityImages.Contains("PreImage"))
                throw new InvalidPluginExecutionException("Contexto não tem PreImage - Delete (triggerValidate)");

            // Pega a entidade antes da exclusão
            targetEntity = context.PreEntityImages["PreImage"];

            // Pega a nota fiscal vinculada
            targetRef = targetEntity.GetAttributeValue<EntityReference>("vi_notafiscal");

        }
        else
        {
            // No create/update pega direto da entidade alvo
            if (targetEntity.Attributes.Contains("vi_notafiscal"))
                targetRef = targetEntity.GetAttributeValue<EntityReference>("vi_notafiscal");

            // se não tiver na entidade alvo, tenta pegar na preimage
            else if (context.PreEntityImages.Contains("PreImage"))
                targetRef = context.PreEntityImages["PreImage"].GetAttributeValue<EntityReference>("vi_notafiscal");
        }

        // Se não tiver nota fiscal vinculada, sai fora
        if (targetRef == null)
            throw new InvalidPluginExecutionException("targetRef Nula");
        else
            return (targetEntity, targetRef);
    }

    public void atualizarICMS(EntityReference notaFiscalRef, decimal totalICMS, IOrganizationService service)
    {
        // Atualiza nota fiscal com o total de ICMS calculado e pa
        var nota = new Entity("vi_notafiscal", notaFiscalRef.Id);

        // Arredonda para 2 casas decimais
        nota["ava_icmstotal"] = new Money(totalICMS);

        // Atualiza a nota fiscal no Dataverse
        service.Update(nota);
    }
}
