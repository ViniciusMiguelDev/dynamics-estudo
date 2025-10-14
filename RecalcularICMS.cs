using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

namespace DynamicsEstudo
{
    public class RecalcularICMS : PluginBase
    {
        public RecalcularICMS(string unsecure, string secure) : base(typeof(RecalcularICMS)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localContext)
        {
            var serviceProvider = localContext.ServiceProvider;
            // contexto para pegar parametros e tracing para a desgraça do log (catch e tlz)
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (!context.InputParameters.Contains("Target"))
                return;

            // Cria a porra dos services
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            // Guarda a tabela de mercadoria que gatilhou o plugin
            Entity targetEntity = context.InputParameters["Target"] as Entity;

            try
            {
                // Pega a nota fiscal vinculada a mercadoria (pai)
                EntityReference notaFiscalRef = null;

                if (context.MessageName.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                {
                    if (!context.PreEntityImages.Contains("PreImage"))
                        return;

                    // Pega a entidade antes da exclusão
                    targetEntity = context.PreEntityImages["PreImage"];
                    tracing.Trace($"Target {targetEntity}");

                    // Pega a nota fiscal vinculada
                    notaFiscalRef = targetEntity.GetAttributeValue<EntityReference>("vi_notafiscal");
                    tracing.Trace($"Nota {notaFiscalRef.Name}");
                }
                else
                {
                    // No create/update pega direto da entidade alvo
                    if (targetEntity.Attributes.Contains("vi_notafiscal"))
                        notaFiscalRef = targetEntity.GetAttributeValue<EntityReference>("vi_notafiscal");

                    // se não tiver na entidade alvo, tenta pegar na preimage
                    else if (context.PreEntityImages.Contains("PreImage"))
                        notaFiscalRef = context.PreEntityImages["PreImage"].GetAttributeValue<EntityReference>("vi_notafiscal");
                }

                // Se não tiver nota fiscal vinculada, sai fora
                if (notaFiscalRef == null)
                    return;
                tracing.Trace($"Target {targetEntity}");

                // Recupera a nota fiscal para obter o estado
                var notaFiscal = service.Retrieve("vi_notafiscal", notaFiscalRef.Id, new ColumnSet("vi_estado"));
                OptionSetValue estadoValor = notaFiscal.GetAttributeValue<OptionSetValue>("vi_estado");

                // Mapeamento dos estados e alíquotas
                var estadoMap = new Dictionary<int, string>
                {
                    { 1, "SP" }, { 2, "RJ" }, { 3, "MG" }, { 4, "ES" },
                    { 5, "PR" }, { 6, "SC" }, { 7, "RS" }
                };

                var aliquotas = new Dictionary<string, decimal>
                {
                    { "SP", 0.18m }, { "RJ", 0.20m }, { "MG", 0.17m },
                    { "ES", 0.17m }, { "PR", 0.18m }, { "SC", 0.17m }, { "RS", 0.18m }
                };

                // Determina a alíquota com base no estado 
                string estadoSigla = estadoMap.ContainsKey(estadoValor.Value) ? estadoMap[estadoValor.Value] : null;
                decimal aliquota = (estadoSigla != null && aliquotas.ContainsKey(estadoSigla)) ? aliquotas[estadoSigla] : 0m;

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
                tracing.Trace($"Numero de mercadorias {mercadorias.Count}");

                decimal totalICMS = 0m;

                // Calcula o ICMS somando o valor de cada mercadoria (Preço * Quantidade * Alíquota)
                foreach (var m in mercadorias)
                {
                    decimal preco = 0m;
                    int qtd = 0;

                    // Pega o preço e a quantidade
                    preco = m.GetAttributeValue<decimal>("vi_preco");
                    qtd = m.GetAttributeValue<int>("vi_quantidade");

                    // Acumula o ICMS
                    totalICMS += preco * qtd * aliquota;
                }

                // Atualiza nota fiscal com o total de ICMS calculado e pa
                var nota = new Entity("vi_notafiscal", notaFiscalRef.Id);

                // Arredonda para 2 casas decimais
                nota["ava_icmstotal"] = new Money(totalICMS);

                // Atualiza a nota fiscal no Dataverse
                service.Update(nota);
            }

            // Loga qualquer exceção que ocorrer
            catch (Exception ex)
            {
                tracing.Trace("RecalcularICMS Exception: " + ex.ToString());
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
