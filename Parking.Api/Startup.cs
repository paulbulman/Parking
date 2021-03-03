namespace Parking.Api
{
    using Amazon.CognitoIdentityProvider;
    using Amazon.DynamoDBv2;
    using Amazon.S3;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleSystemsManagement;
    using Authentication;
    using Business;
    using Business.Data;
    using Converters;
    using Data;
    using Data.Aws;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Middleware;
    using NodaTime;
    using SystemClock = NodaTime.SystemClock;

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new LocalDateConverter()));

            services.AddAuthentication("Default")
                .AddScheme<AuthenticationSchemeOptions, DefaultAuthenticationHandler>("Default", null);

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.AddPolicy(
                    "IsTeamLeader",
                    policy => policy.RequireClaim("cognito:groups", Constants.TeamLeaderGroupName));
                options.AddPolicy(
                    "IsUserAdmin",
                    policy => policy.RequireClaim("cognito:groups", Constants.UserAdminGroupName));
            });

            services.AddSingleton<IClock>(SystemClock.Instance);

            services.AddScoped<IAmazonCognitoIdentityProvider, AmazonCognitoIdentityProviderClient>();
            services.AddScoped<IAmazonDynamoDB, AmazonDynamoDBClient>();
            services.AddScoped<IAmazonS3, AmazonS3Client>();
            services.AddScoped<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>();
            services.AddScoped<IAmazonSimpleSystemsManagement, AmazonSimpleSystemsManagementClient>();
            
            services.AddScoped<IDatabaseProvider, DatabaseProvider>();
            services.AddScoped<IIdentityProvider, IdentityProvider>();
            services.AddScoped<INotificationProvider, NotificationProvider>();
            services.AddScoped<ISecretProvider, SecretProvider>();
            services.AddScoped<IStorageProvider, StorageProvider>();

            services.AddScoped<IBankHolidayRepository, BankHolidayRepository>();
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IDateCalculator, DateCalculator>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IRequestRepository, RequestRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<ITriggerRepository, TriggerRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseMiddleware<HttpErrorMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
