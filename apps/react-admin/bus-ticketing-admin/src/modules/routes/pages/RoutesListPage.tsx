import { useState } from 'react';
import { useRoutes } from '../hooks/useRoutes';
import { RouteDef } from '../models/route.model';
import { PageHeader, DataTable, Pagination, Badge } from '@shared-ui/react';
import type { DataTableColumn } from '@shared-ui/react';

const PAGE_SIZE = 10;

export default function RoutesListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useRoutes({ page, pageSize: PAGE_SIZE });

  const columns: DataTableColumn<RouteDef>[] = [
    { key: 'name', header: 'Route', render: (r) => <span className="font-medium">{r.name}</span> },
    { key: 'distance', header: 'Distance', align: 'right', render: (r) => `${r.distanceKm} km` },
    {
      key: 'duration',
      header: 'Est. duration',
      align: 'right',
      render: (r) => `${Math.floor(r.estimatedDurationMinutes / 60)}h ${r.estimatedDurationMinutes % 60}m`
    },
    { key: 'active', header: 'Active trips', align: 'right', render: (r) => <Badge tone="info">{r.activeTrips}</Badge> }
  ];

  return (
    <div className="p-8">
      <PageHeader eyebrow="Network" title="Routes" description="Origin–destination pairs served across the network." />
      <DataTable columns={columns} rows={data?.items ?? []} rowKey={(r) => r.routeId} loading={isLoading} emptyTitle="No routes found" />
      {data && <Pagination page={page} pageSize={PAGE_SIZE} totalCount={data.totalCount} onPageChange={setPage} />}
    </div>
  );
}
