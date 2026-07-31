import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useBooking } from '../hooks/useBooking';
import { useCancelBooking } from '../hooks/useCancelBooking';

export default function BookingDetailPage() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const { data: booking, isLoading, isError } = useBooking(bookingId);
  const cancelBooking = useCancelBooking();
  const [reason, setReason] = useState('');

  if (isLoading) return <div className="p-8 text-ink-700">Loading booking...</div>;
  if (isError || !booking) {
    return (
      <div className="p-8">
        <p className="text-red-700">Booking not found.</p>
        <Link to="/bookings" className="text-saffron-600 hover:underline text-sm">
          &larr; Back to bookings
        </Link>
      </div>
    );
  }

  const canCancel = booking.status === 'PendingPayment' || booking.status === 'Confirmed';

  return (
    <div className="p-8 max-w-2xl">
      <Link to="/bookings" className="text-saffron-600 hover:underline text-sm">
        &larr; Back to bookings
      </Link>

      <h1 className="font-display text-3xl text-ink-950 mt-3 mb-1">Booking {booking.bookingId.slice(0, 8)}</h1>
      <p className="text-ink-700/60 mb-6">Trip {booking.tripId}</p>

      <div className="bg-white border border-ink-800/10 rounded-xl p-6 shadow-sm">
        <dl className="grid grid-cols-2 gap-y-3 text-sm">
          <dt className="text-ink-700/60">Customer</dt>
          <dd className="text-right font-mono">{booking.customerId}</dd>

          <dt className="text-ink-700/60">Status</dt>
          <dd className="text-right font-medium">{booking.status}</dd>

          <dt className="text-ink-700/60">Total</dt>
          <dd className="text-right font-medium">
            {booking.totalAmount.toFixed(0)} {booking.currency}
          </dd>

          <dt className="text-ink-700/60">Created</dt>
          <dd className="text-right">{new Date(booking.createdAtUtc).toLocaleString()}</dd>
        </dl>

        <hr className="my-4 border-ink-800/10" />

        <p className="text-sm font-medium text-ink-950 mb-2">Seats</p>
        <ul className="flex flex-col gap-1">
          {booking.seats.map((seat) => (
            <li key={seat.seatNumber} className="flex justify-between text-sm">
              <span>{seat.seatNumber}</span>
              <span className="text-ink-700/60">{seat.passengerFullName}</span>
            </li>
          ))}
        </ul>
      </div>

      {canCancel && (
        <div className="mt-6 bg-white border border-ink-800/10 rounded-xl p-6 shadow-sm">
          <p className="text-sm font-medium text-ink-950 mb-2">Cancel this booking</p>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Reason (visible to the customer)"
            className="w-full border border-ink-800/10 rounded-md px-3 py-2 text-sm mb-3"
            rows={2}
          />
          <button
            disabled={!reason.trim() || cancelBooking.isPending}
            onClick={() => cancelBooking.mutate({ bookingId: booking.bookingId, customerId: booking.customerId, reason })}
            className="bg-red-600 disabled:opacity-40 hover:bg-red-700 text-white text-sm font-medium rounded-md px-4 py-2"
          >
            {cancelBooking.isPending ? 'Cancelling...' : 'Cancel booking'}
          </button>
          {cancelBooking.isError && (
            <p className="text-red-700 text-sm mt-2">Could not cancel this booking. Please try again.</p>
          )}
        </div>
      )}
    </div>
  );
}
