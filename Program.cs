using PDFArchiveViewer.Components;
using PDFArchiveViewer.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<PDFIndexStore>();
builder.Services.AddHostedService<BuildPDFIndex>();
builder.Services.AddSingleton<PDFSearchService>();
builder.Services.AddBlazorBootstrap();

builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();   // app refuses to start if ArchiveRootFolder is blank

builder.Services.AddSingleton<PDFIndexStore>();
builder.Services.AddHostedService<BuildPDFIndex>();


var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// mapping endpoint so that browsers can recieve pdf files
app.MapGet("/pdf/{category}/{date}", (string category, string date, PDFIndexStore store) =>
{
    if (!DateOnly.TryParseExact(date, "yyyyMMdd", out var d))
        return Results.BadRequest();

    if (store.Index.TryGetValue(category, out var files) && files.TryGetValue(d, out var path))
        return Results.File(path, "application/pdf");

    return Results.NotFound();
});

app.Run();
