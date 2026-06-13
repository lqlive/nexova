import React from 'react';
import { InboxIcon } from '@heroicons/react/24/outline';

export interface Column<T> {
  key: string;
  header: string;
  render: (row: T) => React.ReactNode;
  className?: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string | number;
  emptyText?: string;
  loading?: boolean;
}

function DataTable<T>({
  columns,
  rows,
  rowKey,
  emptyText = 'No data',
  loading = false,
}: DataTableProps<T>) {
  return (
    <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
      <div className="overflow-x-auto">
        <table className="data-table">
          <thead>
            <tr>
              {columns.map((col) => (
                <th key={col.key} className={col.className}>
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr className="data-table-empty-row">
                <td colSpan={columns.length}>
                  <div className="flex flex-col items-center justify-center gap-3 px-4 py-16 text-center">
                    {loading ? (
                      <span className="h-7 w-7 animate-spin rounded-full border-2 border-gray-200 border-t-primary-500" />
                    ) : (
                      <span className="flex h-12 w-12 items-center justify-center rounded-full bg-gray-100 text-gray-400">
                        <InboxIcon className="h-6 w-6" />
                      </span>
                    )}
                    <p className="text-sm font-medium text-gray-500">
                      {loading ? 'Loading…' : emptyText}
                    </p>
                  </div>
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={rowKey(row)}>
                  {columns.map((col) => (
                    <td key={col.key} className={col.className}>
                      {col.render(row)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default DataTable;
