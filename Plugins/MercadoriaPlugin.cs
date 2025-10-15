using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

namespace DynamicsEstudo
{
    public class MercadoriaPlugin : PluginBase
    {
        public MercadoriaPlugin() : base(typeof(MercadoriaPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localContext)
        {
            var serviceProvider = localContext.ServiceProvider;

            // contexto para pegar parametros e tracing par alog
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Cria o service
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            //Instanciar o repositório
            var Rep = new Repository();

            //Instanciar o servico
            var mercadoriaServ = new MercadoriaService();

            // Util
            var aliquotaUtil = new EstadoAliquotaMap();
            var calcularICMS = new CalcularICMS();

            try
            {
                // Guarda a tabela de mercadoria que gatilhou o plugin
                var entities = mercadoriaServ.triggerValidate(context);

                Entity targetEntity = entities.Item1;
                EntityReference notaFiscalRef = entities.Item2;

                if (notaFiscalRef == null || targetEntity == null)
                    throw new InvalidPluginExecutionException("Entidades estão nulas");

                var notaFiscal = Rep.getNotaFiscal(notaFiscalRef, service);
                
                var mercadorias = Rep.getMercadorias(notaFiscalRef, service);
                
                decimal aliquota = aliquotaUtil.GetAliquota(notaFiscal);

                decimal totalICMS = calcularICMS.CalcularICMSTotal(mercadorias, aliquota);

                mercadoriaServ.atualizarICMS(notaFiscalRef, totalICMS, service);  
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
