export interface RouteDef {
  routeId: string;
  name: string;
  originCity: string;
  destinationCity: string;
  distanceKm: number;
  estimatedDurationMinutes: number;
  activeTrips: number;
}
