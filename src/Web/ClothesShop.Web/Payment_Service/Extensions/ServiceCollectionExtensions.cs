using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment_Service.Configuration;
using Payment_Service.Models;
using Payment_Service.Services;

namespace Payment_Service.Extensions
{
    /// <summary>
    /// Extension methods để đăng ký Payment Service vào DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Đăng ký Payment Service với cấu hình từ appsettings.json
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddPaymentService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Đăng ký settings
            services.Configure<PaymentSettings>(configuration.GetSection("Payment"));
            services.Configure<MoMoSettings>(configuration.GetSection("Payment:MoMo"));
            services.Configure<VNPaySettings>(configuration.GetSection("Payment:VNPay"));

            // Đăng ký HttpClient
            services.AddHttpClient();

            // Đăng ký services
            services.AddScoped<IMoMoService, MoMoService>();
            services.AddScoped<IVNPayService, VNPayService>();
            services.AddScoped<IPaymentService, PaymentService>();

            return services;
        }

        /// <summary>
        /// Đăng ký Payment Service với cấu hình custom
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Action để cấu hình settings</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddPaymentService(
            this IServiceCollection services,
            Action<PaymentSettings> configureOptions)
        {
            // Đăng ký settings với custom configuration
            services.Configure(configureOptions);

            // Đăng ký HttpClient
            services.AddHttpClient();

            // Đăng ký services
            services.AddScoped<IMoMoService, MoMoService>();
            services.AddScoped<IVNPayService, VNPayService>();
            services.AddScoped<IPaymentService, PaymentService>();

            return services;
        }

        /// <summary>
        /// Đăng ký chỉ MoMo Service
        /// </summary>
        public static IServiceCollection AddMoMoPayment(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MoMoSettings>(configuration.GetSection("Payment:MoMo"));
            services.AddHttpClient();
            services.AddScoped<IMoMoService, MoMoService>();
            return services;
        }

        /// <summary>
        /// Đăng ký chỉ VNPay Service
        /// </summary>
        public static IServiceCollection AddVNPayPayment(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<VNPaySettings>(configuration.GetSection("Payment:VNPay"));
            services.AddScoped<IVNPayService, VNPayService>();
            return services;
        }
    }
}
