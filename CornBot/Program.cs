using CornBot;
using CornBot.Utilities;

SimpleRNG.SetSeedFromSystemTime();

var client = new CornClient();

await client.MainAsync();
