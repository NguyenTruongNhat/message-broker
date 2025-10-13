using MassTransit;

namespace Producer.API.Extentions
{
    public static class EventBusConfiguration
    {
        public static IServiceCollection BusConfiguration(this IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("localhost", "nhatnguyen", h =>
                    {
                        h.Username("sa");
                        h.Password("pass");
                    });
                });

            });
            return services;
        }


    }
}
