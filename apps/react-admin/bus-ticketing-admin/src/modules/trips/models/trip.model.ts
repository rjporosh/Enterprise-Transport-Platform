export type TripStatus = 'Scheduled' | 'InTransit' | 'Completed' | 'Cancelled';

export interface Trip {
  tripId: string;
  routeName: string;
  originCity: string;
  destinationCity: string;
  departureUtc: string;
  arrivalUtc: string;
  busPlateNumber: string;
  operator: string;
  status: TripStatus;
  totalSeats: number;
  availableSeats: number;
  pricePerSeat: number;
}
