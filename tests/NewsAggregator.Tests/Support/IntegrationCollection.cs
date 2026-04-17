using Xunit;

namespace NewsAggregator.Tests.Support;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationHostFixture>;
