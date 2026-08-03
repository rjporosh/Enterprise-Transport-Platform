import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useBookings } from '../hooks/useBookings';
import { Booking } from '../models/booking.model';
import { PageHeader, DataTable, Pagination, Badge, statusToBadgeTone } from '@shared-ui/react';
import type { DataTableColumn } from '@shared-ui/react';

const PAGE_SIZE = 20;

export default function BookingsListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, error } = useBookings({ page, pageSize: PAGE_SIZE });

  const columns: DataTableColumn<Booking>[] = [
    {
      key: 'booking',
      header: 'Booking',
      render: (b) => (
        <Link to={`/bookings/${b.bookingId}`} className="text-saffron-600 font-mono hover:underline">
          {b.bookingId.slice(0, 8)}
        </Link>
      )
    },
    { key: 'trip', header: 'Trip', render: (b) => <span className="font-mono text-ink-700">{b.tripId.slice(0, 8)}</span> },
    { key: 'seats', header: 'Seats', align: 'right', render: (b) => b.seats.length },
    {
      key: 'amount',
      header: 'Amount',
      align: 'right',
      render: (b) => `${b.totalAmount.toFixed(0)} ${b.currency}`
    },
    { key: 'status', header: 'Status', render: (b) => <Badge tone={statusToBadgeTone(b.status)}>{b.status}</Badge> },
    { key: 'created', header: 'Created', render: (b) => new Date(b.createdAtUtc).toLocaleString() }
  ];

  return (
    <div className="p-8">
      <PageHeader eyebrow="Operations" title="Bookings" description="Every booking made across the network, confirmed or pending." />

      {isError && (
        <p className="text-danger bg-danger-bg rounded-md px-4 py-3 mb-4">
          {(error as Error)?.message ?? 'Unable to load bookings.'}
        </p>
      )}

      <DataTable
        columns={columns}
        rows={data?.items ?? []}
        rowKey={(b) => b.bookingId}
        loading={isLoading}
        emptyTitle="No bookings found"
      />

      {data && <Pagination page={page} pageSize={PAGE_SIZE} totalCount={data.totalCount} onPageChange={setPage} />}
    </div>
  );
}
