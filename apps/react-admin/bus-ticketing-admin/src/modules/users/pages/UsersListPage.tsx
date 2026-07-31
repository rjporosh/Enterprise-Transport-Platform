import { useState } from 'react';
import { useUsers } from '../hooks/useUsers';
import { AdminUser, AdminUserStatus } from '../models/user.model';
import { PageHeader, DataTable, Pagination, Badge, BadgeTone } from '@shared-ui/react';
import type { DataTableColumn } from '@shared-ui/react';

const STATUS_TONE: Record<AdminUserStatus, BadgeTone> = { Active: 'success', Suspended: 'danger' };
const PAGE_SIZE = 10;

export default function UsersListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useUsers({ page, pageSize: PAGE_SIZE });

  const columns: DataTableColumn<AdminUser>[] = [
    { key: 'name', header: 'Name', render: (u) => <span className="font-medium">{u.fullName}</span> },
    { key: 'email', header: 'Email', render: (u) => u.email },
    { key: 'role', header: 'Role', render: (u) => <Badge tone="neutral">{u.role}</Badge> },
    {
      key: 'lastLogin',
      header: 'Last login',
      render: (u) => (u.lastLoginUtc ? new Date(u.lastLoginUtc).toLocaleString() : '—')
    },
    { key: 'status', header: 'Status', render: (u) => <Badge tone={STATUS_TONE[u.status]}>{u.status}</Badge> }
  ];

  return (
    <div className="p-8">
      <PageHeader eyebrow="Access control" title="Admin users" description="Everyone with access to this console, and what they can do." />
      <DataTable columns={columns} rows={data?.items ?? []} rowKey={(u) => u.userId} loading={isLoading} emptyTitle="No users found" />
      {data && <Pagination page={page} pageSize={PAGE_SIZE} totalCount={data.totalCount} onPageChange={setPage} />}
    </div>
  );
}
