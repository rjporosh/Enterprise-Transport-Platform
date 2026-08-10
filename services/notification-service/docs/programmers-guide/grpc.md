# gRPC

## Proto Definition

`Api/Protos/notification.proto`:
```protobuf
syntax = "proto3";

package notification;

service NotificationGrpcService {
  rpc SendNotification (SendNotificationRequest) returns (SendNotificationResponse);
  rpc GetNotificationStatus (GetNotificationStatusRequest) returns (GetNotificationStatusResponse);
}

message SendNotificationRequest {
  string recipient = 1;
  string channel = 2;
  string subject = 3;
  string body = 4;
  string priority = 5;
  bool is_transactional = 6;
}

message SendNotificationResponse {
  bool success = 1;
  string message = 2;
  string notification_id = 3;
  string status = 4;
}

message GetNotificationStatusRequest {
  string notification_id = 1;
}

message GetNotificationStatusResponse {
  bool success = 1;
  string notification_id = 2;
  string status = 3;
}
```

## Implementation

`Api/Grpc/NotificationGrpcServiceImpl.cs` maps gRPC calls to MediatR commands/queries.

## Usage from Other Services

```csharp
var channel = GrpcChannel.ForAddress("http://notification-service:5301");
var client = new NotificationGrpcService.NotificationGrpcServiceClient(channel);
var response = await client.SendNotificationAsync(new SendNotificationRequest { ... });
```

## Correlation

gRPC metadata propagates `CorrelationId` and `TraceId`. The implementation extracts these from the gRPC context.
