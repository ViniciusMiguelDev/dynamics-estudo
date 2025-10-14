using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

  namespace DynamicsEstudo
{
    public class UpdateEstado : PluginBase
    {
        public UpdateEstado (string unsecure, string secure) : base(typeof(UpdateEstado)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localContext)
        {
            var serviceProvider = localContext.ServiceProvider;
            // contexto para pegar parametros e tracing para a desgraça do log (catch e tlz)

            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Evitar o puto do loop
            if (context.Depth > 1)
                return;

            if (!context.InputParameters.Contains("Target"))
                return;

            // Cria a porra dos services
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            // Guarda a tabela de notaFiscal que gatilhou o plugin
            Entity targetEntity = context.InputParameters["Target"] as Entity;

            // valide se é notaFisal e se é nulo 
            if (targetEntity == null || targetEntity.LogicalName != "vi_notafiscal")
                return;

            try
            {

                // Valida o CNPJ
                string cnpj = null;
                if (targetEntity.Attributes.Contains("vi_cnpj"))
                    cnpj = targetEntity.GetAttributeValue<string>("vi_cnpj");
                else if (context.PreEntityImages.Contains("PreImage"))
                    cnpj = context.PreEntityImages["PreImage"].GetAttributeValue<string>("vi_cnpj");

                if (string.IsNullOrWhiteSpace(cnpj) || !IsCnpjValid(cnpj))
                    throw new InvalidPluginExecutionException("CNPJ inválido na nota fiscal.");


                // Pega o estado da nota fiscal
                OptionSetValue estadoValor = null;

                if (targetEntity.Attributes.Contains("vi_estado"))
                    estadoValor = targetEntity.GetAttributeValue<OptionSetValue>("vi_estado");
                else if (context.PreEntityImages.Contains("PreImage"))
                    estadoValor = context.PreEntityImages["PreImage"].GetAttributeValue<OptionSetValue>("vi_estado");

                if (estadoValor == null)
                    return;

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
                    ColumnSet = new ColumnSet("vi_preco", "vi_quantidade"),
                    Criteria = { Conditions = { new ConditionExpression("vi_notafiscal", ConditionOperator.Equal, targetEntity.Id) } }
                };


                // Executa a consulta e pega as entidades
                var mercadorias = service.RetrieveMultiple(query).Entities;

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

                // Evita atualizar se o valor é o mesmo
                var currentICMS = targetEntity.GetAttributeValue<Money>("ava_icmstotal")?.Value ?? 0m;
                if (currentICMS != totalICMS)
                {
                    targetEntity["ava_icmstotal"] = new Money(totalICMS);
                    service.Update(targetEntity);
                }

            }

            // Loga qualquer exceção que ocorrer
            catch (Exception ex)
            {
                tracing.Trace("RecalcularICMS Exception: " + ex.ToString());
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
        private bool IsCnpjValid(string cnpj)
        {
            if (new string(cnpj[0], 14) == cnpj)
                return false;

            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = (soma % 11);
            if (resto < 2) resto = 0;
            else resto = 11 - resto;

            string digito = resto.ToString();
            tempCnpj += digito;
            soma = 0;

            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = (soma % 11);
            if (resto < 2) resto = 0;
            else resto = 11 - resto;

            digito += resto.ToString();

            return cnpj.EndsWith(digito);
        }
    }
    
}
