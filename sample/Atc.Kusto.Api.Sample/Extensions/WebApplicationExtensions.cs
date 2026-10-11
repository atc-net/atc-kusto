namespace Atc.Kusto.Api.Sample.Extensions;

public static class WebApplicationExtensions
{
    public static IApplicationBuilder ConfigureScalar(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference("/scalar", options =>
        {
            options.Title = "Atc.Kusto Api Sample";
            options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        return app;
    }
}