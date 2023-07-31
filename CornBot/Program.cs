
using CornBot;
using CornBot.Utilities;

SimpleRNG.SetSeedFromSystemTime();

var client = new CornClient();
var api = new CornAPI();

//await api.RunAsync();
await client.MainAsync();

