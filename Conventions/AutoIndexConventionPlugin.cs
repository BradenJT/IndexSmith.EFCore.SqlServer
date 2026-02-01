using IndexSmith.EFCore.SqlServer.Options;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.Logging;

namespace IndexSmith.EFCore.SqlServer.Conventions;

/// <summary>
/// Convention plugin that registers IndexSmith conventions with EF Core.
/// </summary>
public sealed class AutoIndexConventionPlugin : IConventionSetPlugin
{
    private readonly AutoIndexOptions _options;
    private readonly ILogger? _logger;

    public AutoIndexConventionPlugin(AutoIndexOptions options, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.ModelFinalizingConventions.Add(
            new AutoIndexModelFinalizingConvention(_options, _logger));

        return conventionSet;
    }
}
