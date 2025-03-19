using EfCore.Ydb.Extensions;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Ydb.FunctionalTests.TestUtilities;

public class YdbTestStoreFactory(string? additionalSql = null) : RelationalTestStoreFactory
{
    public static YdbTestStoreFactory Instance { get; } = new();

    private readonly string? _scriptPath = null;

    public override TestStore Create(string storeName) =>
        new YdbTestStore(storeName, _scriptPath, additionalSql, shared: false);

    public override TestStore GetOrCreate(string storeName)
        => new YdbTestStore(storeName, _scriptPath, additionalSql, shared: true);

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkYdb();
}
