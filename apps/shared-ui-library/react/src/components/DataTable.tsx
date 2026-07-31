import { ReactNode } from 'react';
import { Spinner } from './Spinner';
import { EmptyState } from './EmptyState';

// Tailwind's JIT scanner needs full, static class names — it can't resolve
// `text-${align}` at build time — so the align -> class mapping is spelled
// out explicitly here instead of interpolated inline below.
const ALIGN_CLASSES: Record<'left' | 'right' | 'center', string> = {
  left: 'text-left',
  right: 'text-right',
  center: 'text-center'
};

export interface DataTableColumn<T> {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  align?: 'left' | 'right' | 'center';
  width?: string;
}

export interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  loading?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  onRowClick?: (row: T) => void;
}

/**
 * Generic, headless-ish data table used by every admin list page (Bookings,
 * Trips, Buses, Routes, Users...). Column definitions stay in each module —
 * this component only owns layout, loading and empty states so every table
 * in the console looks and behaves identically.
 */
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  emptyTitle = 'No records found',
  emptyDescription,
  onRowClick
}: DataTableProps<T>) {
  return (
    <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-slate-50 border-b border-slate-200">
              {columns.map((col) => (
                <th
                  key={col.key}
                  style={{ width: col.width }}
                  className={`px-4 py-3 font-semibold text-ink-950/70 text-xs uppercase tracking-wide ${ALIGN_CLASSES[col.align ?? 'left']}`}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {!loading &&
              rows.map((row) => (
                <tr
                  key={rowKey(row)}
                  onClick={() => onRowClick?.(row)}
                  className={`border-b border-slate-100 last:border-0 ${
                    onRowClick ? 'cursor-pointer hover:bg-slate-50' : ''
                  }`}
                >
                  {columns.map((col) => (
                    <td key={col.key} className={`px-4 py-3 text-ink-950/90 ${ALIGN_CLASSES[col.align ?? 'left']}`}>
                      {col.render(row)}
                    </td>
                  ))}
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {loading && (
        <div className="flex justify-center py-14 text-ink-700">
          <Spinner size="lg" />
        </div>
      )}

      {!loading && rows.length === 0 && (
        <div className="text-ink-950">
          <EmptyState title={emptyTitle} description={emptyDescription} />
        </div>
      )}
    </div>
  );
}
