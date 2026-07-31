import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useBookings } from '../hooks/useBookings';
import { BookingStatus } from '../models/booking.model';

const STATUS_STYLES: Record<BookingStatus, string> = {
  PendingPayment: 'bg-saffron-500/20 text-saffron-600',
  Confirmed: 'bg-emerald-100 text-emerald-700',
  Cancelled: 'bg-red-100 text-red-700',
  Expired: 'bg-ink-800/10 text-ink-800',
  Refunded: 'bg-blue-100 text-blue-700'
};

export default function BookingsListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, error } = useBookings({ page, pageSize: 20 });

  return (
    <div className="p-8">
      <div className="mb-6">
        <p className="text-saffron-600 text-sm tracking-[0.2em] uppercase mb-1">Operations</p>
        <h1 className="font-display text-3xl text-ink-950">Bookings</h1>
      </div>

      {isLoading && <p className="text-ink-700">Loading bookings...</p>}

      {isError && (
        <p className="text-red-700 bg-red-50 border border-red-200 rounded-md px-4 py-3">
          {(error as Error)?.message ?? 'Unable to load bookings.'}
        </p>
      )}

      {data && (
        <div className="bg-white border border-ink-800/10 rounded-xl overflow-hidden shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-ink-950 text-white/80 text-left">
              <tr>
                <th className="px-4 py-3 font-medium">Booking</th>
                <th className="px-4 py-3 font-medium">Trip</th>
                <th className="px-4 py-3 font-medium">Seats</th>
                <th className="px-4 py-3 font-medium">Amount</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Created</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((booking) => (
                <tr key={booking.bookingId} className="border-t border-ink-800/10 hover:bg-slate-50">
                  <td className="px-4 py-3">
                    <Link to={`/bookings/${booking.bookingId}`} className="text-saffron-600 font-mono hover:underline">
                      {booking.bookingId.slice(0, 8)}
                    </Link>
                  </td>
                  <td className="px-4 py-3 font-mono text-ink-700">{booking.tripId.slice(0, 8)}</td>
                  <td className="px-4 py-3 text-ink-700">{booking.seats.length}</td>
                  <td className="px-4 py-3 text-ink-950 font-medium">
                    {booking.totalAmount.toFixed(0)} {booking.currency}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_STYLES[booking.status]}`}>
                      {booking.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-ink-700">{new Date(booking.createdAtUtc).toLocaleString()}</td>
                </tr>
              ))}

              {data.items.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-ink-700/60">
                    No bookings found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <div className="flex items-center justify-between px-4 py-3 border-t border-ink-800/10">
            <p className="text-xs text-ink-700/60">
              Page {data.page} &middot; {data.totalCount} total bookings
            </p>
            <div className="flex gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="text-sm px-3 py-1.5 rounded-md border border-ink-800/10 disabled:opacity-40 hover:bg-slate-50"
              >
                Previous
              </button>
              <button
                disabled={data.items.length < 20}
                onClick={() => setPage((p) => p + 1)}
                className="text-sm px-3 py-1.5 rounded-md border border-ink-800/10 disabled:opacity-40 hover:bg-slate-50"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
