// Shared BI domain types

export interface BiOwner {
  name: string;
  initials: string;
}

export interface DashboardItem {
  id: number;
  title: string;
  status: 'published' | 'draft';
  owners: BiOwner[];
  modified: string;
  modifiedBy: string;
  favorite: boolean;
  charts: number;
}

export type ChartVizType =
  | 'Bar Chart'
  | 'Line Chart'
  | 'Area Chart'
  | 'Pie Chart'
  | 'Table'
  | 'Big Number'
  | 'Time-series'
  | 'Heatmap'
  | 'World Map'
  | 'Treemap';

export interface ChartItem {
  id: number;
  name: string;
  vizType: ChartVizType;
  dataset: string;
  owners: BiOwner[];
  modified: string;
  modifiedBy: string;
  favorite: boolean;
}

export interface DatasetItem {
  id: string | number;
  name: string;
  type: 'physical' | 'virtual';
  database: string;
  schema: string;
  owners: BiOwner[];
  charts: number;
  modified: string;
}

export type GridColumnType = 'string' | 'int' | 'decimal' | 'date';

export interface GridColumn {
  name: string;
  type: GridColumnType;
}

export interface DatasourcePreview {
  columns: GridColumn[];
  rows: (string | number)[][];
}

export interface DataSourceItem {
  id: string | number;
  name: string;
  type: string;
  host: string;
  status: 'connected' | 'error' | 'syncing';
  datasets: number;
  lastSync: string;
  tables: string[];
}

export interface DataSourceConfiguration {
  connectionString?: string | null;
  host?: string | null;
  port?: number | null;
  database?: string | null;
  username?: string | null;
  password?: string | null;
  schema?: string | null;
  path?: string | null;
  storagePath?: string | null;
  options?: Record<string, string | null>;
}

export interface DataSourceFileResponse {
  id: string;
  dataSourceId: string;
  fileName: string;
  storagePath: string;
  path: string;
  contentType: string;
  size: number;
  tableName: string;
  fileType: string;
  hasHeader?: boolean | null;
  delimiter?: string | null;
  sheet?: string | null;
  createdAt: string;
}

export interface DataSourceResponse {
  id: string;
  name: string;
  type: string;
  configuration: DataSourceConfiguration;
  files: DataSourceFileResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface DataSourceRequest {
  name: string;
  type: string;
  configuration: DataSourceConfiguration;
}

export interface FileUploadResponse {
  fileName: string;
  contentType: string;
  size: number;
  storagePath: string;
  path: string;
}

export interface AddDataSourceFileRequest extends FileUploadResponse {
  hasHeader?: boolean;
  delimiter?: string;
  sheet?: string;
}

export interface DatasetDataSourceResponse {
  dataSourceId: string;
  alias?: string | null;
  order: number;
}

export interface DatasetColumnResponse {
  id: string;
  name: string;
  type: string;
  nullable: boolean;
  precision?: number | null;
  scale?: number | null;
  ordinal: number;
}

export interface DatasetResponse {
  id: string;
  name: string;
  sql: string;
  description?: string | null;
  dataSources: DatasetDataSourceResponse[];
  columns: DatasetColumnResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface EngineDataSourceConnection extends DataSourceConfiguration {
  type: string;
  // Logical table exposed to SQL (single source) and its federated alias.
  table?: string | null;
  alias?: string | null;
  // File source options.
  hasHeader?: boolean | null;
  delimiter?: string | null;
  sheet?: string | null;
}

export interface EngineTableInfo {
  schema?: string | null;
  name: string;
  type: string;
}

export interface EngineColumnInfo {
  name: string;
  type: string;
  nullable: boolean;
  precision?: number | null;
  scale?: number | null;
}

export interface EngineQueryResult {
  columns: EngineColumnInfo[];
  rows: unknown[][];
  rowCount: number;
  durationMs: number;
}

export interface EngineExplainPlanInfo {
  planType: string;
  plan: string;
}

export interface EngineExplainResult {
  logicalPlan?: string | null;
  physicalPlan?: string | null;
  plans: EngineExplainPlanInfo[];
  durationMs: number;
}
