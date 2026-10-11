namespace Atc.Kusto.Tests.Handlers.Internal;

/// <summary>
/// Streaming queries map each row from an <see cref="IDataRecord"/>.
/// These tests pin that a row maps to the JSON the cluster returned, and to the same object in every path.
/// </summary>
public sealed class StreamingMappingParityTests
{
    /// <summary>
    /// Captured from help.kusto.windows.net (2026-10-10) for
    /// <c>print b=true, bn=bool(null), i=int(7), n=int(null), l=long(5), d=decimal(1.25), r=real(2.5),
    /// rb=real(1234.5), g=guid(74be27de-1e4e-49d9-b579-fe0b331d3642), dy=dynamic({"a":1,"e":"X"}), dn=dynamic(null),
    /// da=dynamic([1,2]), dt=datetime(2024-01-02 03:04:05.678), ts=1h, s="x", sn=""</c>.
    /// </summary>
    private const string CapturedJson =
        """{"b":true,"bn":null,"i":7,"n":null,"l":5,"d":1.25,"r":2.5,"rb":1234.5,"g":"74be27de-1e4e-49d9-b579-fe0b331d3642","dy":{"a":1,"e":"X"},"dn":null,"da":[1,2],"dt":"2024-01-02T03:04:05.678Z","ts":"01:00:00","s":"x","sn":""}""";

    private static readonly (string Name, Type Type)[] CapturedColumns =
    [
        ("b", typeof(bool)),
        ("bn", typeof(bool)),
        ("i", typeof(int)),
        ("n", typeof(int)),
        ("l", typeof(long)),
        ("d", typeof(SqlDecimal)),
        ("r", typeof(double)),
        ("rb", typeof(double)),
        ("g", typeof(Guid)),
        ("dy", typeof(object)),
        ("dn", typeof(object)),
        ("da", typeof(object)),
        ("dt", typeof(DateTime)),
        ("ts", typeof(TimeSpan)),
        ("s", typeof(string)),
        ("sn", typeof(string)),
    ];

    public enum DeviceType
    {
        Unknown,
        ElectricVehicleChargingStation,
        IotDevice,
    }

    public enum ProviderType
    {
        Unknown,
        Monta,
    }

    [Fact]
    public void A_Fragment_Row_Maps_To_The_Json_The_Cluster_Returned()
    {
        // Arrange - the values exactly as the SDK sent them in the TableFragment frame (invariant culture)
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            object?[] values =
            [
                "True",                                                         // b
                DBNull.Value,                                                   // bn
                "7",                                                            // i
                DBNull.Value,                                                   // n
                "5",                                                            // l
                "1.25",                                                         // d
                "2.5",                                                          // r
                "1234.5",                                                       // rb
                "74be27de-1e4e-49d9-b579-fe0b331d3642",                         // g
                Newtonsoft.Json.Linq.JObject.Parse("""{"a":1,"e":"X"}"""),      // dy
                DBNull.Value,                                                   // dn
                Newtonsoft.Json.Linq.JArray.Parse("[1,2]"),                     // da
                "2024-01-02T03:04:05.678Z",                                     // dt
                "01:00:00",                                                     // ts
                "x",                                                            // s
                string.Empty,                                                   // sn
            ];

            var record = CreateTable(CapturedColumns).CreateRecord(values);

            // Act
            var json = record.ReadObject<JsonElement>().GetRawText();

            // Assert
            json.Should().Be(CapturedJson);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void A_DataTable_Frame_Row_Maps_To_The_Json_The_Cluster_Returned()
    {
        // Arrange - the values as the SDK's reader returns them without progressive results
        using var table = CreateDataTable(CapturedColumns);
        var row = table.NewRow();
        row.ItemArray =
        [
            true,                                                           // b
            DBNull.Value,                                                   // bn
            7,                                                              // i
            DBNull.Value,                                                   // n
            5L,                                                             // l
            new SqlDecimal(1.25m),                                          // d
            2.5,                                                            // r
            1234.5,                                                         // rb
            Guid.Parse("74be27de-1e4e-49d9-b579-fe0b331d3642"),             // g
            Newtonsoft.Json.Linq.JObject.Parse("""{"a":1,"e":"X"}"""),      // dy
            Newtonsoft.Json.Linq.JValue.CreateNull(),                       // dn
            Newtonsoft.Json.Linq.JArray.Parse("[1,2]"),                     // da
            new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc),       // dt
            TimeSpan.FromHours(1),                                          // ts
            "x",                                                            // s
            string.Empty,                                                   // sn
        ];
        table.Rows.Add(row);

        using var reader = table.CreateDataReader();
        reader.Read();

        // Act
        var json = reader.ReadObject<JsonElement>().GetRawText();

        // Assert
        json.Should().Be(CapturedJson);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Device_Row_Maps_To_The_Same_Device_In_Every_Path(
        bool hasParent)
    {
        // Arrange - shaped like the rows of kl-iot-insights-api's ListDevicesQuery
        const string id = "74be27de-1e4e-49d9-b579-fe0b331d3642";
        const string parentId = "0b5c4a8e-3f0d-4c1e-9a3b-2d6f7e8a9b10";
        const string providerData = """{"ProviderDeviceId":"cp-42","ProviderName":"Monta","ProviderType":"Monta"}""";
        const string properties = """[{"Key":"Power","Value":"22","ValueType":"int","UnitOfMeasurement":"kW"},{"Key":"Site","Value":"HQ","ValueType":"string","UnitOfMeasurement":""}]""";

        var expected = new Device(
            Guid.Parse(id),
            hasParent ? Guid.Parse(parentId) : null,
            "Charger 1",
            DeviceType.ElectricVehicleChargingStation,
            "Brand",
            string.Empty,
            "SN-1",
            new ProviderData("cp-42", "Monta", ProviderType.Monta),
            [
                new DeviceProperty("Power", "22", "int", "kW"),
                new DeviceProperty("Site", "HQ", "string", string.Empty),
            ]);

        // Progressive: values as the SDK sends them in a TableFragment frame
        object?[] fragmentValues =
        [
            id,                                                                     // Id
            hasParent ? new Newtonsoft.Json.Linq.JValue(parentId) : DBNull.Value,   // ParentDeviceId
            "Charger 1",                                                            // Name
            "ElectricVehicleChargingStation",                                       // DeviceType
            "Brand",                                                                // BrandName
            string.Empty,                                                           // ModelName
            "SN-1",                                                                 // SerialNumber
            Newtonsoft.Json.Linq.JObject.Parse(providerData),                       // ProviderData
            Newtonsoft.Json.Linq.JArray.Parse(properties),                          // DeviceProperties
        ];

        // Without progressive results: typed values as the SDK's reader returns them
        using var table = CreateDataTable(DeviceColumns);
        var row = table.NewRow();
        row.ItemArray =
        [
            Guid.Parse(id),                                                                                     // Id
            hasParent ? new Newtonsoft.Json.Linq.JValue(parentId) : Newtonsoft.Json.Linq.JValue.CreateNull(),   // ParentDeviceId
            "Charger 1",                                                                                        // Name
            "ElectricVehicleChargingStation",                                                                   // DeviceType
            "Brand",                                                                                            // BrandName
            string.Empty,                                                                                       // ModelName
            "SN-1",                                                                                             // SerialNumber
            Newtonsoft.Json.Linq.JObject.Parse(providerData),                                                   // ProviderData
            Newtonsoft.Json.Linq.JArray.Parse(properties),                                                      // DeviceProperties
        ];
        table.Rows.Add(row);

        using var reader = table.CreateDataReader();
        reader.Read();

        using var readerForKustoQuery = table.CreateDataReader();

        var query = new DevicesQuery();

        // Act
        var fromMapDataRow = table.Rows[0].MapDataRow<Device>();
        var fromFragment = query.MapRecord(CreateTable(DeviceColumns).CreateRecord(fragmentValues));
        var fromReader = query.MapRecord(reader);
        var fromKustoQuery = new DevicesListQuery().ReadResult(readerForKustoQuery);

        // Assert
        fromMapDataRow.Should().BeEquivalentTo(expected);
        fromFragment.Should().BeEquivalentTo(expected);
        fromReader.Should().BeEquivalentTo(expected);
        fromKustoQuery.Should().ContainSingle().Which.Should().BeEquivalentTo(expected);
    }

    private static (string Name, Type Type)[] DeviceColumns =>
    [
        ("Id", typeof(Guid)),
        ("ParentDeviceId", typeof(object)),
        ("Name", typeof(string)),
        ("DeviceType", typeof(string)),
        ("BrandName", typeof(string)),
        ("ModelName", typeof(string)),
        ("SerialNumber", typeof(string)),
        ("ProviderData", typeof(object)),
        ("DeviceProperties", typeof(object)),
    ];

    private static DataTable CreateDataTable((string Name, Type Type)[] columns)
    {
        var table = new DataTable(nameof(WellKnownDataSet.PrimaryResult));
        foreach (var (name, type) in columns)
        {
            // Keep DateTime values UTC, like the SDK's reader returns them.
            var column = table.Columns.Add(name, type);
            if (type == typeof(DateTime))
            {
                column.DateTimeMode = DataSetDateTime.Utc;
            }
        }

        return table;
    }

    private static FrameTable CreateTable((string Name, Type Type)[] columns)
    {
        using var schema = CreateDataTable(columns);
        return FrameTable.FromSchemaFrame(new SchemaFrameStub(1, schema));
    }

    public sealed record ProviderData(
        string ProviderDeviceId,
        string ProviderName,
        ProviderType ProviderType);

    public sealed record DeviceProperty(
        string Key,
        string Value,
        string ValueType,
        string UnitOfMeasurement);

    public sealed record Device(
        Guid Id,
        Guid? ParentDeviceId,
        string Name,
        DeviceType DeviceType,
        string? BrandName,
        string? ModelName,
        string? SerialNumber,
        ProviderData ProviderData,
        IReadOnlyList<DeviceProperty> DeviceProperties);

    private sealed record DevicesQuery : KustoStreamingQuery<Device>;

    private sealed record DevicesListQuery : KustoQuery<Device>;
}