import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  PlusIcon,
  PencilSquareIcon,
  TrashIcon,
  CircleStackIcon,
  XMarkIcon,
  ArrowUpTrayIcon,
} from '@heroicons/react/24/outline';
import PageHeader from '../components/PageHeader';
import FilterBar from '../components/FilterBar';
import DataTable, { Column } from '../components/DataTable';
import Tag from '../components/Tag';
import Select from '../components/Select';
import {
  addFileToDataSource,
  createDataSource,
  deleteDataSource,
  getDataSource,
  listDataSources,
  mapDataSourceToItem,
  updateDataSource,
  uploadDataSourceFile,
} from '../api/client';
import type {
  DataSourceConfiguration,
  DataSourceItem,
  DataSourceResponse,
  FileUploadResponse,
} from '../api/types';

const statusVariant = (s: DataSourceItem['status']) =>
  s === 'connected' ? 'success' : s === 'syncing' ? 'info' : 'error';

const statusLabel = (s: DataSourceItem['status']) =>
  s === 'connected' ? 'Connected' : s === 'syncing' ? 'Syncing' : 'Error';

const dataSourceTypes = ['postgres', 'mysql', 'clickhouse', 'mongodb', 'files'] as const;

type DataSourceType = (typeof dataSourceTypes)[number];

type UploadStatus = 'uploading' | 'done' | 'error';

interface UploadEntry {
  id: string;
  fileName: string;
  status: UploadStatus;
  result?: FileUploadResponse;
  error?: string;
}

interface CreateDataSourceForm {
  name: string;
  type: DataSourceType;
  host: string;
  port: string;
  database: string;
  username: string;
  password: string;
  schema: string;
  connectionString: string;
  path: string;
  hasHeader: boolean;
  delimiter: string;
}

const defaultCreateForm: CreateDataSourceForm = {
  name: '',
  type: 'postgres',
  host: 'localhost',
  port: '5432',
  database: '',
  username: '',
  password: '',
  schema: 'public',
  connectionString: '',
  path: '',
  hasHeader: true,
  delimiter: ',',
};

const Datasources: React.FC = () => {
  const [datasources, setDatasources] = useState<DataSourceItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [createForm, setCreateForm] = useState<CreateDataSourceForm>(defaultCreateForm);
  const [createError, setCreateError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [uploads, setUploads] = useState<UploadEntry[]>([]);
  const [uploadDir, setUploadDir] = useState('');
  const [editOpen, setEditOpen] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [editOriginal, setEditOriginal] = useState<DataSourceResponse | null>(null);
  const [editForm, setEditForm] = useState<CreateDataSourceForm>(defaultCreateForm);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<DataSourceItem | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [search, setSearch] = useState('');
  const [type, setType] = useState('All');
  const [status, setStatus] = useState('All');

  const loadDataSources = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const items = await listDataSources();
      setDatasources(items.map(mapDataSourceToItem));
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load data sources');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDataSources();
  }, [loadDataSources]);

  const updateCreateForm = <K extends keyof CreateDataSourceForm>(
    key: K,
    value: CreateDataSourceForm[K]
  ) => {
    setCreateForm((current) => ({ ...current, [key]: value }));
  };

  const openCreateDialog = () => {
    setCreateError(null);
    setCreateForm(defaultCreateForm);
    setUploads([]);
    setUploadDir(`data-sources/${crypto.randomUUID()}`);
    setCreateOpen(true);
  };

  const closeCreateDialog = () => {
    if (saving) return;
    setCreateOpen(false);
    setCreateError(null);
    setUploads([]);
  };

  const handleFilesSelected = async (selected: File[]) => {
    if (selected.length === 0) return;

    const entries: UploadEntry[] = selected.map((file) => ({
      id: crypto.randomUUID(),
      fileName: file.name,
      status: 'uploading',
    }));
    setUploads((current) => [...current, ...entries]);

    await Promise.all(
      selected.map(async (file, index) => {
        const entryId = entries[index].id;
        try {
          const result = await uploadDataSourceFile(file, { storageDirectory: uploadDir });
          setUploads((current) =>
            current.map((entry) =>
              entry.id === entryId ? { ...entry, status: 'done', result } : entry
            )
          );
        } catch (uploadError) {
          const message =
            uploadError instanceof Error ? uploadError.message : 'Upload failed';
          setUploads((current) =>
            current.map((entry) =>
              entry.id === entryId ? { ...entry, status: 'error', error: message } : entry
            )
          );
        }
      })
    );
  };

  const handleRemoveUpload = (id: string) => {
    setUploads((current) => current.filter((entry) => entry.id !== id));
  };

  const handleTypeChange = (nextType: string) => {
    const sourceType = nextType as DataSourceType;
    const defaultPort = sourceType === 'mysql' ? '3306' : sourceType === 'clickhouse' ? '8123' : '5432';
    setCreateForm((current) => ({
      ...current,
      type: sourceType,
      port: sourceType === 'mongodb' || sourceType === 'files' ? '' : defaultPort,
      schema: sourceType === 'postgres' ? current.schema || 'public' : current.schema,
    }));
  };

  const handleCreateSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setCreateError(null);

    const name = createForm.name.trim();
    if (!name) {
      setCreateError('Name is required.');
      return;
    }

    const hasUploads = uploads.length > 0;
    if (createForm.type === 'files') {
      if (uploads.some((entry) => entry.status === 'uploading')) {
        setCreateError('Please wait for files to finish uploading.');
        return;
      }
      if (uploads.some((entry) => entry.status === 'error')) {
        setCreateError('Remove the failed uploads before continuing.');
        return;
      }
      if (!hasUploads && !createForm.path.trim()) {
        setCreateError('Upload one or more files or provide a server path.');
        return;
      }
    }

    try {
      setSaving(true);
      const configuration = buildConfiguration(createForm, uploadDir, hasUploads);
      const created = await createDataSource({
        name,
        type: createForm.type,
        configuration,
      });

      let dataSource = created;
      if (createForm.type === 'files') {
        for (const entry of uploads) {
          if (entry.status !== 'done' || !entry.result) continue;
          dataSource = await addFileToDataSource(created.id, {
            ...entry.result,
            hasHeader: createForm.hasHeader,
            delimiter: createForm.delimiter || ',',
          });
        }
      }

      setDatasources((current) => [mapDataSourceToItem(dataSource), ...current]);
      setCreateOpen(false);
      setCreateForm(defaultCreateForm);
      setUploads([]);
    } catch (createFailure) {
      setCreateError(createFailure instanceof Error ? createFailure.message : 'Failed to create datasource');
    } finally {
      setSaving(false);
    }
  };

  const openEditDialog = async (item: DataSourceItem) => {
    setError(null);
    try {
      const dataSource = await getDataSource(String(item.id));
      setEditOriginal(dataSource);
      setEditId(dataSource.id);
      setEditForm(formFromResponse(dataSource));
      setEditError(null);
      setEditOpen(true);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load datasource');
    }
  };

  const closeEditDialog = () => {
    if (editSaving) return;
    setEditOpen(false);
    setEditError(null);
    setEditId(null);
    setEditOriginal(null);
  };

  const updateEditForm = <K extends keyof CreateDataSourceForm>(
    key: K,
    value: CreateDataSourceForm[K]
  ) => {
    setEditForm((current) => ({ ...current, [key]: value }));
  };

  const handleEditSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setEditError(null);

    if (!editId || !editOriginal) return;

    const name = editForm.name.trim();
    if (!name) {
      setEditError('Name is required.');
      return;
    }

    try {
      setEditSaving(true);
      const configuration = buildUpdateConfiguration(editForm, editOriginal);
      const updated = await updateDataSource(editId, {
        name,
        type: editForm.type,
        configuration,
      });

      setDatasources((current) =>
        current.map((item) => (String(item.id) === editId ? mapDataSourceToItem(updated) : item))
      );
      closeEditDialog();
    } catch (updateFailure) {
      setEditError(updateFailure instanceof Error ? updateFailure.message : 'Failed to update datasource');
    } finally {
      setEditSaving(false);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;

    try {
      setDeleting(true);
      await deleteDataSource(String(deleteTarget.id));
      setDatasources((current) => current.filter((item) => item.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch (deleteFailure) {
      setError(deleteFailure instanceof Error ? deleteFailure.message : 'Failed to delete datasource');
    } finally {
      setDeleting(false);
    }
  };

  const typeOptions = useMemo(
    () => ['All', ...Array.from(new Set(datasources.map((d) => d.type)))],
    [datasources]
  );

  const filtered = useMemo(() => {
    return datasources.filter((d) => {
      if (type !== 'All' && d.type !== type) return false;
      if (status !== 'All' && statusLabel(d.status) !== status) return false;
      if (search && !d.name.toLowerCase().includes(search.toLowerCase())) return false;
      return true;
    });
  }, [datasources, search, type, status]);

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
      render: (d) => (
        <div className="flex items-center gap-3 text-gray-400">
          <button
            type="button"
            className="hover:text-gray-900"
            title="Edit"
            onClick={() => openEditDialog(d)}
          >
            <PencilSquareIcon className="h-4 w-4" />
          </button>
          <button
            type="button"
            className="hover:text-red-500"
            title="Delete"
            onClick={() => setDeleteTarget(d)}
          >
            <TrashIcon className="h-4 w-4" />
          </button>
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
          <button className="btn-primary" onClick={openCreateDialog}>
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

      {error && (
        <div className="mb-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <DataTable
        columns={columns}
        rows={loading ? [] : filtered}
        rowKey={(d) => d.id}
        loading={loading}
        emptyText="No datasources found"
      />

      {createOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-gray-900/40 px-4">
          <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl">
            <div className="flex items-center justify-between border-b border-gray-100 px-5 py-4">
              <div>
                <h2 className="text-base font-semibold text-gray-900">Add datasource</h2>
                <p className="text-sm text-gray-500">Create a database or file data source.</p>
              </div>
              <button
                type="button"
                className="rounded-md p-1.5 text-gray-400 hover:bg-gray-100 hover:text-gray-700"
                onClick={closeCreateDialog}
                disabled={saving}
              >
                <XMarkIcon className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handleCreateSubmit}>
              <div className="max-h-[70vh] space-y-4 overflow-y-auto px-5 py-4">
                {createError && (
                  <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                    {createError}
                  </div>
                )}

                <div className="grid gap-4 sm:grid-cols-2">
                  <label className="block">
                    <span className="mb-1 block text-sm font-medium text-gray-700">Name</span>
                    <input
                      className="input"
                      value={createForm.name}
                      onChange={(event) => updateCreateForm('name', event.target.value)}
                      placeholder="Local PostgreSQL"
                      required
                    />
                  </label>

                  <label className="block">
                    <span className="mb-1 block text-sm font-medium text-gray-700">Type</span>
                    <Select
                      options={[...dataSourceTypes]}
                      value={createForm.type}
                      onChange={handleTypeChange}
                    />
                  </label>
                </div>

                {createForm.type === 'mongodb' ? (
                  <MongoFields form={createForm} onChange={updateCreateForm} />
                ) : createForm.type === 'files' ? (
                  <FileFields
                    form={createForm}
                    onChange={updateCreateForm}
                    uploads={uploads}
                    onFilesSelected={handleFilesSelected}
                    onRemoveUpload={handleRemoveUpload}
                  />
                ) : (
                  <DatabaseFields form={createForm} onChange={updateCreateForm} />
                )}
              </div>

              <div className="flex items-center justify-end gap-2 border-t border-gray-100 px-5 py-4">
                <button type="button" className="btn-secondary" onClick={closeCreateDialog} disabled={saving}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary" disabled={saving}>
                  {saving ? 'Creating...' : 'Create datasource'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {editOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-gray-900/40 px-4">
          <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl">
            <div className="flex items-center justify-between border-b border-gray-100 px-5 py-4">
              <div>
                <h2 className="text-base font-semibold text-gray-900">Edit datasource</h2>
                <p className="text-sm text-gray-500">Update connection details for this data source.</p>
              </div>
              <button
                type="button"
                className="rounded-md p-1.5 text-gray-400 hover:bg-gray-100 hover:text-gray-700"
                onClick={closeEditDialog}
                disabled={editSaving}
              >
                <XMarkIcon className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handleEditSubmit}>
              <div className="max-h-[70vh] space-y-4 overflow-y-auto px-5 py-4">
                {editError && (
                  <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                    {editError}
                  </div>
                )}

                <div className="grid gap-4 sm:grid-cols-2">
                  <label className="block">
                    <span className="mb-1 block text-sm font-medium text-gray-700">Name</span>
                    <input
                      className="input"
                      value={editForm.name}
                      onChange={(event) => updateEditForm('name', event.target.value)}
                      placeholder="Local PostgreSQL"
                      required
                    />
                  </label>

                  <label className="block">
                    <span className="mb-1 block text-sm font-medium text-gray-700">Type</span>
                    <input className="input bg-gray-50 text-gray-500" value={editForm.type} disabled />
                  </label>
                </div>

                {editForm.type === 'mongodb' ? (
                  <MongoFields form={editForm} onChange={updateEditForm} />
                ) : editForm.type === 'files' ? (
                  <TextField
                    label="Server path"
                    value={editForm.path}
                    onChange={(value) => updateEditForm('path', value)}
                    placeholder="C:/Users/neuron/source/vision/data"
                  />
                ) : (
                  <DatabaseFields form={editForm} onChange={updateEditForm} />
                )}
              </div>

              <div className="flex items-center justify-end gap-2 border-t border-gray-100 px-5 py-4">
                <button type="button" className="btn-secondary" onClick={closeEditDialog} disabled={editSaving}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary" disabled={editSaving}>
                  {editSaving ? 'Saving...' : 'Save changes'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-gray-900/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white shadow-xl">
            <div className="px-5 py-4">
              <h2 className="text-base font-semibold text-gray-900">Delete datasource</h2>
              <p className="mt-2 text-sm text-gray-600">
                Are you sure you want to delete{' '}
                <span className="font-medium text-gray-900">{deleteTarget.name}</span>? This action
                cannot be undone.
              </p>
            </div>
            <div className="flex items-center justify-end gap-2 border-t border-gray-100 px-5 py-4">
              <button
                type="button"
                className="btn-secondary"
                onClick={() => setDeleteTarget(null)}
                disabled={deleting}
              >
                Cancel
              </button>
              <button
                type="button"
                className="inline-flex items-center gap-1.5 rounded-md bg-red-600 px-3 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-60"
                onClick={confirmDelete}
                disabled={deleting}
              >
                {deleting ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

interface CreateFieldProps {
  form: CreateDataSourceForm;
  onChange: <K extends keyof CreateDataSourceForm>(key: K, value: CreateDataSourceForm[K]) => void;
}

const DatabaseFields: React.FC<CreateFieldProps> = ({ form, onChange }) => (
  <div className="grid gap-4 sm:grid-cols-2">
    <TextField label="Host" value={form.host} onChange={(value) => onChange('host', value)} required />
    <TextField label="Port" value={form.port} onChange={(value) => onChange('port', value)} type="number" />
    <TextField label="Database" value={form.database} onChange={(value) => onChange('database', value)} />
    <TextField label="Username" value={form.username} onChange={(value) => onChange('username', value)} />
    <TextField
      label="Password"
      value={form.password}
      onChange={(value) => onChange('password', value)}
      type="password"
    />
    <TextField label="Schema" value={form.schema} onChange={(value) => onChange('schema', value)} />
  </div>
);

const MongoFields: React.FC<CreateFieldProps> = ({ form, onChange }) => (
  <div className="space-y-4">
    <TextField
      label="Connection string"
      value={form.connectionString}
      onChange={(value) => onChange('connectionString', value)}
      placeholder="mongodb://root:password@localhost:27017/mongo_db?authSource=admin"
      required
    />
    <TextField label="Database" value={form.database} onChange={(value) => onChange('database', value)} />
  </div>
);

interface FileFieldsProps extends CreateFieldProps {
  uploads: UploadEntry[];
  onFilesSelected: (files: File[]) => void;
  onRemoveUpload: (id: string) => void;
}

const FileFields: React.FC<FileFieldsProps> = ({
  form,
  onChange,
  uploads,
  onFilesSelected,
  onRemoveUpload,
}) => (
  <div className="space-y-4">
    <div>
      <span className="mb-1 block text-sm font-medium text-gray-700">Upload file</span>
      <label className="flex cursor-pointer items-center justify-between gap-3 rounded-md border border-dashed border-gray-300 bg-gray-50 px-4 py-3 transition-colors hover:border-gray-400 hover:bg-white">
        <input
          className="sr-only"
          type="file"
          multiple
          onChange={(event) => {
            onFilesSelected(Array.from(event.target.files ?? []));
            event.target.value = '';
          }}
        />
        <span className="flex min-w-0 items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-white text-gray-500 shadow-sm ring-1 ring-gray-200">
            <ArrowUpTrayIcon className="h-4 w-4" />
          </span>
          <span className="min-w-0">
            <span className="block truncate text-sm font-medium text-gray-800">
              {fileSelectionLabel(uploads)}
            </span>
            <span className="block text-xs text-gray-400">
              CSV, Excel, JSON, Parquet or other supported files
            </span>
          </span>
        </span>
        <span className="shrink-0 rounded-md bg-gray-900 px-3 py-1.5 text-xs font-medium text-white">
          Browse
        </span>
      </label>
      <span className="mt-1 block text-xs text-gray-400">
        Files are uploaded immediately. You can also provide a server-side folder/file path below.
      </span>
    </div>

    {uploads.length > 0 && (
      <ul className="space-y-1.5">
        {uploads.map((entry) => (
          <li
            key={entry.id}
            className="flex items-center justify-between gap-3 rounded-md border border-gray-200 bg-white px-3 py-2 text-sm"
          >
            <span className="min-w-0 flex-1 truncate font-mono text-xs text-gray-700">
              {entry.fileName}
            </span>
            <span className="flex shrink-0 items-center gap-2">
              <UploadStatusBadge entry={entry} />
              <button
                type="button"
                className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-700"
                onClick={() => onRemoveUpload(entry.id)}
                title="Remove"
              >
                <XMarkIcon className="h-4 w-4" />
              </button>
            </span>
          </li>
        ))}
      </ul>
    )}

    <TextField
      label="Server path"
      value={form.path}
      onChange={(value) => onChange('path', value)}
      placeholder="C:/Users/neuron/source/vision/data"
    />
    <div className="grid gap-4 sm:grid-cols-2">
      <TextField label="Delimiter" value={form.delimiter} onChange={(value) => onChange('delimiter', value)} />
      <label className="flex items-center gap-2 pt-7 text-sm text-gray-700">
        <input
          type="checkbox"
          checked={form.hasHeader}
          onChange={(event) => onChange('hasHeader', event.target.checked)}
        />
        Has header
      </label>
    </div>
  </div>
);

const UploadStatusBadge: React.FC<{ entry: UploadEntry }> = ({ entry }) => {
  if (entry.status === 'uploading') {
    return <span className="text-xs text-gray-400">Uploading...</span>;
  }
  if (entry.status === 'error') {
    return (
      <span className="text-xs text-red-600" title={entry.error}>
        Failed
      </span>
    );
  }
  return <span className="text-xs text-green-600">Uploaded</span>;
};

interface TextFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  placeholder?: string;
  required?: boolean;
}

const TextField: React.FC<TextFieldProps> = ({
  label,
  value,
  onChange,
  type = 'text',
  placeholder,
  required,
}) => (
  <label className="block">
    <span className="mb-1 block text-sm font-medium text-gray-700">{label}</span>
    <input
      className="input"
      type={type}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      placeholder={placeholder}
      required={required}
    />
  </label>
);

const buildConfiguration = (
  form: CreateDataSourceForm,
  uploadDir: string,
  hasUploads: boolean
): DataSourceConfiguration => {
  if (form.type === 'files') {
    if (hasUploads) {
      return compactConfiguration({
        storagePath: uploadDir,
      });
    }

    return {
      path: form.path.trim(),
    };
  }

  if (form.type === 'mongodb') {
    return compactConfiguration({
      connectionString: form.connectionString.trim(),
      database: form.database.trim(),
    });
  }

  return compactConfiguration({
    host: form.host.trim(),
    port: form.port ? Number(form.port) : null,
    database: form.database.trim(),
    username: form.username.trim(),
    password: form.password,
    schema: form.schema.trim(),
  });
};

const buildUpdateConfiguration = (
  form: CreateDataSourceForm,
  original: DataSourceResponse
): DataSourceConfiguration => {
  const base = original.configuration ?? {};

  if (form.type === 'files') {
    return compactConfiguration({
      ...base,
      path: form.path.trim(),
    });
  }

  if (form.type === 'mongodb') {
    return compactConfiguration({
      ...base,
      connectionString: form.connectionString.trim(),
      database: form.database.trim(),
    });
  }

  return compactConfiguration({
    ...base,
    host: form.host.trim(),
    port: form.port ? Number(form.port) : null,
    database: form.database.trim(),
    username: form.username.trim(),
    password: form.password,
    schema: form.schema.trim(),
  });
};

const formFromResponse = (dataSource: DataSourceResponse): CreateDataSourceForm => {
  const configuration = dataSource.configuration ?? {};
  const type = (dataSourceTypes as readonly string[]).includes(dataSource.type)
    ? (dataSource.type as DataSourceType)
    : 'postgres';

  return {
    name: dataSource.name,
    type,
    host: configuration.host ?? '',
    port: configuration.port != null ? String(configuration.port) : '',
    database: configuration.database ?? '',
    username: configuration.username ?? '',
    password: configuration.password ?? '',
    schema: configuration.schema ?? '',
    connectionString: configuration.connectionString ?? '',
    path: configuration.path ?? '',
    hasHeader: true,
    delimiter: ',',
  };
};

const compactConfiguration = (configuration: DataSourceConfiguration): DataSourceConfiguration =>
  Object.fromEntries(
    Object.entries(configuration).filter(([, value]) => value !== '' && value !== null && value !== undefined)
  ) as DataSourceConfiguration;

const fileSelectionLabel = (uploads: UploadEntry[]): string => {
  if (uploads.length === 0) return 'Choose files';
  if (uploads.length === 1) return uploads[0].fileName;
  return `${uploads.length} files selected`;
};

export default Datasources;
