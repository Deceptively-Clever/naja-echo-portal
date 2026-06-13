using Xunit;

// Prevent parallel execution between API test classes that share WebApplicationFactory
// to avoid HostFactoryResolver interference between concurrent server startups.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
