import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  MagnifyingGlassIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  FolderIcon,
  TableCellsIcon,
  CircleStackIcon,
  CheckCircleIcon,
  ArrowDownTrayIcon,
  ArrowPathIcon,
  XMarkIcon,
} from '@heroicons/react/24/outline';
import classNames from 'classnames';
import { datasources, datasourcePreview, findDatasource } from '../api/mockData';
import type { GridColumnType } from '../api/types';

const TypeBadge: React.FC<{ type: GridColumnType }> = ({ type }) => {
  if (type === 'date') {
    return (
      <span className="text-gray-400" title="date">
        <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2}>
          <rect x="3" y="4" width="18" height="17" rx="2" />
          <path d="M3 9h18M8 2v4M16 2v4" />
        </svg>
      </span>
    );
  }
  const label = type === 'string' ? 'ABC' : type === 'decimal' ? '1.2' : '123';
  return (
    <span className="text-[10px] font-mono font-semibold text-gray-400" title={type}>
      {label}
    </span>
  );
};

const DatasourceDetail: React.FC = () => {
  const { id } = useParams();
  const datasource = findDatasource(Number(id)) ?? datasources[0];
  const [search, setSearch] = useState('');
  const [tablesOpen, setTablesOpen] = useState(true);
  const [filesOpen, setFilesOpen] = useState(false);
  const [activeTable, setActiveTable] = useState(datasource.tables[0]);

  useEffect(() => {
    setActiveTable(datasource.tables[0]);
  }, [datasource]);

  const visibleTables = datasource.tables.filter((t) =>
    t.toLowerCase().includes(search.toLowerCase())
  );

  const { columns, rows } = datasourcePreview;

  return (
    <div className="flex flex-col h-[calc(100vh-7rem)]">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm mb-3">
        <Link to="/datasources" className="text-gray-500 hover:text-gray-900">
          Datasources
        </Link>
        <ChevronRightIcon className="h-3.5 w-3.5 text-gray-400" />
        <span className="text-gray-900 font-medium">{datasource.name}</span>
        <span className="ml-1 tag-neutral">{datasource.type}</span>
      </div>

      {/* Explorer + data */}
      <div className="flex flex-1 min-h-0 border border-gray-200 rounded-lg overflow-hidden bg-white">
        {/* Explorer */}
        <aside className="w-60 shrink-0 border-r border-gray-100 flex flex-col">
          <div className="h-9 flex items-center justify-between px-3 border-b border-gray-100">
            <span className="text-xs font-semibold text-gray-700">Explorer</span>
            <MagnifyingGlassIcon className="h-4 w-4 text-gray-400" />
          </div>
          <div className="p-2 border-b border-gray-100">
            <div className="relative">
              <MagnifyingGlassIcon className="h-3.5 w-3.5 text-gray-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
              <input
                className="input h-7 text-xs pl-8"
                placeholder="Search tables"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
          </div>
          <div className="flex-1 overflow-y-auto py-1 text-[13px]">
            {/* Root */}
            <div className="flex items-center gap-1.5 px-3 py-2 text-gray-800 font-medium">
              <CircleStackIcon className="h-4 w-4 text-gray-500" />
              <span className="truncate">{datasource.name}</span>
            </div>

            {/* Tables */}
            <button
              onClick={() => setTablesOpen((o) => !o)}
              className="w-full flex items-center gap-1 pl-5 pr-3 py-2 text-gray-700 hover:bg-gray-50"
            >
              {tablesOpen ? (
                <ChevronDownIcon className="h-3.5 w-3.5 text-gray-400" />
              ) : (
                <ChevronRightIcon className="h-3.5 w-3.5 text-gray-400" />
              )}
              <FolderIcon className="h-4 w-4 text-gray-400" />
              <span>Tables</span>
            </button>
            {tablesOpen &&
              visibleTables.map((t) => (
                <button
                  key={t}
                  onClick={() => setActiveTable(t)}
                  className={classNames(
                    'w-full flex items-center gap-1.5 pl-11 pr-3 py-2 hover:bg-gray-50 text-left',
                    t === activeTable ? 'bg-gray-100 text-gray-900' : 'text-gray-600'
                  )}
                >
                  <TableCellsIcon className="h-4 w-4 text-gray-400 shrink-0" />
                  <span className="font-mono text-xs truncate">{t}</span>
                </button>
              ))}

            {/* Files */}
            <button
              onClick={() => setFilesOpen((o) => !o)}
              className="w-full flex items-center gap-1 pl-5 pr-3 py-2 text-gray-700 hover:bg-gray-50"
            >
              {filesOpen ? (
                <ChevronDownIcon className="h-3.5 w-3.5 text-gray-400" />
              ) : (
                <ChevronRightIcon className="h-3.5 w-3.5 text-gray-400" />
              )}
              <FolderIcon className="h-4 w-4 text-gray-400" />
              <span>Files</span>
            </button>
            {filesOpen && (
              <div className="pl-11 pr-3 py-2 text-xs text-gray-400">No files</div>
            )}
          </div>
        </aside>

        {/* Data area */}
        <section className="flex-1 flex flex-col min-w-0">
          {/* Tabs */}
          <div className="h-10 flex items-end border-b border-gray-100 px-2 gap-1 bg-gray-50/60">
            <div className="flex items-center gap-2 px-3 h-9 bg-white border border-b-0 border-gray-200 rounded-t-md text-sm text-gray-900">
              <TableCellsIcon className="h-4 w-4 text-gray-500" />
              <span className="font-mono text-xs">{activeTable}</span>
              <XMarkIcon className="h-3.5 w-3.5 text-gray-400 hover:text-gray-700" />
            </div>
            <div className="flex items-center gap-2 px-3 h-9 text-sm text-gray-500 hover:text-gray-800 cursor-pointer">
              <FolderIcon className="h-4 w-4" />
              <span className="text-xs">Files</span>
            </div>
          </div>

          {/* Toolbar */}
          <div className="h-10 flex items-center justify-between px-3 border-b border-gray-100">
            <div className="flex items-center gap-2">
              <button className="btn-ghost h-7 px-2 text-xs">
                Table view
                <ChevronDownIcon className="h-3 w-3" />
              </button>
              <button className="text-gray-400 hover:text-gray-700" title="Refresh">
                <ArrowPathIcon className="h-4 w-4" />
              </button>
              <button className="text-gray-400 hover:text-gray-700" title="Export">
                <ArrowDownTrayIcon className="h-4 w-4" />
              </button>
            </div>
            <span className="text-xs text-gray-400">Showing 1,000 rows</span>
          </div>

          {/* Grid */}
          <div className="flex-1 overflow-auto">
            <table className="border-collapse text-xs w-full">
              <thead className="sticky top-0 z-10">
                <tr className="bg-gray-50">
                  <th className="sticky left-0 z-20 bg-gray-50 w-12 px-2 py-1.5 border-b border-r border-gray-200 text-gray-400 font-normal text-right">
                    #
                  </th>
                  {columns.map((col) => (
                    <th
                      key={col.name}
                      className="px-3 py-1.5 border-b border-r border-gray-200 text-left font-medium text-gray-600 whitespace-nowrap"
                    >
                      <span className="flex items-center gap-1.5">
                        <TypeBadge type={col.type} />
                        {col.name}
                      </span>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((row, ri) => (
                  <tr key={ri} className="hover:bg-gray-50">
                    <td className="sticky left-0 z-10 bg-white w-12 px-2 py-1.5 border-b border-r border-gray-100 text-gray-400 text-right">
                      {ri + 1}
                    </td>
                    {row.map((cell, ci) => (
                      <td
                        key={ci}
                        className={classNames(
                          'px-3 py-1.5 border-b border-r border-gray-100 whitespace-nowrap',
                          columns[ci].type === 'int' || columns[ci].type === 'decimal'
                            ? 'text-right font-mono text-gray-700'
                            : 'text-gray-700'
                        )}
                      >
                        {cell}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Status bar */}
          <div className="h-8 flex items-center justify-between px-3 border-t border-gray-100 bg-gray-50/60 text-xs text-gray-500">
            <span className="flex items-center gap-1.5">
              <CheckCircleIcon className="h-4 w-4 text-green-500" />
              Ran in 0.48s
            </span>
            <span>
              {columns.length} columns · 1,000 rows
            </span>
          </div>
        </section>
      </div>
    </div>
  );
};

export default DatasourceDetail;
