# Programmer's Guide — gRPC

Bus Service exposes gRPC for internal service-to-service communication.

## Service Definition

```protobuf
service BusService {
  rpc GetBus (GetBusRequest) returns (GetBusResponse);
  rpc ListBuses (ListBusesRequest) returns (ListBusesResponse);
}
```

## Getting a Bus by gRPC

```csharp
var channel = GrpcChannel.ForAddress("http://localhost:5201");
var client = new BusService.BusServiceClient(channel);

var response = await client.GetBusAsync(new GetBusRequest
{
  BusId = "b1c2d3e4-..."
});

Console.WriteLine($"{response.PlateNumber} — {response.Status}");
```

## Listing Buses

```csharp
var response = await client.ListBusesAsync(new ListBusesRequest
{
  Page = 1,
  PageSize = 50,
  Status = "Active"
});

foreach (var bus in response.Buses)
{
  Console.WriteLine($"{bus.BusId}: {bus.PlateNumber}");
}
```

## Authentication

gRPC calls use the same JWT bearer token. Attach it via metadata:

```csharp
var credentials = CallCredentials.FromInterceptor((context, metadata) =>
{
  metadata.Add("Authorization", $"Bearer {jwtToken}");
  return Task.CompletedTask;
});

var channel = GrpcChannel.ForAddress("http://localhost:5201", new GrpcChannelOptions
{
  Credentials = ChannelCredentials.SecureSsl.With(credentials)
});
```
