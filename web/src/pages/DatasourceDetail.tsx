import React, { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  MagnifyingGlassIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  FolderIcon,
  DocumentIcon,
  TableCellsIcon,
  CircleStackIcon,
  CheckCircleIcon,
  ArrowDownTrayIcon,
  ArrowPathIcon,
  XMarkIcon,
  PlusIcon,
} from '@heroicons/react/24/outline';
import classNames from 'classnames';
import {
  addFileToDataSource,
  getDataSource,
  listEngineTables,
  queryEngine,
  uploadDataSourceFile,
} from '../api/client';
import type {
  DataSourceResponse,
  EngineColumnInfo,
  EngineQueryResult,
  EngineTableInfo,
  GridColumnType,
} from '../api/types';

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
  const [datasource, setDatasource] = useState<DataSourceResponse | null>(null);
  const [tables, setTables] = useState<EngineTableInfo[]>([]);
  const [preview, setPreview] = useState<EngineQueryResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const [reloadKey, setReloadKey] = useState(0);
  const [search, setSearch] = useState('');
  const [tablesOpen, setTablesOpen] = useState(true);
  const [filesOpen, setFilesOpen] = useState(false);
  const [activeTable, setActiveTable] = useState('');
  const [adding, setAdding] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  const hasLoadedRef = useRef(false);

  useEffect(() => {
    hasLoadedRef.current = false;
  }, [id]);

  useEffect(() => {
    if (!id) return;

    let cancelled = false;

    const loadDatasource = async () => {
      try {
        if (!hasLoadedRef.current) setLoading(true);
        setError(null);
        const item = await getDataSource(id);
        const engineTables = await listEngineTables(item);
        if (!cancelled) {
          setDatasource(item);
          setTables(engineTables);
          setActiveTable((current) => current || engineTables[0]?.name || '');
          hasLoadedRef.current = true;
        }
      } catch (loadError) {
        if (!cancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load datasource from engine');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    loadDatasource();

    return () => {
      cancelled = true;
    };
  }, [id, reloadKey]);

  useEffect(() => {
    if (!datasource || !activeTable) return;

    let cancelled = false;

    const loadPreview = async () => {
      try {
        setPreviewLoading(true);
        setPreviewError(null);
        const table = tables.find((candidate) => candidate.name === activeTable);
        const result = await queryEngine(datasource, `select * from ${tableReference(table ?? activeTable)}`, 1000);
        if (!cancelled) {
          setPreview(result);
        }
      } catch (loadError) {
        if (!cancelled) {
          setPreview(null);
          setPreviewError(loadError instanceof Error ? loadError.message : 'Failed to load table preview');
        }
      } finally {
        if (!cancelled) {
          setPreviewLoading(false);
        }
      }
    };

    loadPreview();

    return () => {
      cancelled = true;
    };
  }, [activeTable, datasource, refreshKey, tables]);

  const handleAddFiles = async (selected: File[]) => {
    if (!datasource || selected.length === 0) return;

    setAddError(null);
    setAdding(true);
    try {
      const storageDirectory = datasource.configuration.storagePath ?? undefined;
      for (const file of selected) {
        const upload = await uploadDataSourceFile(file, { storageDirectory });
        await addFileToDataSource(datasource.id, { ...upload });
      }
      setFilesOpen(true);
      setReloadKey((key) => key + 1);
    } catch (uploadError) {
      setAddError(uploadError instanceof Error ? uploadError.message : 'Failed to add file');
    } finally {
      setAdding(false);
    }
  };

  if (loading) {
    return <div className="card p-6 text-sm text-gray-500">Loading datasource...</div>;
  }

  if (error || !datasource) {
    return (
      <div className="card p-6 text-sm text-red-700">
        {error ?? 'Datasource not found'}
      </div>
    );
  }

  const visibleTables = tables.filter((table) =>
    table.name.toLowerCase().includes(search.toLowerCase())
  );
  const visibleFiles = fileEntriesFromDatasource(datasource, tables).filter((file) =>
    file.name.toLowerCase().includes(search.toLowerCase())
  );
  const canAddFiles = Boolean(datasource.configuration.storagePath);

  const columns = preview?.columns ?? [];
  const rows = preview?.rows ?? [];

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
              visibleTables.map((table) => (
                <button
                  key={`${table.schema ?? ''}.${table.name}`}
                  onClick={() => setActiveTable(table.name)}
                  className={classNames(
                    'w-full flex items-center gap-1.5 pl-11 pr-3 py-2 hover:bg-gray-50 text-left',
                    table.name === activeTable ? 'bg-gray-100 text-gray-900' : 'text-gray-600'
                  )}
                >
                  <TableCellsIcon className="h-4 w-4 text-gray-400 shrink-0" />
                  <span className="font-mono text-xs truncate">{table.name}</span>
                </button>
              ))}

            {/* Files */}
            <div className="flex items-center pr-2 hover:bg-gray-50">
              <button
                onClick={() => setFilesOpen((o) => !o)}
                className="flex flex-1 items-center gap-1 pl-5 pr-3 py-2 text-gray-700"
              >
                {filesOpen ? (
                  <ChevronDownIcon className="h-3.5 w-3.5 text-gray-400" />
                ) : (
                  <ChevronRightIcon className="h-3.5 w-3.5 text-gray-400" />
                )}
                <FolderIcon className="h-4 w-4 text-gray-400" />
                <span>Files</span>
              </button>
              {canAddFiles && (
                <label
                  className={classNames(
                    'flex h-6 w-6 shrink-0 items-center justify-center rounded text-gray-400',
                    adding ? 'cursor-not-allowed opacity-60' : 'cursor-pointer hover:bg-gray-200 hover:text-gray-700'
                  )}
                  title="Add file"
                >
                  <input
                    className="sr-only"
                    type="file"
                    multiple
                    disabled={adding}
                    onChange={(event) => {
                      handleAddFiles(Array.from(event.target.files ?? []));
                      event.target.value = '';
                    }}
                  />
                  {adding ? (
                    <ArrowPathIcon className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <PlusIcon className="h-4 w-4" />
                  )}
                </label>
              )}
            </div>
            {addError && (
              <div className="px-5 pb-1 text-xs text-red-600">{addError}</div>
            )}
            {filesOpen &&
              (visibleFiles.length > 0 ? (
                visibleFiles.map((file) => (
                  <button
                    key={file.name}
                    onClick={() => setActiveTable(file.tableName)}
                    className={classNames(
                      'w-full flex items-center gap-1.5 pl-11 pr-3 py-2 hover:bg-gray-50 text-left',
                      file.tableName === activeTable ? 'bg-gray-100 text-gray-900' : 'text-gray-600'
                    )}
                  >
                    <DocumentIcon className="h-4 w-4 text-gray-400 shrink-0" />
                    <span className="min-w-0 flex-1 truncate font-mono text-xs">{file.name}</span>
                  </button>
                ))
              ) : (
                <div className="pl-11 pr-3 py-2 text-xs text-gray-400">No files</div>
              ))}
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
              <button
                className="text-gray-400 hover:text-gray-700"
                title="Refresh"
                onClick={() => setRefreshKey((key) => key + 1)}
              >
                <ArrowPathIcon className="h-4 w-4" />
              </button>
              <button className="text-gray-400 hover:text-gray-700" title="Export">
                <ArrowDownTrayIcon className="h-4 w-4" />
              </button>
            </div>
            <span className="text-xs text-gray-400">
              {previewLoading ? 'Loading preview...' : `Showing ${rows.length} rows`}
            </span>
          </div>

          {/* Grid */}
          <div className="flex-1 overflow-auto">
            {previewError ? (
              <div className="p-6 text-sm text-red-700">{previewError}</div>
            ) : !activeTable ? (
              <div className="p-6 text-sm text-gray-500">No tables found for this datasource.</div>
            ) : (
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
                          <TypeBadge type={mapEngineColumnType(col)} />
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
                            isNumericColumn(columns[ci])
                              ? 'text-right font-mono text-gray-700'
                              : 'text-gray-700'
                          )}
                        >
                          {formatCell(cell)}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Status bar */}
          <div className="h-8 flex items-center justify-between px-3 border-t border-gray-100 bg-gray-50/60 text-xs text-gray-500">
            <span className="flex items-center gap-1.5">
              <CheckCircleIcon className="h-4 w-4 text-green-500" />
              {previewLoading ? 'Running...' : preview ? `Ran in ${preview.durationMs}ms` : 'Ready'}
            </span>
            <span>
              {columns.length} columns · {rows.length} rows
            </span>
          </div>
        </section>
      </div>
    </div>
  );
};

const tableReference = (table: EngineTableInfo | string): string => {
  if (typeof table === 'string') {
    return quoteIdentifier(table);
  }

  return table.schema
    ? `${quoteIdentifier(table.schema)}.${quoteIdentifier(table.name)}`
    : quoteIdentifier(table.name);
};

const quoteIdentifier = (value: string): string => `"${value.replace(/"/g, '""')}"`;

interface FileEntry {
  name: string;
  tableName: string;
  type: string;
}

const fileEntriesFromDatasource = (
  datasource: DataSourceResponse,
  tables: EngineTableInfo[]
): FileEntry[] => {
  if (datasource.files.length > 0) {
    return datasource.files.map((file) => ({
      name: file.fileName,
      tableName: file.tableName,
      type: file.fileType,
    }));
  }

  const pathFileName = fileNameFromPath(datasource.configuration.path);
  if (pathFileName) {
    return [
      {
        name: pathFileName,
        tableName: tables[0]?.name ?? tableNameFromFileName(pathFileName),
        type: fileExtension(pathFileName) ?? tables[0]?.type ?? 'file',
      },
    ];
  }

  return tables.map((table) => ({
    name: fileNameFromTable(table),
    tableName: table.name,
    type: table.type,
  }));
};

const fileNameFromPath = (path?: string | null): string | null => {
  if (!path) return null;
  const parts = path.split(/[\\/]/).filter(Boolean);
  const name = parts[parts.length - 1];
  if (!name || !fileExtension(name)) return null;
  return name;
};

const fileNameFromTable = (table: EngineTableInfo): string => {
  const extension = table.type.toLowerCase();
  return extension ? `${table.name}.${extension}` : table.name;
};

const fileExtension = (name: string): string | null => {
  const parts = name.split('.');
  const extension = parts[parts.length - 1];
  return extension && extension !== name ? extension : null;
};

const tableNameFromFileName = (name: string): string => {
  const withoutExtension = name.replace(/\.[^.]+$/, '');
  const sanitized = withoutExtension.replace(/[^A-Za-z0-9_]/g, '_').replace(/^_+|_+$/g, '');
  return /^\d/.test(sanitized) ? `t_${sanitized}` : sanitized || 'data';
};

const mapEngineColumnType = (column: EngineColumnInfo): GridColumnType => {
  const type = column.type.toLowerCase();
  if (type.includes('date') || type.includes('timestamp')) return 'date';
  if (type.includes('decimal') || type.includes('float') || type.includes('double')) return 'decimal';
  if (type.includes('int') || type.includes('uint')) return 'int';
  return 'string';
};

const isNumericColumn = (column: EngineColumnInfo | undefined): boolean => {
  if (!column) return false;
  const type = mapEngineColumnType(column);
  return type === 'int' || type === 'decimal';
};

const formatCell = (value: unknown): string => {
  if (value === null || value === undefined) return '';
  if (typeof value === 'object') return JSON.stringify(value);
  return String(value);
};

export default DatasourceDetail;
