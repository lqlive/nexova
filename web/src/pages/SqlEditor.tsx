import React, { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  MagnifyingGlassIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  FolderIcon,
  TableCellsIcon,
  CircleStackIcon,
  CheckCircleIcon,
  PlayIcon,
  BookmarkIcon,
  DocumentTextIcon,
  ArrowPathIcon,
  ViewColumnsIcon,
  CubeIcon,
  VariableIcon,
  ShieldCheckIcon,
  XMarkIcon,
  ClipboardDocumentIcon,
  PlusIcon,
  Square3Stack3DIcon,
} from '@heroicons/react/24/outline';
import classNames from 'classnames';
import {
  datasets,
  datasourcePreview,
  datasourceTables,
  findDataset,
} from '../api/mockData';
import type { GridColumnType } from '../api/types';

// ---------------------------------------------------------------------------
// Explorer tree
// ---------------------------------------------------------------------------
type NodeIcon =
  | 'db'
  | 'schema'
  | 'folder'
  | 'table'
  | 'view'
  | 'func'
  | 'proc'
  | 'query'
  | 'security';

interface TreeNode {
  id: string;
  label: string;
  icon: NodeIcon;
  children?: TreeNode[];
  defaultOpen?: boolean;
  active?: boolean;
}

const nodeIcon = (icon: NodeIcon) => {
  const cls = 'h-4 w-4 shrink-0';
  switch (icon) {
    case 'db':
      return <CircleStackIcon className={`${cls} text-gray-500`} />;
    case 'schema':
      return <CubeIcon className={`${cls} text-gray-400`} />;
    case 'folder':
      return <FolderIcon className={`${cls} text-gray-400`} />;
    case 'table':
      return <TableCellsIcon className={`${cls} text-gray-400`} />;
    case 'view':
      return <ViewColumnsIcon className={`${cls} text-gray-400`} />;
    case 'func':
      return <VariableIcon className={`${cls} text-gray-400`} />;
    case 'proc':
      return <Square3Stack3DIcon className={`${cls} text-gray-400`} />;
    case 'query':
      return <DocumentTextIcon className={`${cls} text-gray-400`} />;
    case 'security':
      return <ShieldCheckIcon className={`${cls} text-gray-400`} />;
  }
};

const TreeItem: React.FC<{ node: TreeNode; depth: number }> = ({ node, depth }) => {
  const [open, setOpen] = useState(!!node.defaultOpen);
  const hasChildren = !!node.children?.length;

  return (
    <div>
      <button
        onClick={() => hasChildren && setOpen((o) => !o)}
        className={classNames(
          'w-full flex items-center gap-1.5 py-2 pr-2 text-left hover:bg-gray-50 text-[13px]',
          node.active ? 'bg-gray-100 text-gray-900 font-medium' : 'text-gray-700'
        )}
        style={{ paddingLeft: depth * 14 + 8 }}
      >
        {hasChildren ? (
          open ? (
            <ChevronDownIcon className="h-3 w-3 text-gray-400 shrink-0" />
          ) : (
            <ChevronRightIcon className="h-3 w-3 text-gray-400 shrink-0" />
          )
        ) : (
          <span className="w-3 shrink-0" />
        )}
        {nodeIcon(node.icon)}
        <span className="truncate font-mono">{node.label}</span>
      </button>
      {open &&
        node.children?.map((c) => <TreeItem key={c.id} node={c} depth={depth + 1} />)}
    </div>
  );
};

// ---------------------------------------------------------------------------
// Column type badge
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------
const SqlEditor: React.FC = () => {
  const { id } = useParams();
  const dataset = findDataset(Number(id)) ?? datasets[0];
  const [search, setSearch] = useState('');
  const [resultTab, setResultTab] = useState<'messages' | 'results'>('results');
  const [sql, setSql] = useState(`select * from [${dataset.name}]`);

  const tableNodes: TreeNode[] = useMemo(
    () =>
      datasourceTables(dataset.database, dataset.name).map((t) => ({
        id: `tbl-${t}`,
        label: t,
        icon: 'table' as NodeIcon,
        active: t === dataset.name,
      })),
    [dataset]
  );

  const tree: TreeNode = useMemo(
    () => ({
      id: 'lakehouse',
      label: 'lakehouse',
      icon: 'db',
      defaultOpen: true,
      children: [
        {
          id: 'schemas',
          label: 'Schemas',
          icon: 'folder',
          defaultOpen: true,
          children: [
            { id: 'tables', label: 'Tables', icon: 'folder', defaultOpen: true, children: tableNodes },
          ],
        },
      ],
    }),
    [tableNodes]
  );

  const queriesTree: TreeNode = useMemo(
    () => ({
      id: 'queries',
      label: 'Queries',
      icon: 'folder',
      defaultOpen: true,
      children: [
        {
          id: 'my-queries',
          label: 'My queries',
          icon: 'folder',
          defaultOpen: true,
          children: [
            { id: 'q1', label: 'SQL query 1', icon: 'query' },
            { id: 'q3', label: 'SQL query 3', icon: 'query', active: true },
          ],
        },
        { id: 'shared', label: 'Shared queries', icon: 'folder' },
      ],
    }),
    []
  );

  const { columns, rows } = datasourcePreview;
  const lineCount = sql.split('\n').length;

  const ToolbarButton: React.FC<{
    icon: React.ReactNode;
    label: string;
    accent?: boolean;
  }> = ({ icon, label, accent }) => (
    <button
      className={classNames(
        'flex items-center gap-1 px-2 h-7 rounded text-xs hover:bg-gray-100 transition-colors',
        accent ? 'text-green-600' : 'text-gray-600'
      )}
    >
      {icon}
      {label}
    </button>
  );

  return (
    <div className="flex h-[calc(100vh-7rem)] border border-gray-200 rounded-lg overflow-hidden bg-white">
      {/* Explorer */}
      <aside className="w-64 shrink-0 border-r border-gray-100 flex flex-col">
        <div className="h-9 flex items-center justify-between px-3 border-b border-gray-100">
          <span className="text-xs font-semibold text-gray-700">Explorer</span>
          <div className="flex items-center gap-2 text-gray-400">
            <PlusIcon className="h-4 w-4 hover:text-gray-700 cursor-pointer" />
            <MagnifyingGlassIcon className="h-4 w-4 hover:text-gray-700 cursor-pointer" />
          </div>
        </div>
        <div className="p-2 border-b border-gray-100">
          <div className="relative">
            <MagnifyingGlassIcon className="h-3.5 w-3.5 text-gray-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
            <input
              className="input h-7 text-xs pl-8"
              placeholder="Search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </div>
        <div className="flex-1 overflow-y-auto py-1">
          <TreeItem node={tree} depth={0} />
          <TreeItem node={queriesTree} depth={0} />
        </div>
      </aside>

      {/* Editor + results */}
      <section className="flex-1 flex flex-col min-w-0">
        {/* Tab bar */}
        <div className="h-9 flex items-center justify-between border-b border-gray-100 bg-gray-50/60 pl-2 pr-3">
          <div className="flex items-center gap-2 px-3 h-8 bg-white border border-b-0 border-gray-200 rounded-t-md text-sm text-gray-900 -mb-px">
            <DocumentTextIcon className="h-4 w-4 text-green-600" />
            <span className="text-xs">SQL query 3</span>
            <XMarkIcon className="h-3.5 w-3.5 text-gray-400 hover:text-gray-700" />
          </div>
          <ArrowPathIcon className="h-4 w-4 text-gray-400 hover:text-gray-700 cursor-pointer" />
        </div>

        {/* Toolbar */}
        <div className="h-9 flex items-center gap-1 border-b border-gray-100 px-2">
          <ToolbarButton icon={<PlayIcon className="h-4 w-4" />} label="Run" accent />
          <span className="w-px h-4 bg-gray-200 mx-1" />
          <ToolbarButton icon={<BookmarkIcon className="h-4 w-4" />} label="Save" />
          <ToolbarButton icon={<ViewColumnsIcon className="h-4 w-4" />} label="Save as view" />
          <ToolbarButton icon={<VariableIcon className="h-4 w-4" />} label="New measure" />
          <ToolbarButton icon={<ClipboardDocumentIcon className="h-4 w-4" />} label="Copy query" />
        </div>

        {/* SQL editor */}
        <div className="h-40 flex border-b border-gray-100 overflow-auto">
          <div className="select-none text-right text-gray-300 py-2 pr-3 pl-3 border-r border-gray-100 bg-gray-50/40 font-mono text-sm leading-6">
            {Array.from({ length: lineCount }).map((_, i) => (
              <div key={i}>{i + 1}</div>
            ))}
          </div>
          <textarea
            value={sql}
            onChange={(e) => setSql(e.target.value)}
            spellCheck={false}
            className="flex-1 p-2 font-mono text-sm leading-6 text-gray-800 resize-none focus:outline-none"
          />
        </div>

        {/* Results header */}
        <div className="h-9 flex items-center justify-between border-b border-gray-100 px-3">
          <div className="flex items-center gap-4 text-xs">
            <button
              onClick={() => setResultTab('messages')}
              className={classNames(
                'py-1',
                resultTab === 'messages'
                  ? 'text-gray-900 font-medium border-b-2 border-gray-900 -mb-[2px]'
                  : 'text-gray-500 hover:text-gray-800'
              )}
            >
              Messages
            </button>
            <button
              onClick={() => setResultTab('results')}
              className={classNames(
                'py-1',
                resultTab === 'results'
                  ? 'text-gray-900 font-medium border-b-2 border-gray-900 -mb-[2px]'
                  : 'text-gray-500 hover:text-gray-800'
              )}
            >
              Results
            </button>
            <span className="text-gray-400 hover:text-gray-700 cursor-pointer">Open in Excel</span>
          </div>
          <div className="flex items-center gap-3 text-xs text-gray-400">
            <span>Limit 10,000 rows</span>
            <ClipboardDocumentIcon className="h-4 w-4 hover:text-gray-700 cursor-pointer" />
            <div className="relative">
              <MagnifyingGlassIcon className="h-3.5 w-3.5 text-gray-400 absolute left-2 top-1/2 -translate-y-1/2" />
              <input className="input h-6 text-xs pl-7 w-36" placeholder="Search" />
            </div>
          </div>
        </div>

        {/* Results body */}
        <div className="flex-1 overflow-auto">
          {resultTab === 'messages' ? (
            <div className="p-4 font-mono text-xs text-gray-600">
              (1,000 rows affected)
              <br />
              Total execution time: 00:00:00.333
            </div>
          ) : (
            <table className="border-collapse text-xs w-full">
              <thead className="sticky top-0 z-10">
                <tr className="bg-gray-50">
                  <th className="sticky left-0 z-20 bg-gray-50 w-10 px-2 py-1.5 border-b border-r border-gray-200 text-gray-400 font-normal text-right">
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
                    <td className="sticky left-0 z-10 bg-white w-10 px-2 py-1.5 border-b border-r border-gray-100 text-gray-400 text-right">
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
          )}
        </div>

        {/* Status bar */}
        <div className="h-7 flex items-center justify-between px-3 border-t border-gray-100 bg-gray-50/60 text-xs text-gray-500">
          <span className="flex items-center gap-1.5">
            <CheckCircleIcon className="h-4 w-4 text-green-500" />
            Succeeded · 333 ms
          </span>
          <span>
            {columns.length} columns · 1,000 rows
          </span>
        </div>
      </section>
    </div>
  );
};

export default SqlEditor;
