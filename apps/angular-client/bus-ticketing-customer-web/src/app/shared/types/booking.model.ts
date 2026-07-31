export type BookingStatus = 'PendingPayment' | 'Confirmed' | 'Cancelled' | 'Expired' | 'Refunded';

export interface PassengerInput {
  seatNumber: string;
  fullName: string;
  age: number;
  gender: 'Male' | 'Female' | 'Other';
}

export interface CreateBookingRequest {
  tripId: string;
  customerId: string;
  passengers: PassengerInput[];
}

export interface BookingSeat {
  seatNumber: string;
  passengerFullName: string;
}

export interface Booking {
  bookingId: string;
  tripId: string;
  customerId: string;
  status: BookingStatus;
  totalAmount: number;
  currency: string;
  createdAtUtc: string;
  holdExpiresAtUtc: string;
  seats: BookingSeat[];
}
