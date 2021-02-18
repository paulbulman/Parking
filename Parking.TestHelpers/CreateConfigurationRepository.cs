namespace Parking.TestHelpers
{
    using Business.Data;
    using Model;
    using Moq;

    public static class CreateConfigurationRepository
    {
        public static IConfigurationRepository WithDefaultConfiguration() =>
            WithConfiguration(CreateConfiguration.With());

        public static IConfigurationRepository WithConfiguration(Configuration configuration)
        {
            var mockConfigurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);

            mockConfigurationRepository
                .Setup(r => r.GetConfiguration())
                .ReturnsAsync(configuration);

            return mockConfigurationRepository.Object;
        }
    }
}