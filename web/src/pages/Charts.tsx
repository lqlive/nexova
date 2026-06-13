import React, { useMemo, useState } from 'react';
import {
  PlusIcon,
  PencilSquareIcon,
  TrashIcon,
  ChartBarIcon,
  ChartPieIcon,
  PresentationChartLineIcon,
  TableCellsIcon,
  HashtagIcon,
} from '@heroicons/react/24/outline';
import PageHeader from '../components/PageHeader';
import FilterBar from '../components/FilterBar';
import DataTable, { Column } from '../components/DataTable';
import Tag from '../components/Tag';
import OwnerAvatars from '../components/OwnerAvatars';
import FavoriteStar from '../components/FavoriteStar';
import { charts as initialCharts } from '../api/mockData';
import type { ChartItem, ChartVizType } from '../api/types';

const vizIcon = (viz: ChartVizType) => {
  switch (viz) {
    case 'Pie Chart':
      return ChartPieIcon;
    case 'Line Chart':
    case 'Area Chart':
    case 'Time-series':
      return PresentationChartLineIcon;
    case 'Table':
      return TableCellsIcon;
    case 'Big Number':
      return HashtagIcon;
    default:
      return ChartBarIcon;
  }
};

const Charts: React.FC = () => {
  const [items, setItems] = useState<ChartItem[]>(initialCharts);
  const [tab, setTab] = useState('all');
  const [search, setSearch] = useState('');
  const [viz, setViz] = useState('All');

  const vizOptions = useMemo(
    () => ['All', ...Array.from(new Set(initialCharts.map((c) => c.vizType)))],
    []
  );

  const toggleFavorite = (id: number) =>
    setItems((prev) => prev.map((c) => (c.id === id ? { ...c, favorite: !c.favorite } : c)));

  const filtered = useMemo(() => {
    return items.filter((c) => {
      if (tab === 'favorite' && !c.favorite) return false;
      if (viz !== 'All' && c.vizType !== viz) return false;
      if (search && !c.name.toLowerCase().includes(search.toLowerCase())) return false;
      return true;
    });
  }, [items, tab, viz, search]);

  const columns: Column<ChartItem>[] = [
    {
      key: 'name',
      header: 'Chart',
      render: (c) => {
        const Icon = vizIcon(c.vizType);
        return (
          <div className="flex items-center gap-2">
            <FavoriteStar active={c.favorite} onToggle={() => toggleFavorite(c.id)} />
            <Icon className="h-4 w-4 text-accent-400" />
            <span className="font-medium text-gray-900 hover:underline cursor-pointer">{c.name}</span>
          </div>
        );
      },
    },
    { key: 'vizType', header: 'Visualization', render: (c) => <Tag variant="primary">{c.vizType}</Tag> },
    { key: 'dataset', header: 'Dataset', render: (c) => <span className="font-mono text-xs">{c.dataset}</span> },
    { key: 'owners', header: 'Owners', render: (c) => <OwnerAvatars owners={c.owners} /> },
    { key: 'modifiedBy', header: 'Modified by', render: (c) => <span>{c.modifiedBy}</span> },
    { key: 'modified', header: 'Last modified', render: (c) => <span className="text-accent-400">{c.modified}</span> },
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
        title="Charts"
        actions={
          <button className="btn-primary">
            <PlusIcon className="h-4 w-4" /> Chart
          </button>
        }
        tabs={[
          { key: 'all', label: 'All', count: items.length },
          { key: 'favorite', label: 'Favorite', count: items.filter((c) => c.favorite).length },
          { key: 'mine', label: 'Mine' },
        ]}
        activeTab={tab}
        onTabChange={setTab}
      />

      <FilterBar
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder="Search charts"
        filters={[{ label: 'Type', options: vizOptions, value: viz, onChange: setViz }]}
      />

      <DataTable columns={columns} rows={filtered} rowKey={(c) => c.id} emptyText="No charts found" />
    </div>
  );
};

export default Charts;
