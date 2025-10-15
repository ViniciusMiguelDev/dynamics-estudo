using Microsoft.Xrm.Sdk;
using System;

namespace DynamicsEstudo
{
    public class MercadoriaPlugin : PluginBase
    {
        private readonly MercadoriaService _mercadoriaService;

        public MercadoriaPlugin() : base(typeof(MercadoriaPlugin))
        {
            _mercadoriaService = new MercadoriaService(
                new Repository(),
                new EstadoAliquotaMap(),
                new CalcularICMS()
            );
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localContext)
        {
            var serviceProvider = localContext.ServiceProvider;

            // contexto para pegar parametros e tracing par alog
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Cria o service
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                _mercadoriaService.ProcessarICMS(context, service);
            }
            catch (Exception ex)
            {
                tracing.Trace($"[MercadoriaPlugin] Erro: {ex}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
