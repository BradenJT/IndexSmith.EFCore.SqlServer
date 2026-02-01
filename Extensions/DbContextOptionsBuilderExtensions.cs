using IndexSmith.EFCore.SqlServer.Conventions;
using IndexSmith.EFCore.SqlServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IndexSmith.EFCore.SqlServer.Extensions;

/// <summary>
/// Extension methods for configuring IndexSmith on DbContextOptionsBuilder.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables IndexSmith automatic indexing with default options.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder UseIndexSmith(
        this DbContextOptionsBuilder optionsBuilder)
    {
        return optionsBuilder.UseIndexSmith(_ => { });
    }

    /// <summary>
    /// Enables IndexSmith automatic indexing with custom configuration.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="configure">Action to configure IndexSmith options.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder UseIndexSmith(
        this DbContextOptionsBuilder optionsBuilder,
        Action<AutoIndexOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AutoIndexOptionsBuilder();
        configure(builder);
        var options = builder.Build();

        // Register the extension
        var extension = optionsBuilder.Options.FindExtension<IndexSmithOptionsExtension>()
            ?? new IndexSmithOptionsExtension(options);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    /// <summary>
    /// Enables IndexSmith automatic indexing with default options.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder<TContext> UseIndexSmith<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)
            ((DbContextOptionsBuilder)optionsBuilder).UseIndexSmith();
    }

    /// <summary>
    /// Enables IndexSmith automatic indexing with custom configuration.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="configure">Action to configure IndexSmith options.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder<TContext> UseIndexSmith<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Action<AutoIndexOptionsBuilder> configure)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)
            ((DbContextOptionsBuilder)optionsBuilder).UseIndexSmith(configure);
    }
}

/// <summary>
/// EF Core options extension for IndexSmith.
/// </summary>
internal sealed class IndexSmithOptionsExtension : IDbContextOptionsExtension
{
    private readonly AutoIndexOptions _options;
    private DbContextOptionsExtensionInfo? _info;

    public IndexSmithOptionsExtension(AutoIndexOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public AutoIndexOptions Options => _options;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // Register the convention plugin
        services.AddSingleton<IConventionSetPlugin>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<AutoIndexConventionPlugin>();
            return new AutoIndexConventionPlugin(_options, logger);
        });
    }

    public void Validate(IDbContextOptions options)
    {
        // Validate that SQL Server provider is being used
        var sqlServerExtension = options.Extensions
            .FirstOrDefault(e => e.GetType().Name.Contains("SqlServer"));

        if (sqlServerExtension == null)
        {
            throw new InvalidOperationException(
                "IndexSmith.EFCore.SqlServer requires the SQL Server provider. " +
                "Ensure UseSqlServer() is called before UseIndexSmith().");
        }
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using IndexSmith ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["IndexSmith:Enabled"] = "true";
        }
    }
}
