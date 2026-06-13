import axios from 'axios';
import type {
  AddDataSourceFileRequest,
  DataSourceItem,
  DataSourceRequest,
  DataSourceResponse,
  DatasetItem,
  DatasetResponse,
  EngineColumnInfo,
  EngineDataSourceConnection,
  EngineExplainResult,
  EngineQueryResult,
  EngineTableInfo,
  FileUploadResponse,
} from './types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

const api = axios.create({
  baseURL: API_BASE_URL,
});

export const listDataSources = async (): Promise<DataSourceResponse[]> => {
  const response = await api.get<DataSourceResponse[]>('/api/datasources');
  return response.data;
};

export const getDataSource = async (id: string): Promise<DataSourceResponse> => {
  const response = await api.get<DataSourceResponse>(`/api/datasources/${id}`);
  return response.data;
};

export const createDataSource = async (request: DataSourceRequest): Promise<DataSourceResponse> => {
  const response = await api.post<DataSourceResponse>('/api/datasources', request);
  return response.data;
};

export const updateDataSource = async (
  id: string,
  request: DataSourceRequest
): Promise<DataSourceResponse> => {
  const response = await api.put<DataSourceResponse>(`/api/datasources/${id}`, request);
  return response.data;
};

export const deleteDataSource = async (id: string): Promise<void> => {
  await api.delete(`/api/datasources/${id}`);
};

export const uploadDataSourceFile = async (
  file: File,
  options?: {
    storageDirectory?: string;
  }
): Promise<FileUploadResponse> => {
  const formData = new FormData();
  formData.append('file', file);
  if (options?.storageDirectory) {
    formData.append('storageDirectory', options.storageDirectory);
  }

  const response = await api.post<FileUploadResponse>('/api/datasources/upload', formData);
  return response.data;
};

export const addFileToDataSource = async (
  dataSourceId: string,
  request: AddDataSourceFileRequest
): Promise<DataSourceResponse> => {
  const response = await api.post<DataSourceResponse>(
    `/api/datasources/${dataSourceId}/files`,
    request
  );
  return response.data;
};

export const uploadFileToDataSource = async (
  dataSourceId: string,
  file: File,
  options?: {
    storageDirectory?: string;
    hasHeader?: boolean;
    delimiter?: string;
    sheet?: string;
  }
): Promise<FileUploadResponse> => {
  const upload = await uploadDataSourceFile(file, {
    storageDirectory: options?.storageDirectory,
  });

  await addFileToDataSource(dataSourceId, {
    ...upload,
    hasHeader: options?.hasHeader,
    delimiter: options?.delimiter,
    sheet: options?.sheet,
  });

  return upload;
};

export const listDatasets = async (): Promise<DatasetResponse[]> => {
  const response = await api.get<DatasetResponse[]>('/api/datasets');
  return response.data;
};

// The query engine is now hosted in-process by Nexova under /api/query (it is no longer a
// separate service), so every engine call goes through the main API instance.
export const listEngineTables = async (
  dataSource: DataSourceResponse
): Promise<EngineTableInfo[]> => {
  const response = await api.post<EngineTableInfo[]>('/api/query/schema/tables', {
    dataSource: toEngineDataSource(dataSource),
  });
  return response.data;
};

export const listEngineColumns = async (
  dataSource: DataSourceResponse,
  table: string,
  schema?: string | null
): Promise<EngineColumnInfo[]> => {
  const response = await api.post<EngineColumnInfo[]>('/api/query/schema/columns', {
    dataSource: toEngineDataSource(dataSource),
    schema,
    table,
  });
  return response.data;
};

export const testEngineConnection = async (
  dataSource: DataSourceResponse
): Promise<void> => {
  await api.post('/api/query/test-connection', {
    dataSource: toEngineDataSource(dataSource),
  });
};

export const queryEngine = async (
  dataSource: DataSourceResponse,
  sql: string,
  limit = 1000
): Promise<EngineQueryResult> => {
  const response = await api.post<EngineQueryResult>('/api/query', {
    dataSource: toEngineDataSource(dataSource),
    sql,
    limit,
    timeoutMs: 30000,
  });
  return response.data;
};

export const explainEngine = async (
  dataSource: DataSourceResponse,
  sql: string,
  limit = 1000
): Promise<EngineExplainResult> => {
  const response = await api.post<EngineExplainResult>('/api/query/explain', {
    dataSource: toEngineDataSource(dataSource),
    sql,
    limit,
    timeoutMs: 30000,
  });
  return response.data;
};

export const federatedQueryEngine = async (
  dataSources: EngineDataSourceConnection[],
  sql: string,
  limit = 1000
): Promise<EngineQueryResult> => {
  const response = await api.post<EngineQueryResult>('/api/query/federated', {
    dataSources,
    sql,
    limit,
    timeoutMs: 30000,
  });
  return response.data;
};

export const mapDataSourceToItem = (dataSource: DataSourceResponse): DataSourceItem => ({
  id: dataSource.id,
  name: dataSource.name,
  type: formatDataSourceType(dataSource.type),
  host: dataSourceHost(dataSource),
  status: 'connected',
  datasets: 0,
  lastSync: formatRelativeDate(dataSource.updatedAt),
  tables: dataSource.files.map((file) => file.tableName),
});

export const mapDatasetToItem = (dataset: DatasetResponse): DatasetItem => ({
  id: dataset.id,
  name: dataset.name,
  type: dataset.sql.trim().toLowerCase().startsWith('select') ? 'virtual' : 'physical',
  database: dataset.dataSources.length > 1 ? `${dataset.dataSources.length} sources` : 'Data source',
  schema: dataset.dataSources.map((source) => source.alias).filter(Boolean).join(', ') || '-',
  owners: [],
  charts: 0,
  modified: formatRelativeDate(dataset.updatedAt),
});

const toEngineDataSource = (dataSource: DataSourceResponse): EngineDataSourceConnection => {
  const configuration = dataSource.configuration;
  return compactObject({
    type: dataSource.type,
    connectionString: configuration.connectionString,
    host: configuration.host,
    port: configuration.port,
    database: configuration.database,
    username: configuration.username,
    password: configuration.password,
    schema: configuration.schema,
    path: configuration.path,
  });
};

const dataSourceHost = (dataSource: DataSourceResponse): string => {
  const configuration = dataSource.configuration;
  if (configuration.path) return configuration.path;
  if (configuration.connectionString) return configuration.connectionString;
  if (configuration.host) {
    return configuration.port ? `${configuration.host}:${configuration.port}` : configuration.host;
  }

  return '-';
};

const formatDataSourceType = (type: string): string =>
  type
    .split(/[-_\s]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');

const formatRelativeDate = (value: string): string => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';

  const diffMs = Date.now() - date.getTime();
  const diffMinutes = Math.max(0, Math.floor(diffMs / 60_000));
  if (diffMinutes < 1) return 'just now';
  if (diffMinutes < 60) return `${diffMinutes} min ago`;

  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;

  const diffDays = Math.floor(diffHours / 24);
  return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
};

const compactObject = <T extends Record<string, unknown>>(value: T): T =>
  Object.fromEntries(
    Object.entries(value).filter(([, fieldValue]) => fieldValue !== null && fieldValue !== undefined && fieldValue !== '')
  ) as T;
