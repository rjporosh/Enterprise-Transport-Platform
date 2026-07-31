import { Link } from 'react-router-dom';
import { useDashboardStats } from '../hooks/useDashboardStats';
import { PageHeader, StatCard, Card, Badge, Spinner, statusToBadgeTone } from '@shared-ui/react';

export default function DashboardPage() {
  const { data, isLoading } = useDashboardStats();

  return (
    <div className="p-8">
      <PageHeader eyebrow="Overview" title="Dashboard" description="Live snapshot of bookings, revenue and fleet activity." />

      {isLoading && (
        <div className="flex justify-center py-16 text-ink-700">
          <Spinner size="lg" />
        </div>
      )}

      {data && (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            <StatCard tone="light" label="Total bookings" value={data.totalBookings} />
            <StatCard tone="light" label="Confirmed revenue" value={`${data.totalRevenue.toLocaleString()} BDT`} />
            <StatCard tone="light" label="Active trips" value={data.activeTrips} />
            <StatCard tone="light" label="Pending payments" value={data.pendingPayments} />
          </div>

          <Card tone="light" padded={false}>
            <div className="flex items-center justify-between px-5 py-4 border-b border-slate-200">
              <h2 className="font-display text-lg text-ink-950">Recent bookings</h2>
              <Link to="/bookings" className="text-saffron-600 text-sm font-medium hover:underline">
                View all
              </Link>
            </div>
            <ul className="divide-y divide-slate-100">
              {data.recentBookings.map((booking) => (
                <li key={booking.bookingId} className="flex items-center justify-between px-5 py-3">
                  <div>
                    <Link to={`/bookings/${booking.bookingId}`} className="font-mono text-sm text-saffron-600 hover:underline">
                      {booking.bookingId}
                    </Link>
                    <p className="text-xs text-ink-700/50 mt-0.5">{new Date(booking.createdAtUtc).toLocaleString()}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-ink-950">
                      {booking.totalAmount.toLocaleString()} {booking.currency}
                    </span>
                    <Badge tone={statusToBadgeTone(booking.status)}>{booking.status}</Badge>
                  </div>
                </li>
              ))}
            </ul>
          </Card>
        </>
      )}
    </div>
  );
}
