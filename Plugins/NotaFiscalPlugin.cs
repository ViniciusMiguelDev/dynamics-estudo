using Microsoft.Xrm.Sdk;
using System;

namespace DynamicsEstudo
{
    public class NotaFiscalPlugin : PluginBase
    {
        private readonly NotaFiscalService _notaFiscalService;

        public NotaFiscalPlugin() : base(typeof(NotaFiscalPlugin))
        {
            _notaFiscalService = new NotaFiscalService(
                new Repository()
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
                // Evita loop infinito de atualização
                if (context.Depth > 1)
                    return;

                // Chama o service que orquestra tudo
                _notaFiscalService.ProcessarNotaFiscal(context, service);
            }
            catch (Exception ex)
            {
                tracing.Trace($"[NotaFiscalPlugin] Erro: {ex}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
