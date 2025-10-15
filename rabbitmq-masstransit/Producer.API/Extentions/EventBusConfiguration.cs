using MassTransit;
using Model.Share;
using RabbitMQ.Client;

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


                    // Đặt tên exchange chung
                    cfg.Message<NotificationEvent>(x =>
                    {
                        x.SetEntityName("notification-exchange");
                    });

                    cfg.Publish<NotificationEvent>(x =>
                    {
                        x.ExchangeType = ExchangeType.Direct; // sử dụng direct exchange
                    });
                });

            });
            return services;
        }


    }
}
