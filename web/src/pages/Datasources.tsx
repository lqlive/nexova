import React, { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  PlusIcon,
  PencilSquareIcon,
  TrashIcon,
  CircleStackIcon,
} from '@heroicons/react/24/outline';
import PageHeader from '../components/PageHeader';
import FilterBar from '../components/FilterBar';
import DataTable, { Column } from '../components/DataTable';
import Tag from '../components/Tag';
import { datasources } from '../api/mockData';
import type { DataSourceItem } from '../api/types';

const statusVariant = (s: DataSourceItem['status']) =>
  s === 'connected' ? 'success' : s === 'syncing' ? 'info' : 'error';

const statusLabel = (s: DataSourceItem['status']) =>
  s === 'connected' ? 'Connected' : s === 'syncing' ? 'Syncing' : 'Error';

const Datasources: React.FC = () => {
  const [search, setSearch] = useState('');
  const [type, setType] = useState('All');
  const [status, setStatus] = useState('All');

  const typeOptions = useMemo(
    () => ['All', ...Array.from(new Set(datasources.map((d) => d.type)))],
    []
  );

  const filtered = useMemo(() => {
    return datasources.filter((d) => {
      if (type !== 'All' && d.type !== type) return false;
      if (status !== 'All' && statusLabel(d.status) !== status) return false;
      if (search && !d.name.toLowerCase().includes(search.toLowerCase())) return false;
      return true;
    });
  }, [search, type, status]);

  const columns: Column<DataSourceItem>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (d) => (
        <div className="flex items-center gap-2">
          <CircleStackIcon className="h-4 w-4 text-gray-400" />
          <Link to={`/datasources/${d.id}`} className="font-medium text-gray-900 hover:underline">
            {d.name}
          </Link>
        </div>
      ),
    },
    { key: 'type', header: 'Type', render: (d) => <Tag variant="neutral">{d.type}</Tag> },
    { key: 'host', header: 'Host', render: (d) => <span className="font-mono text-xs text-gray-500">{d.host}</span> },
    {
      key: 'status',
      header: 'Status',
      render: (d) => (
        <Tag variant={statusVariant(d.status)} dot>
          {statusLabel(d.status)}
        </Tag>
      ),
    },
    { key: 'datasets', header: 'Datasets', render: (d) => <span>{d.datasets}</span> },
    { key: 'lastSync', header: 'Last sync', render: (d) => <span className="text-gray-400">{d.lastSync}</span> },
    {
      key: 'actions',
      header: '',
      className: 'w-px',
      render: () => (
        <div className="flex items-center gap-3 text-gray-400">
          <button className="hover:text-gray-900" title="Edit"><PencilSquareIcon className="h-4 w-4" /></button>
          <button className="hover:text-red-500" title="Delete"><TrashIcon className="h-4 w-4" /></button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Datasources"
        subtitle="Connected databases and data sources"
        actions={
          <button className="btn-primary">
            <PlusIcon className="h-4 w-4" /> Datasource
          </button>
        }
      />

      <FilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder="Search datasources"
        filters={[
          { label: 'Type', options: typeOptions, value: type, onChange: setType },
          { label: 'Status', options: ['All', 'Connected', 'Syncing', 'Error'], value: status, onChange: setStatus },
        ]}
      />

      <DataTable columns={columns} rows={filtered} rowKey={(d) => d.id} emptyText="No datasources found" />
    </div>
  );
};

export default Datasources;
