export type BookingStatus = 'PendingPayment' | 'Confirmed' | 'Cancelled' | 'Expired' | 'Refunded';

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
