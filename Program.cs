var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/events", async (HttpResponse response, CancellationToken cancellationToken) =>
{
	response.Headers.ContentType = "text/event-stream";
	response.Headers.CacheControl = "no-cache";

	await foreach (var number in GenerateEvents(cancellationToken))
	{
		await response.WriteAsync($"event: tick\ndata: {{\"number\":{number},\"sentAt\":\"{DateTimeOffset.UtcNow:O}\"}}\n\n", cancellationToken);
		await response.Body.FlushAsync(cancellationToken);
	}
});

static async IAsyncEnumerable<int> GenerateEvents(
	[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
{
	var number = 0;

	while (!cancellationToken.IsCancellationRequested)
	{
		await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
		yield return ++number;
	}
}

app.Run();
