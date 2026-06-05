using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vistora.Core.Entities;

public abstract class AbstractContext<TContext> : DbContext, IContext
    where TContext : DbContext
{
    public const int DefaultMaxStringLength = 4000;
    public const int MaxNameLength = 256;
    public const int MaxDataSourceTypeLength = 64;
    public const int MaxVisualizationTypeLength = 64;
    public const int MaxHostLength = 512;
    public const int MaxDatabaseLength = 256;
    public const int MaxUsernameLength = 256;
    public const int MaxSchemaLength = 256;
    public const int MaxTableLength = 256;
    public const int MaxAliasLength = 256;
    public const int MaxDelimiterLength = 8;
    public const int MaxColumnNameLength = 256;
    public const int MaxColumnTypeLength = 128;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly ValueComparer<Dictionary<string, string?>> StringDictionaryComparer =
        new(
            (left, right) => StringDictionaryEquals(left, right),
            value => StringDictionaryHashCode(value),
            value => value == null ? new Dictionary<string, string?>() : new Dictionary<string, string?>(value)
        );

    private static readonly ValueComparer<Dictionary<string, object?>> ObjectDictionaryComparer =
        new(
            (left, right) => ObjectDictionaryEquals(left, right),
            value => ObjectDictionaryHashCode(value),
            value => value == null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(value)
        );

    protected AbstractContext(DbContextOptions<TContext> options)
        : base(options)
    {
    }

    public DbSet<DataSource> DataSources { get; set; } = null!;

    public DbSet<Dataset> Datasets { get; set; } = null!;

    public DbSet<Chart> Charts { get; set; } = null!;

    public DbSet<Dashboard> Dashboards { get; set; } = null!;

    public Task<int> SaveChangesAsync() => SaveChangesAsync(default);

    public virtual async Task RunMigrationsAsync(CancellationToken cancellationToken)
        => await Database.MigrateAsync(cancellationToken);

    public abstract bool IsUniqueConstraintViolationException(DbUpdateException exception);

    public virtual bool SupportsLimitInSubqueries => true;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<DataSource>(BuildDataSourceEntity);
        builder.Entity<Dataset>(BuildDatasetEntity);
        builder.Entity<DatasetDataSource>(BuildDatasetDataSourceEntity);
        builder.Entity<DatasetColumn>(BuildDatasetColumnEntity);
        builder.Entity<Chart>(BuildChartEntity);
        builder.Entity<Dashboard>(BuildDashboardEntity);
        builder.Entity<DashboardChart>(BuildDashboardChartEntity);
    }

    private static void BuildDataSourceEntity(EntityTypeBuilder<DataSource> dataSource)
    {
        dataSource.HasKey(source => source.Id);
        dataSource.HasIndex(source => source.Name).IsUnique();

        dataSource.Property(source => source.Name)
            .HasMaxLength(MaxNameLength)
            .IsRequired();

        dataSource.Property(source => source.Type)
            .HasMaxLength(MaxDataSourceTypeLength)
            .IsRequired();

        dataSource.OwnsOne(source => source.Configuration, BuildDataSourceConfiguration);

        dataSource.Navigation(source => source.Configuration).IsRequired();

        dataSource.HasMany(source => source.Datasets)
            .WithOne(link => link.DataSource)
            .HasForeignKey(link => link.DataSourceId)
            .IsRequired();
    }

    private static void BuildDataSourceConfiguration(
        OwnedNavigationBuilder<DataSource, DataSourceConfiguration> configuration
    )
    {
        configuration.Property(value => value.ConnectionString).HasMaxLength(DefaultMaxStringLength);
        configuration.Property(value => value.Host).HasMaxLength(MaxHostLength);
        configuration.Property(value => value.Database).HasMaxLength(MaxDatabaseLength);
        configuration.Property(value => value.Username).HasMaxLength(MaxUsernameLength);
        configuration.Property(value => value.Password).HasMaxLength(DefaultMaxStringLength);
        configuration.Property(value => value.Schema).HasMaxLength(MaxSchemaLength);
        configuration.Property(value => value.Path).HasMaxLength(DefaultMaxStringLength);
        configuration.Property(value => value.Table).HasMaxLength(MaxTableLength);
        configuration.Property(value => value.Alias).HasMaxLength(MaxAliasLength);
        configuration.Property(value => value.Delimiter).HasMaxLength(MaxDelimiterLength);
        configuration.Property(value => value.Sheet).HasMaxLength(MaxNameLength);

        configuration.Property(value => value.Options)
            .HasMaxLength(DefaultMaxStringLength)
            .HasConversion(
                value => SerializeStringDictionary(value),
                value => DeserializeStringDictionary(value)
            )
            .Metadata.SetValueComparer(StringDictionaryComparer);
    }

    private static void BuildDatasetEntity(EntityTypeBuilder<Dataset> dataset)
    {
        dataset.HasKey(value => value.Id);
        dataset.HasIndex(value => value.Name).IsUnique();

        dataset.Property(value => value.Name)
            .HasMaxLength(MaxNameLength)
            .IsRequired();

        dataset.Property(value => value.Sql).IsRequired();
        dataset.Property(value => value.Description).HasMaxLength(DefaultMaxStringLength);

        dataset.HasMany(value => value.DataSources)
            .WithOne(value => value.Dataset)
            .HasForeignKey(value => value.DatasetId)
            .IsRequired();

        dataset.HasMany(value => value.Columns)
            .WithOne(value => value.Dataset)
            .HasForeignKey(value => value.DatasetId)
            .IsRequired();

        dataset.HasMany(value => value.Charts)
            .WithOne(value => value.Dataset)
            .HasForeignKey(value => value.DatasetId)
            .IsRequired();
    }

    private static void BuildDatasetDataSourceEntity(EntityTypeBuilder<DatasetDataSource> datasetDataSource)
    {
        datasetDataSource.HasKey(value => new { value.DatasetId, value.DataSourceId });

        datasetDataSource.Property(value => value.Alias).HasMaxLength(MaxAliasLength);

        datasetDataSource.HasIndex(value => new { value.DatasetId, value.SortOrder });
    }

    private static void BuildDatasetColumnEntity(EntityTypeBuilder<DatasetColumn> column)
    {
        column.HasKey(value => value.Id);
        column.HasIndex(value => new { value.DatasetId, value.Name }).IsUnique();
        column.HasIndex(value => new { value.DatasetId, value.Ordinal }).IsUnique();

        column.Property(value => value.Name)
            .HasMaxLength(MaxColumnNameLength)
            .IsRequired();

        column.Property(value => value.Type)
            .HasMaxLength(MaxColumnTypeLength)
            .IsRequired();
    }

    private static void BuildChartEntity(EntityTypeBuilder<Chart> chart)
    {
        chart.HasKey(value => value.Id);
        chart.HasIndex(value => value.Name);

        chart.Property(value => value.Name)
            .HasMaxLength(MaxNameLength)
            .IsRequired();

        chart.Property(value => value.VisualizationType)
            .HasMaxLength(MaxVisualizationTypeLength)
            .IsRequired();

        chart.Property(value => value.Configuration)
            .HasMaxLength(DefaultMaxStringLength)
            .HasConversion(
                value => SerializeObjectDictionary(value),
                value => DeserializeObjectDictionary(value)
            )
            .Metadata.SetValueComparer(ObjectDictionaryComparer);

        chart.HasMany(value => value.DashboardCharts)
            .WithOne(value => value.Chart)
            .HasForeignKey(value => value.ChartId)
            .IsRequired();
    }

    private static void BuildDashboardEntity(EntityTypeBuilder<Dashboard> dashboard)
    {
        dashboard.HasKey(value => value.Id);
        dashboard.HasIndex(value => value.Name).IsUnique();

        dashboard.Property(value => value.Name)
            .HasMaxLength(MaxNameLength)
            .IsRequired();

        dashboard.Property(value => value.Description).HasMaxLength(DefaultMaxStringLength);

        dashboard.HasMany(value => value.Charts)
            .WithOne(value => value.Dashboard)
            .HasForeignKey(value => value.DashboardId)
            .IsRequired();
    }

    private static void BuildDashboardChartEntity(EntityTypeBuilder<DashboardChart> dashboardChart)
    {
        dashboardChart.HasKey(value => new { value.DashboardId, value.ChartId });
        dashboardChart.HasIndex(value => new { value.DashboardId, value.Order });
    }

    private static string SerializeStringDictionary(Dictionary<string, string?> value)
        => JsonSerializer.Serialize(value, JsonOptions);

    private static Dictionary<string, string?> DeserializeStringDictionary(string value)
        => JsonSerializer.Deserialize<Dictionary<string, string?>>(value, JsonOptions) ?? new();

    private static string SerializeObjectDictionary(Dictionary<string, object?> value)
        => JsonSerializer.Serialize(value, JsonOptions);

    private static Dictionary<string, object?> DeserializeObjectDictionary(string value)
        => JsonSerializer.Deserialize<Dictionary<string, object?>>(value, JsonOptions) ?? new();

    private static bool StringDictionaryEquals(
        Dictionary<string, string?>? left,
        Dictionary<string, string?>? right
    )
        => DictionaryEquals(left, right);

    private static int StringDictionaryHashCode(Dictionary<string, string?>? value)
        => DictionaryHashCode(value);

    private static bool ObjectDictionaryEquals(
        Dictionary<string, object?>? left,
        Dictionary<string, object?>? right
    )
        => DictionaryEquals(left, right);

    private static int ObjectDictionaryHashCode(Dictionary<string, object?>? value)
        => DictionaryHashCode(value);

    private static bool DictionaryEquals<TValue>(
        IReadOnlyDictionary<string, TValue?>? left,
        IReadOnlyDictionary<string, TValue?>? right
    )
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, leftValue) in left)
        {
            if (!right.TryGetValue(key, out var rightValue)
                || !EqualityComparer<TValue?>.Default.Equals(leftValue, rightValue))
            {
                return false;
            }
        }

        return true;
    }

    private static int DictionaryHashCode<TValue>(IReadOnlyDictionary<string, TValue?>? value)
    {
        if (value is null)
        {
            return 0;
        }

        var hashCode = new HashCode();
        foreach (var item in value.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            hashCode.Add(item.Key, StringComparer.Ordinal);
            hashCode.Add(item.Value);
        }

        return hashCode.ToHashCode();
    }
}
