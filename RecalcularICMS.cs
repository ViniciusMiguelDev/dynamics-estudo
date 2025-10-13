using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

namespace DynamicsEstudo
{
    public class RecalcularICMS : PluginBase
    {
        public RecalcularICMS(string unsecure, string secure) : base(typeof(RecalcularICMS)) { }

        protected override void ExecuteDataversePlugin(IServiceProvider serviceProvider)
        {
            // contexto para pegar parametros e tracing para log pourra (catch e tlz)
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (!context.InputParameters.Contains("Target"))
                return;

            // Cria a porra do service
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            // Guarda a tabela de mercadoria que gatilhou o plugin
            Entity targetEntity = context.InputParameters["Target"] as Entity;

            // valide se é mercadoria e se é nulo 
            if (targetEntity == null || targetEntity.LogicalName != "vi_mercadoria")
                return;

            try
            {
                // Pega a nota fiscal vinculada a mercadoria (pai)
                EntityReference notaFiscalRef = null;

                // Diferente tratamento para delete
                if (context.MessageName == "Delete")
                {
                    // No delete, a entidade alvo não tem atributos, pega na preimage (estado anterior)
                    if (context.PreEntityImages.Contains("PreImage"))
                        notaFiscalRef = context.PreEntityImages["PreImage"].GetAttributeValue<EntityReference>("vi_notafiscalid");
                }
                else
                {
                    // No create/update pega direto da entidade alvo
                    if (targetEntity.Attributes.Contains("vi_notafiscalid"))
                        notaFiscalRef = targetEntity.GetAttributeValue<EntityReference>("vi_notafiscalid");

                    // No update, se não tiver na entidade alvo, tenta pegar na preimage
                    else if (context.PreEntityImages.Contains("PreImage"))
                        notaFiscalRef = context.PreEntityImages["PreImage"].GetAttributeValue<EntityReference>("vi_notafiscalid");
                }

                // Se não tiver nota fiscal vinculada, sai fora
                if (notaFiscalRef == null)
                    return;

                // Recupera a nota fiscal para obter o estado
                var notaFiscal = service.Retrieve("vi_notafiscal", notaFiscalRef.Id, new ColumnSet("vi_estado"));
                int estadoValor = notaFiscal.Contains("vi_estado") ? notaFiscal.GetAttributeValue<int>("vi_estado") : 0;

                // Mapeamento dos estados e alíquotas (copiado do JS)
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
                string estadoSigla = estadoMap.ContainsKey(estadoValor) ? estadoMap[estadoValor] : null;
                decimal aliquota = (estadoSigla != null && aliquotas.ContainsKey(estadoSigla)) ? aliquotas[estadoSigla] : 0m;

                // Consulta mercadorias vinculadas
                var query = new QueryExpression("vi_mercadoria")
                {
                    // Consulta apenas preço e quantidade 
                    ColumnSet = new ColumnSet("vi_preco", "vi_quantidade"),
                    // Filtro para pegar apenas as mercadorias da nota fiscal atual
                    Criteria = { Conditions = { new ConditionExpression("vi_notafiscalid", ConditionOperator.Equal, notaFiscalRef.Id) } }
                };

                // Executa a consulta e pega as entidades
                var mercadorias = service.RetrieveMultiple(query).Entities;

                decimal totalICMS = 0m;

                // Calcula o ICMS somando o valor de cada mercadoria (Preço * Quantidade * Alíquota)
                foreach (var m in mercadorias)
                {
                    decimal preco = 0m;
                    decimal qtd = 0m;

                    // Pega o preço (Money) e converte para decimal
                    var precoMoney = m.GetAttributeValue<Money>("vi_preco");
                    if (precoMoney != null) preco = (decimal)precoMoney.Value;

                    if (m.Contains("vi_quantidade"))
                    {
                        var val = m["vi_quantidade"];
                        qtd = (int)val;
                    }

                    // Acumula o ICMS
                    totalICMS += preco * qtd * aliquota;
                }

                // Atualiza nota fiscal com o total de ICMS calculado e pa
                var nota = new Entity("vi_notafiscal", notaFiscalRef.Id);

                // Arredonda para 2 casas decimais
                nota["vi_icms_total"] = new Money(totalICMS);

                // Atualiza a nota fiscal no Dataverse
                service.Update(nota);
            }

            // Loga qualquer exceção que ocorrer
            catch (Exception ex)
            {
                tracing.Trace("RecalcularICMS Exception: " + ex.ToString());
                throw;
            }
        }
    }
}
