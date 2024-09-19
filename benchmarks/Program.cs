using Npgsql;
using org.zhangwenqing.utilities;
using StackExchange.Profiling;

// Default configuration usually works for most, but override, you can call:
// MiniProfiler.Configure(new MiniProfilerOptions { ... });
const string ConnectionString = "Host=localhost;Username=tmpdba;Password=P@assword;Database=testdb";

await Setup();

var profiler = MiniProfiler.StartNew("MiniProfiler");
foreach (var t in Enum.GetValues<IdTypes>())
{
    using (profiler.Step(t.ToString()))
    {
        try
        {
            await TestInsertLargeDataset(t, 10_000_000);
        }
        catch (Exception e)
        {
            // nobody cares, just print the exception
            Console.WriteLine(e);
        }
    }
}

profiler?.Stop();
Console.WriteLine(profiler.RenderPlainText());


async Task Setup()
{
    await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
    await using var batch = dataSource.CreateBatch();
    Enum.GetValues<IdTypes>()
        .Select(it => new NpgsqlBatchCommand
        {
            CommandText = it switch
            {
                IdTypes.Int => "CREATE TABLE IF NOT EXISTS test_table_int (id BIGINT PRIMARY KEY)",
                IdTypes.Text => "CREATE TABLE IF NOT EXISTS test_table_text (id TEXT PRIMARY KEY)",
                IdTypes.GuidV4 => "CREATE TABLE IF NOT EXISTS test_table_guidv4 (id UUID PRIMARY KEY)",
                IdTypes.GuidV7 => "CREATE TABLE IF NOT EXISTS test_table_guidv7 (id UUID PRIMARY KEY)",
                _ => throw new ArgumentOutOfRangeException()
            }
        })
        .ToList()
        .ForEach(cmd => batch.BatchCommands.Add(cmd));
    await batch.ExecuteNonQueryAsync();
}

async Task TestInsertLargeDataset(IdTypes id, int count = 10_000_000)
{
    await using var dataSource = NpgsqlDataSource.Create(ConnectionString);

    await using var batch = dataSource.CreateBatch();
    for (var i = 1; i <= count; i++)
    {
        batch.BatchCommands.Add(new NpgsqlBatchCommand
        {
            CommandText = id switch
            {
                IdTypes.Int => "INSERT INTO test_table_int (id) VALUES ($1);",
                IdTypes.Text => "INSERT INTO test_table_text (id) VALUES ($1);",
                IdTypes.GuidV4 => "INSERT INTO test_table_guidv4 (id) VALUES ($1);",
                IdTypes.GuidV7 => "INSERT INTO test_table_guidv7 (id) VALUES ($1);",
                _ => throw new ArgumentOutOfRangeException()
            },
            Parameters =
            {
                new NpgsqlParameter
                {
                    Value = id switch
                    {
                        IdTypes.Int => i,
                        IdTypes.Text => Guid.NewGuid(),
                        IdTypes.GuidV4 => GuidExtensions.CreateVersion4(),
                        IdTypes.GuidV7 => GuidExtensions.CreateVersion7(),
                        _ => throw new ArgumentOutOfRangeException()
                    }
                }
            }
        });
    }

    await batch.ExecuteNonQueryAsync();
}

public enum IdTypes
{
    Int,
    Text,
    GuidV4,
    GuidV7
}