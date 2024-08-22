using Microsoft.AspNetCore.Mvc;
using CRDOrderService.Services;

[assembly: ApiController]
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapControllers();

Console.WriteLine("Creating pubsub subscriber");

string projectId = "qwiklabs-gcp-03-be475c229b90";
string subscriptionId = "ap1";

// Create an instance of your message puller
var messagePuller = new PullMessagesAsyncSample();

// Start pulling messages in the background
await messagePuller.PullMessagesAsync(projectId, subscriptionId, true); // Acknowledge messages

app.Run();

