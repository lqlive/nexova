import React, { useMemo, useState } from 'react';
import {
  PlusIcon,
  Squares2X2Icon,
  ListBulletIcon,
  PencilSquareIcon,
  TrashIcon,
  ShareIcon,
} from '@heroicons/react/24/outline';
import PageHeader from '../components/PageHeader';
import FilterBar from '../components/FilterBar';
import DataTable, { Column } from '../components/DataTable';
import Tag from '../components/Tag';
import OwnerAvatars from '../components/OwnerAvatars';
import FavoriteStar from '../components/FavoriteStar';
import { dashboards as initialDashboards } from '../api/mockData';
import type { DashboardItem } from '../api/types';
import classNames from 'classnames';

const Dashboards: React.FC = () => {
  const [items, setItems] = useState<DashboardItem[]>(initialDashboards);
  const [tab, setTab] = useState('all');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('All');
  const [view, setView] = useState<'card' | 'table'>('table');

  const toggleFavorite = (id: number) =>
    setItems((prev) => prev.map((d) => (d.id === id ? { ...d, favorite: !d.favorite } : d)));

  const filtered = useMemo(() => {
    return items.filter((d) => {
      if (tab === 'favorite' && !d.favorite) return false;
      if (status !== 'All' && d.status !== status.toLowerCase()) return false;
      if (search && !d.title.toLowerCase().includes(search.toLowerCase())) return false;
      return true;
    });
  }, [items, tab, status, search]);

  const columns: Column<DashboardItem>[] = [
    {
      key: 'title',
      header: 'Name',
      render: (d) => (
        <div className="flex items-center gap-2">
          <FavoriteStar active={d.favorite} onToggle={() => toggleFavorite(d.id)} />
          <span className="font-medium text-gray-900 hover:underline cursor-pointer">{d.title}</span>
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (d) => (
        <Tag variant={d.status === 'published' ? 'success' : 'neutral'} dot>
          {d.status === 'published' ? 'Published' : 'Draft'}
        </Tag>
      ),
    },
    { key: 'charts', header: 'Charts', render: (d) => <span>{d.charts}</span> },
    { key: 'owners', header: 'Owners', render: (d) => <OwnerAvatars owners={d.owners} /> },
    { key: 'modifiedBy', header: 'Modified by', render: (d) => <span>{d.modifiedBy}</span> },
    { key: 'modified', header: 'Last modified', render: (d) => <span className="text-accent-400">{d.modified}</span> },
    {
      key: 'actions',
      header: '',
      className: 'w-px',
      render: () => (
        <div className="flex items-center gap-3 text-gray-400">
          <button className="hover:text-gray-900" title="Edit"><PencilSquareIcon className="h-4 w-4" /></button>
          <button className="hover:text-gray-900" title="Share"><ShareIcon className="h-4 w-4" /></button>
          <button className="hover:text-error-400" title="Delete"><TrashIcon className="h-4 w-4" /></button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Dashboards"
        actions={
          <button className="btn-primary">
            <PlusIcon className="h-4 w-4" /> Dashboard
          </button>
        }
        tabs={[
          { key: 'all', label: 'All', count: items.length },
          { key: 'favorite', label: 'Favorite', count: items.filter((d) => d.favorite).length },
          { key: 'mine', label: 'Mine' },
        ]}
        activeTab={tab}
        onTabChange={setTab}
      />

      <div className="flex items-center gap-3">
        <div className="flex-1">
          <FilterBar
            search={search}
            onSearchChange={setSearch}
            searchPlaceholder="Search dashboards"
            filters={[
              { label: 'Status', options: ['All', 'Published', 'Draft'], value: status, onChange: setStatus },
            ]}
          />
        </div>
        <div className="flex items-center border border-gray-300 rounded overflow-hidden mb-4">
          <button
            onClick={() => setView('card')}
            className={classNames('p-1.5', view === 'card' ? 'bg-gray-100 text-gray-900' : 'text-gray-400')}
            title="Card view"
          >
            <Squares2X2Icon className="h-4 w-4" />
          </button>
          <button
            onClick={() => setView('table')}
            className={classNames('p-1.5', view === 'table' ? 'bg-gray-100 text-gray-900' : 'text-gray-400')}
            title="Table view"
          >
            <ListBulletIcon className="h-4 w-4" />
          </button>
        </div>
      </div>

      {view === 'table' ? (
        <DataTable columns={columns} rows={filtered} rowKey={(d) => d.id} emptyText="No dashboards found" />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {filtered.map((d) => (
            <div key={d.id} className="card overflow-hidden group">
              <div className="h-32 bg-gray-50 border-b border-gray-100 flex items-center justify-center relative">
                <Squares2X2Icon className="h-12 w-12 text-gray-300 group-hover:scale-110 transition-transform" />
                <div className="absolute top-2 right-2">
                  <FavoriteStar active={d.favorite} onToggle={() => toggleFavorite(d.id)} />
                </div>
              </div>
              <div className="p-3">
                <div className="text-sm font-semibold text-accent-700 truncate">{d.title}</div>
                <div className="text-xs text-accent-400 mt-0.5">{d.charts} charts · {d.modified}</div>
                <div className="flex items-center justify-between mt-3">
                  <Tag variant={d.status === 'published' ? 'success' : 'neutral'} dot>
                    {d.status === 'published' ? 'Published' : 'Draft'}
                  </Tag>
                  <OwnerAvatars owners={d.owners} />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default Dashboards;
