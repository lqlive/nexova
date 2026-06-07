import React, { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  PlusIcon,
  PencilSquareIcon,
  TrashIcon,
  TableCellsIcon,
} from '@heroicons/react/24/outline';
import PageHeader from '../components/PageHeader';
import FilterBar from '../components/FilterBar';
import DataTable, { Column } from '../components/DataTable';
import Tag from '../components/Tag';
import OwnerAvatars from '../components/OwnerAvatars';
import { datasets } from '../api/mockData';
import type { DatasetItem } from '../api/types';

const Datasets: React.FC = () => {
  const [search, setSearch] = useState('');
  const [type, setType] = useState('All');
  const [database, setDatabase] = useState('All');

  const dbOptions = useMemo(
    () => ['All', ...Array.from(new Set(datasets.map((d) => d.database)))],
    []
  );

  const filtered = useMemo(() => {
    return datasets.filter((d) => {
      if (type !== 'All' && d.type !== type.toLowerCase()) return false;
      if (database !== 'All' && d.database !== database) return false;
      if (search && !d.name.toLowerCase().includes(search.toLowerCase())) return false;
      return true;
    });
  }, [search, type, database]);

  const columns: Column<DatasetItem>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (d) => (
        <div className="flex items-center gap-2">
          <TableCellsIcon className="h-4 w-4 text-gray-400" />
          <Link
            to={`/sql-editor/${d.id}`}
            className="font-medium text-gray-900 hover:underline font-mono text-sm"
          >
            {d.name}
          </Link>
        </div>
      ),
    },
    {
      key: 'type',
      header: 'Type',
      render: (d) => (
        <Tag variant={d.type === 'physical' ? 'info' : 'warning'}>
          {d.type === 'physical' ? 'Physical' : 'Virtual'}
        </Tag>
      ),
    },
    { key: 'database', header: 'Database', render: (d) => <span>{d.database}</span> },
    { key: 'schema', header: 'Schema', render: (d) => <span className="text-accent-500">{d.schema}</span> },
    { key: 'charts', header: 'Charts', render: (d) => <span>{d.charts}</span> },
    { key: 'owners', header: 'Owners', render: (d) => <OwnerAvatars owners={d.owners} /> },
    { key: 'modified', header: 'Last modified', render: (d) => <span className="text-accent-400">{d.modified}</span> },
    {
      key: 'actions',
      header: '',
      className: 'w-px',
      render: () => (
        <div className="flex items-center gap-3 text-gray-400">
          <button className="hover:text-gray-900" title="Edit"><PencilSquareIcon className="h-4 w-4" /></button>
          <button className="hover:text-error-400" title="Delete"><TrashIcon className="h-4 w-4" /></button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Datasets"
        actions={
          <button className="btn-primary">
            <PlusIcon className="h-4 w-4" /> Dataset
          </button>
        }
      />

      <FilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder="Search datasets"
        filters={[
          { label: 'Type', options: ['All', 'Physical', 'Virtual'], value: type, onChange: setType },
          { label: 'Database', options: dbOptions, value: database, onChange: setDatabase },
        ]}
      />

      <DataTable columns={columns} rows={filtered} rowKey={(d) => d.id} emptyText="No datasets found" />
    </div>
  );
};

export default Datasets;
