using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

  namespace DynamicsEstudo
{
    public class NotaFiscalPlugin : PluginBase
    {
        public NotaFiscalPlugin(string unsecure, string secure) : base(typeof(NotaFiscalPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localContext)
        {
            var serviceProvider = localContext.ServiceProvider;

            // contexto para pegar parametros e tracing par alog
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Cria o service
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            //Instanciar os repositórios
            var Rep = new Repository();

            //Instanciar o servico
            var NotaFiscalServ = new NotaFiscalService();

            // Util
            var aliquotaUtil = new EstadoAliquotaMap();
            var calcularICMS = new CalcularICMS();

            // Instanciar o validador de CNPJ
            var cnpjValidator = new CnpjValidator();

            // Evitar o puto do loop
            if (context.Depth > 1)
                return;

            if (!context.InputParameters.Contains("Target"))
                return;

            // Guarda a tabela de notaFiscal que gatilhou o plugin
            Entity targetEntity = context.InputParameters["Target"] as Entity;
            EntityReference notaFiscalRef = new EntityReference(targetEntity.LogicalName, targetEntity.Id);

            try
            {
                // Valida o CNPJ
                cnpjValidator.ValidarCnpj(targetEntity, context);

                var mercadorias = Rep.getMercadorias(notaFiscalRef, service);

                decimal aliquota = aliquotaUtil.GetAliquota(targetEntity);

                decimal totalICMS = calcularICMS.CalcularICMSTotal(mercadorias, aliquota);

                NotaFiscalServ.validAndUpdate(targetEntity, totalICMS, service);
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
