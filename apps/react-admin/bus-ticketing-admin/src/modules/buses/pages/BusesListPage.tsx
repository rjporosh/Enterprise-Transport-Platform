import { useState } from 'react';
import { useBuses } from '../hooks/useBuses';
import { Bus, BusStatus } from '../models/bus.model';
import { PageHeader, DataTable, Pagination, Badge, BadgeTone } from '@shared-ui/react';
import type { DataTableColumn } from '@shared-ui/react';

const STATUS_TONE: Record<BusStatus, BadgeTone> = { Active: 'success', Maintenance: 'warning', Suspended: 'danger' };
const PAGE_SIZE = 10;

export default function BusesListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useBuses({ page, pageSize: PAGE_SIZE });

  const columns: DataTableColumn<Bus>[] = [
    { key: 'plate', header: 'Plate number', render: (b) => <span className="font-mono">{b.plateNumber}</span> },
    { key: 'operator', header: 'Operator', render: (b) => b.operator },
    { key: 'type', header: 'Type', render: (b) => b.busType },
    { key: 'capacity', header: 'Capacity', align: 'right', render: (b) => b.capacity },
    { key: 'status', header: 'Status', render: (b) => <Badge tone={STATUS_TONE[b.status]}>{b.status}</Badge> }
  ];

  return (
    <div className="p-8">
      <PageHeader eyebrow="Fleet" title="Buses" description="Vehicles registered across every operator on the network." />
      <DataTable columns={columns} rows={data?.items ?? []} rowKey={(b) => b.busId} loading={isLoading} emptyTitle="No buses found" />
      {data && <Pagination page={page} pageSize={PAGE_SIZE} totalCount={data.totalCount} onPageChange={setPage} />}
    </div>
  );
}
