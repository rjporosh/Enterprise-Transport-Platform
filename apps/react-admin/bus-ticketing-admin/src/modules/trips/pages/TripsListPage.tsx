import { useState } from 'react';
import { useTrips } from '../hooks/useTrips';
import { Trip, TripStatus } from '../models/trip.model';
import { PageHeader, DataTable, Pagination, Badge, Select, BadgeTone } from '@shared-ui/react';
import type { DataTableColumn } from '@shared-ui/react';

const STATUS_TONE: Record<TripStatus, BadgeTone> = {
  Scheduled: 'info',
  InTransit: 'brand',
  Completed: 'success',
  Cancelled: 'danger'
};

const PAGE_SIZE = 10;

export default function TripsListPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string>('');
  const { data, isLoading } = useTrips({ page, pageSize: PAGE_SIZE, status: status || undefined });

  const columns: DataTableColumn<Trip>[] = [
    { key: 'route', header: 'Route', render: (t) => <span className="font-medium">{t.routeName}</span> },
    { key: 'bus', header: 'Bus', render: (t) => <span className="font-mono text-xs">{t.busPlateNumber}</span> },
    { key: 'operator', header: 'Operator', render: (t) => t.operator },
    { key: 'departure', header: 'Departs', render: (t) => new Date(t.departureUtc).toLocaleString() },
    {
      key: 'seats',
      header: 'Seats',
      align: 'right',
      render: (t) => (
        <span>
          {t.availableSeats}/{t.totalSeats}
        </span>
      )
    },
    { key: 'price', header: 'Price', align: 'right', render: (t) => `${t.pricePerSeat} BDT` },
    { key: 'status', header: 'Status', render: (t) => <Badge tone={STATUS_TONE[t.status]}>{t.status}</Badge> }
  ];

  return (
    <div className="p-8">
      <PageHeader
        eyebrow="Fleet operations"
        title="Trips"
        description="Every scheduled, active and completed trip across the network."
        actions={
          <Select
            value={status}
            onChange={(e) => {
              setPage(1);
              setStatus(e.target.value);
            }}
            className="!w-44"
          >
            <option value="">All statuses</option>
            <option value="Scheduled">Scheduled</option>
            <option value="InTransit">In transit</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </Select>
        }
      />

      <DataTable
        columns={columns}
        rows={data?.items ?? []}
        rowKey={(t) => t.tripId}
        loading={isLoading}
        emptyTitle="No trips match this filter"
      />

      {data && <Pagination page={page} pageSize={PAGE_SIZE} totalCount={data.totalCount} onPageChange={setPage} />}
    </div>
  );
}
