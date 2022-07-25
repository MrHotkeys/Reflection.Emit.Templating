using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public static class EmitTemplating
    {
        private static IServiceProvider? _serviceProvider;
        public static IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider is null)
                {
                    var serviceCollection = new ServiceCollection()
                        .AddLogging(c => c.AddProvider(NullLoggerProvider.Instance))
                        .AddSingleton<EmitTemplateProcessor>();

                    return serviceCollection.BuildServiceProvider();
                }

                return _serviceProvider;
            }
            set
            {
                _serviceProvider = value ?? throw new ArgumentNullException(nameof(ServiceProvider));
                _loggerFactory = null;
                _templateProcessor = null;
            }
        }

        private static ILoggerFactory? _loggerFactory;
        public static ILoggerFactory LoggerFactory
        {
            get
            {
                if (_loggerFactory is null)
                    _loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();

                return _loggerFactory;
            }
        }

        private static EmitTemplateProcessor? _templateProcessor;
        public static EmitTemplateProcessor TemplateProcessor
        {
            get
            {
                if (_templateProcessor is null)
                    _templateProcessor = ServiceProvider.GetRequiredService<EmitTemplateProcessor>();

                return _templateProcessor;
            }
        }
    }
}