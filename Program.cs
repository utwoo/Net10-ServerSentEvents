var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/events", (CancellationToken cancellationToken) =>
	TypedResults.ServerSentEvents(GenerateEvents(cancellationToken)));

static async IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<object>> GenerateEvents(
	[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
{
	var number = 0;

	while (!cancellationToken.IsCancellationRequested)
	{
		await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
		yield return new System.Net.ServerSentEvents.SseItem<object>(
			new { number = ++number, sentAt = DateTimeOffset.UtcNow },
			eventType: "tick");
	}
}

app.Run();
