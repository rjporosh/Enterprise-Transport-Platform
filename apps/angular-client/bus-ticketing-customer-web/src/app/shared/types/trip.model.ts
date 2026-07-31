export interface TripSearchResult {
  tripId: string;
  originCity: string;
  destinationCity: string;
  departureUtc: string;
  arrivalUtc: string;
  busType: string;
  operatorPlateNumber: string;
  pricePerSeat: number;
  currency: string;
  availableSeats: number;
  totalSeats: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface TripSearchCriteria {
  origin: string;
  destination: string;
  date: string; // yyyy-MM-dd
}
